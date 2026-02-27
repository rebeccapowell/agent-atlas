using Atlas.Host.Auth;
using Atlas.Host.Configuration;
using Atlas.Host.Execution;
using Atlas.Host.Models;
using Atlas.Host.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Atlas.Host.Mcp;

/// <summary>
/// MCP server tools for the Atlas tool catalog. Exposes search, describe, and execute
/// as MCP-protocol tools using the official ModelContextProtocol.AspNetCore SDK.
/// </summary>
[McpServerToolType]
public sealed class AtlasMcpTools(
    IToolIndex toolIndex,
    IExecutionEngine executionEngine,
    IHttpContextAccessor httpContextAccessor,
    IOptions<AtlasOptions> options)
{
    private AtlasOptions Opts => options.Value;

    /// <summary>Returns the current caller's platform permissions from the JWT.</summary>
    private HashSet<string> CallerPerms =>
        PlatformAuth.GetPermissions(httpContextAccessor.HttpContext!, Opts.PlatformPermissions.Claim);

    /// <summary>Asserts the caller has the given platform permission; throws if not.</summary>
    private void RequirePlatformPermission(string permission)
    {
        // When anonymous MCP access is enabled (local dev / MCP Inspector) and the caller
        // has no JWT, skip permission enforcement so the inspector can exercise all tools.
        if (Opts.Mcp.AllowAnonymous)
        {
            var isAuthenticated = httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated;
            if (isAuthenticated == false || isAuthenticated == null)
                return;
        }

        if (!CallerPerms.Contains(permission))
            throw new UnauthorizedAccessException(
                $"Missing platform permission '{permission}'. " +
                $"Ensure your token contains this value in the '{Opts.PlatformPermissions.Claim}' claim.");
    }

    [McpServerTool]
    [Description(
        "Search the Agent Atlas tool catalog. Returns matching tools with their metadata " +
        "including method, path, safety level, and required downstream permissions (informational).")]
    public IReadOnlyList<object> SearchTools(
        [Description("Optional free-text query to filter by tool name, summary, or tags.")] string? query = null,
        [Description("Optional API ID to restrict results to a specific registered API.")] string? apiId = null,
        [Description("Optional safety filter: 'read', 'write', or 'destructive'.")] string? safety = null,
        [Description("Optional access filter: 'all', 'granted', or 'missing' (requires JWT claims).")] string? accessFilter = null)
    {
        RequirePlatformPermission(Opts.PlatformPermissions.Search);

        var tools = toolIndex.Search(query, apiId, null, safety);
        var perms = CallerPerms;

        // Compute access status per tool, then apply access filter before final projection
        var enriched = tools.Select(t =>
        {
            string? access = null;
            string[]? missingPerms = null;

            if (accessFilter is "granted" or "missing" or "unknown")
                (access, missingPerms) = ComputeAccess(t, perms);

            return (tool: t, access, missingPerms);
        });

        if (accessFilter is "granted")
            enriched = enriched.Where(r => r.access == "granted");
        else if (accessFilter is "missing")
            enriched = enriched.Where(r => r.access == "missing");

        return enriched.Select(r => (object)new
        {
            toolId = r.tool.ToolId,
            apiId = r.tool.ApiId,
            displayName = r.tool.DisplayName,
            summary = r.tool.Summary,
            method = r.tool.Method,
            path = r.tool.Path,
            safety = r.tool.Safety,
            requiredPermissions = r.tool.RequiredPermissions,
            entitlementHint = r.tool.EntitlementHint,
            tags = r.tool.Tags,
            access = r.access,
            missingPermissions = r.missingPerms,
        }).ToList();
    }

    [McpServerTool]
    [Description(
        "Get full metadata for a specific tool including its request/response schema, " +
        "required downstream permissions, and access entitlement hints.")]
    public object DescribeTool(
        [Description("The stable tool ID (e.g. 'sample-api.customers.list').")] string toolId)
    {
        RequirePlatformPermission(Opts.PlatformPermissions.Search);

        var tool = toolIndex.GetById(toolId)
            ?? throw new KeyNotFoundException($"Tool '{toolId}' not found in the catalog.");

        return new
        {
            toolId = tool.ToolId,
            apiId = tool.ApiId,
            displayName = tool.DisplayName,
            summary = tool.Summary,
            description = tool.Description,
            method = tool.Method,
            path = tool.Path,
            safety = tool.Safety,
            requiredPermissions = tool.RequiredPermissions,
            tags = tool.Tags,
            entitlementHint = tool.EntitlementHint,
            requestSchema = tool.RequestSchema,
            responseSchema = tool.ResponseSchema,
            examples = tool.Examples,
        };
    }

    [McpServerTool]
    [Description(
        "Execute an Atlas plan against one or more downstream API tools. " +
        "Use mode='dryRun' to validate and preview the execution plan without making HTTP calls, " +
        "or mode='run' to execute it. " +
        "The caller's JWT is forwarded to downstream APIs; Atlas does not enforce downstream permissions.")]
    public async Task<object> ExecutePlan(
        [Description(
            "The execution plan as a JSON object. Supports 'call', 'foreach', 'if', and 'return' steps. " +
            "Example: {\"steps\":[{\"type\":\"call\",\"toolId\":\"sample-api.customers.list\",\"args\":{},\"saveAs\":\"customers\"}]}"
        )] object plan,
        [Description("Execution mode: 'dryRun' (validate only) or 'run' (execute).")] string mode = "dryRun",
        [Description("Optional environment name to select the base URL (e.g. 'production').")] string? environment = null)
    {
        RequirePlatformPermission(Opts.PlatformPermissions.Execute);

        var token = httpContextAccessor.HttpContext?.Request.Headers.Authorization
            .ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            ?? string.Empty;

        var ct = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var result = await executionEngine.ExecuteAsync(plan, mode, environment, token, ct);

        return new
        {
            success = result.Success,
            mode = result.Mode,
            result = result.Result,
            error = result.Error,
            steps = result.Steps?.Select(s => new
            {
                s.ToolId, s.Method, s.Url, s.StatusCode, s.DurationMs, s.Error, s.DryRun,
            }),
        };
    }

    // ---------- helpers ----------

    private static (string access, string[]? missing) ComputeAccess(
        ToolDefinition tool, HashSet<string> callerPerms)
    {
        if (tool.RequiredPermissions.Length == 0)
            return ("unknown", null);

        var missing = tool.RequiredPermissions.Where(p => !callerPerms.Contains(p)).ToArray();
        return missing.Length == 0 ? ("granted", null) : ("missing", missing);
    }
}
