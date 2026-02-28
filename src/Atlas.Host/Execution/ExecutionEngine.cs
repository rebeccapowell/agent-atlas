using Atlas.Host.Configuration;
using Atlas.Host.Mcp;
using Atlas.Host.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Atlas.Host.Execution;

public interface IExecutionEngine
{
    Task<McpExecuteResult> ExecuteAsync(JsonElement plan, string mode, string? environment, string token, CancellationToken ct = default);
}

public class ExecutionEngine : IExecutionEngine
{
    private readonly IToolIndex _toolIndex;
    private readonly ICatalogLoader _catalog;
    private readonly ExecLimitsOptions _limits;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExecutionEngine> _logger;

    public ExecutionEngine(
        IToolIndex toolIndex,
        ICatalogLoader catalog,
        IOptions<AtlasOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<ExecutionEngine> logger)
    {
        _toolIndex = toolIndex;
        _catalog = catalog;
        _limits = options.Value.ExecLimits;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<McpExecuteResult> ExecuteAsync(JsonElement plan, string mode, string? environment, string token, CancellationToken ct = default)
    {
        if (plan.ValueKind == JsonValueKind.Undefined)
            return new McpExecuteResult(false, mode, Error: "Invalid plan format: plan is undefined");

        // Enforce the MaxSeconds wall-clock budget in addition to the caller's token
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_limits.MaxSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var execCt = linkedCts.Token;

        var context = new ExecContext(mode == "dryRun", _limits, token, environment);
        var steps = new List<McpExecuteStep>();

        // Unwrap plan root: support {"steps":[...]} as well as a bare array or single step
        var stepsEl = plan.ValueKind == JsonValueKind.Object && plan.TryGetProperty("steps", out var s) ? s : plan;

        try
        {
            await ExecuteStepsAsync(stepsEl, context, steps, execCt);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return new McpExecuteResult(false, mode, Error: $"Execution exceeded the {_limits.MaxSeconds}s time limit", Steps: steps.ToArray());
        }
        catch (ExecutionLimitException ex)
        {
            return new McpExecuteResult(false, mode, Error: ex.Message, Steps: steps.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution error");
            return new McpExecuteResult(false, mode, Error: ex.Message, Steps: steps.ToArray());
        }

        return new McpExecuteResult(true, mode, Result: context.LastResult, Steps: steps.ToArray());
    }

    private async Task ExecuteStepsAsync(JsonElement steps, ExecContext ctx, List<McpExecuteStep> stepLog, CancellationToken ct)
    {
        if (steps.ValueKind != JsonValueKind.Array)
        {
            await ExecuteStepAsync(steps, ctx, stepLog, ct);
            return;
        }

        foreach (var step in steps.EnumerateArray())
        {
            await ExecuteStepAsync(step, ctx, stepLog, ct);
        }
    }

    private async Task ExecuteStepAsync(JsonElement step, ExecContext ctx, List<McpExecuteStep> stepLog, CancellationToken ct)
    {
        if (!step.TryGetProperty("type", out var typeEl))
            throw new InvalidOperationException("Step missing 'type' field");

        var type = typeEl.GetString();
        switch (type)
        {
            case "call":
                await ExecuteCallAsync(step, ctx, stepLog, ct);
                break;
            case "foreach":
                await ExecuteForeachAsync(step, ctx, stepLog, ct);
                break;
            case "if":
                await ExecuteIfAsync(step, ctx, stepLog, ct);
                break;
            case "return":
                ExecuteReturn(step, ctx);
                break;
            default:
                throw new InvalidOperationException($"Unknown step type: {type}");
        }
    }

    private async Task ExecuteCallAsync(JsonElement step, ExecContext ctx, List<McpExecuteStep> stepLog, CancellationToken ct)
    {
        ctx.IncrementSteps(_limits.MaxSteps);
        ctx.IncrementCalls(_limits.MaxCalls);

        var toolId = step.GetProperty("toolId").GetString()!;
        var saveAs = step.TryGetProperty("saveAs", out var saveAsEl) ? saveAsEl.GetString() : null;
        var argsEl = step.TryGetProperty("args", out var argsE) ? argsE : default;

        var tool = _toolIndex.GetById(toolId)
            ?? throw new InvalidOperationException($"Tool '{toolId}' not found");

        var api = _catalog.GetApis().FirstOrDefault(a => a.ApiId == tool.ApiId)
            ?? throw new InvalidOperationException($"API '{tool.ApiId}' not found");

        string baseUrl;
        if (!string.IsNullOrEmpty(ctx.Environment) && api.Environments.TryGetValue(ctx.Environment, out var envUrl))
            baseUrl = envUrl;
        else if (!string.IsNullOrEmpty(api.BaseUrl))
            baseUrl = api.BaseUrl;
        else
            throw new InvalidOperationException($"No base URL for API '{tool.ApiId}' in environment '{ctx.Environment}'");

        var path = tool.Path;
        var args = argsEl.ValueKind == JsonValueKind.Undefined
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(argsEl.GetRawText()) ?? new();

        args = SubstituteVariables(args, ctx.Variables);

        foreach (var arg in args.ToList())
        {
            var placeholder = $"{{{arg.Key}}}";
            if (path.Contains(placeholder))
            {
                path = path.Replace(placeholder, Uri.EscapeDataString(arg.Value?.ToString() ?? ""));
                args.Remove(arg.Key);
            }
        }

        // Build the URL using UriBuilder to avoid path/slash ambiguity
        var uriBuilder = new UriBuilder(baseUrl.TrimEnd('/'));
        uriBuilder.Path = uriBuilder.Path.TrimEnd('/') + path;

        if (tool.Method == "GET" && args.Count > 0)
        {
            uriBuilder.Query = string.Join("&", args.Select(a =>
                $"{Uri.EscapeDataString(a.Key)}={Uri.EscapeDataString(a.Value?.ToString() ?? "")}"));
        }

        var url = uriBuilder.Uri.AbsoluteUri;

        var stepRecord = new McpExecuteStep(toolId, tool.Method, url, DryRun: ctx.DryRun);
        stepLog.Add(stepRecord);

        if (ctx.DryRun)
        {
            ctx.SetVariable(saveAs ?? "result", new { dryRun = true, url, method = tool.Method });
            ctx.LastResult = stepLog.Select(s => new { s.ToolId, s.Method, s.Url, s.DryRun }).Cast<object>().ToArray();
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var client = _httpClientFactory.CreateClient("atlas-exec");

        using var request = new HttpRequestMessage(new HttpMethod(tool.Method), url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ctx.Token);

        if (tool.Method is "POST" or "PUT" or "PATCH" && args.Count > 0)
        {
            request.Content = JsonContent.Create(args);
        }

        var response = await client.SendAsync(request, ct);
        sw.Stop();

        // Read as bytes for accurate size enforcement (body.Length would be character count, not bytes)
        var bodyBytes = await response.Content.ReadAsByteArrayAsync(ct);
        ctx.CheckResponseSize(bodyBytes.Length);

        var stepFinal = stepRecord with { StatusCode = (int)response.StatusCode, DurationMs = sw.ElapsedMilliseconds };
        stepLog[^1] = stepFinal;

        if (!response.IsSuccessStatusCode)
        {
            // Log the full body at a controlled level; do not expose potentially-sensitive content to the caller
            var bodyPreview = bodyBytes.Length > 500
                ? System.Text.Encoding.UTF8.GetString(bodyBytes, 0, 500) + "…"
                : System.Text.Encoding.UTF8.GetString(bodyBytes);
            _logger.LogWarning("Downstream call to {ToolId} returned {StatusCode}. Body (truncated): {Body}",
                toolId, (int)response.StatusCode, bodyPreview);
            var errorMsg = $"Downstream API '{toolId}' returned {(int)response.StatusCode}";
            stepLog[^1] = stepFinal with { Error = errorMsg };
            throw new InvalidOperationException(errorMsg);
        }

        var body = System.Text.Encoding.UTF8.GetString(bodyBytes);

        object? result = null;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try { result = JsonSerializer.Deserialize<object>(body); } catch { result = body; }
        }

        ctx.SetVariable(saveAs ?? "result", result);
        ctx.LastResult = result;
    }

    private async Task ExecuteForeachAsync(JsonElement step, ExecContext ctx, List<McpExecuteStep> stepLog, CancellationToken ct)
    {
        var itemsRef = step.GetProperty("items").GetString()!;
        var asVar = step.GetProperty("as").GetString()!;
        var doSteps = step.GetProperty("do");

        var rawItems = ctx.ResolveVariable(itemsRef);

        // JsonElement arrays are not IEnumerable — handle them explicitly before the generic fallback
        IEnumerable<object?> items;
        if (rawItems is JsonElement je && je.ValueKind == JsonValueKind.Array)
            items = je.EnumerateArray().Cast<object?>();
        else if (rawItems is System.Collections.IEnumerable enumerable and not string)
            items = enumerable.Cast<object?>();
        else
            throw new InvalidOperationException($"Variable '{itemsRef}' is not enumerable");

        foreach (var item in items)
        {
            ctx.SetVariable(asVar, item);
            await ExecuteStepsAsync(doSteps, ctx, stepLog, ct);
        }
    }

    private async Task ExecuteIfAsync(JsonElement step, ExecContext ctx, List<McpExecuteStep> stepLog, CancellationToken ct)
    {
        var condition = step.GetProperty("condition").GetString()!;
        var condResult = EvaluateCondition(condition, ctx);

        if (condResult && step.TryGetProperty("then", out var thenSteps))
            await ExecuteStepsAsync(thenSteps, ctx, stepLog, ct);
        else if (!condResult && step.TryGetProperty("else", out var elseSteps))
            await ExecuteStepsAsync(elseSteps, ctx, stepLog, ct);
    }

    private void ExecuteReturn(JsonElement step, ExecContext ctx)
    {
        var from = step.TryGetProperty("from", out var fromEl) ? fromEl.GetString() : "result";
        ctx.LastResult = ctx.ResolveVariable(from ?? "result");
    }

    private bool EvaluateCondition(string condition, ExecContext ctx)
    {
        condition = condition.Trim();
        if (condition.Contains("!="))
        {
            var parts = condition.Split("!=", 2);
            var val = ctx.ResolveVariable(parts[0].Trim());
            var expected = parts[1].Trim().Trim('"', '\'');
            return val?.ToString() != expected;
        }
        if (condition.Contains("=="))
        {
            var parts = condition.Split("==", 2);
            var val = ctx.ResolveVariable(parts[0].Trim());
            var expected = parts[1].Trim().Trim('"', '\'');
            return val?.ToString() == expected;
        }
        var boolVal = ctx.ResolveVariable(condition);
        return boolVal is true || boolVal?.ToString() == "true";
    }

    private Dictionary<string, object?> SubstituteVariables(Dictionary<string, object?> args, Dictionary<string, object?> variables)
    {
        var result = new Dictionary<string, object?>();
        foreach (var (key, value) in args)
        {
            if (value is string strVal && strVal.Contains("{{"))
            {
                var substituted = strVal;
                foreach (var (varName, varVal) in variables)
                {
                    substituted = substituted.Replace($"{{{{{varName}}}}}", varVal?.ToString() ?? "");
                }
                result[key] = substituted;
            }
            else
            {
                result[key] = value;
            }
        }
        return result;
    }
}

public class ExecContext
{
    private int _steps;
    private int _calls;
    private long _bytesDownloaded;
    private readonly ExecLimitsOptions _limits;

    public bool DryRun { get; }
    public string Token { get; }
    public string? Environment { get; }
    public Dictionary<string, object?> Variables { get; } = new();
    public object? LastResult { get; set; }

    public ExecContext(bool dryRun, ExecLimitsOptions limits, string token, string? environment)
    {
        DryRun = dryRun;
        _limits = limits;
        Token = token;
        Environment = environment;
    }

    public void IncrementSteps(int max)
    {
        if (++_steps > max)
            throw new ExecutionLimitException($"Exceeded max steps ({max})");
    }

    public void IncrementCalls(int max)
    {
        if (++_calls > max)
            throw new ExecutionLimitException($"Exceeded max HTTP calls ({max})");
    }

    /// <summary>
    /// Checks that a single response does not exceed <see cref="ExecLimitsOptions.MaxResponseBytes"/>
    /// and that the cumulative download across the whole plan does not exceed <see cref="ExecLimitsOptions.MaxBytes"/>.
    /// </summary>
    public void CheckResponseSize(long bytes)
    {
        _bytesDownloaded += bytes;
        if (bytes > _limits.MaxResponseBytes)
            throw new ExecutionLimitException($"Response exceeds max response bytes ({_limits.MaxResponseBytes})");
        if (_bytesDownloaded > _limits.MaxBytes)
            throw new ExecutionLimitException($"Total downloaded bytes exceeds limit ({_limits.MaxBytes})");
    }

    public void SetVariable(string name, object? value) => Variables[name] = value;

    public object? ResolveVariable(string path)
    {
        var parts = path.Split('.', 2);
        if (!Variables.TryGetValue(parts[0], out var val))
            return null;

        if (parts.Length == 1)
            return val;

        if (val is System.Text.Json.JsonElement je)
            return NavigateJson(je, parts[1]);

        var prop = val?.GetType().GetProperty(parts[1], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance);
        return prop?.GetValue(val);
    }

    private object? NavigateJson(System.Text.Json.JsonElement element, string path)
    {
        var parts = path.Split('.', 2);
        if (element.TryGetProperty(parts[0], out var prop))
        {
            if (parts.Length == 1)
                return prop.ValueKind == System.Text.Json.JsonValueKind.String ? prop.GetString() : (object)prop;
            return NavigateJson(prop, parts[1]);
        }
        return null;
    }
}

public class ExecutionLimitException : Exception
{
    public ExecutionLimitException(string message) : base(message) { }
}
