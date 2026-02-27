using Atlas.Host.Configuration;
using Atlas.Host.Models;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Atlas.Host.Services;

public interface ICatalogLoader
{
    Task LoadAsync(CancellationToken ct = default);
    Catalog? GetCatalog();
    IReadOnlyList<ApiEntry> GetApis();
    IReadOnlyList<ToolDefinition> GetTools();
}

public class CatalogLoader : ICatalogLoader
{
    private readonly AtlasOptions _options;
    private readonly ILogger<CatalogLoader> _logger;
    private Catalog? _catalog;
    private readonly List<ApiEntry> _apis = new();
    private readonly List<ToolDefinition> _tools = new();

    public CatalogLoader(IOptions<AtlasOptions> options, ILogger<CatalogLoader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var catalogPath = _options.CatalogPath;
        _logger.LogInformation("Loading catalog from {CatalogPath}", catalogPath);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        // Load catalog.yaml
        var catalogFile = Path.Combine(catalogPath, "catalog.yaml");
        if (!File.Exists(catalogFile))
        {
            if (_options.CatalogStrict)
                throw new InvalidOperationException($"catalog.yaml not found at {catalogFile}");
            _logger.LogWarning("catalog.yaml not found, using empty catalog");
            _catalog = new Catalog { OrganizationName = "Unknown" };
        }
        else
        {
            var catalogYaml = await File.ReadAllTextAsync(catalogFile, ct);
            _catalog = deserializer.Deserialize<Catalog>(catalogYaml);
        }

        // Load APIs
        var apisDir = Path.Combine(catalogPath, "apis");
        if (!Directory.Exists(apisDir))
        {
            _logger.LogWarning("No apis directory found at {ApisDir}", apisDir);
            return;
        }

        foreach (var apiDir in Directory.GetDirectories(apisDir))
        {
            try
            {
                await LoadApiAsync(apiDir, deserializer, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load API from {ApiDir}", apiDir);
                if (_options.CatalogStrict) throw;
            }
        }

        _logger.LogInformation("Loaded {ApiCount} APIs and {ToolCount} tools", _apis.Count, _tools.Count);
    }

    private async Task LoadApiAsync(string apiDir, IDeserializer deserializer, CancellationToken ct)
    {
        var apiYamlFile = Path.Combine(apiDir, "api.yaml");
        if (!File.Exists(apiYamlFile))
        {
            _logger.LogWarning("No api.yaml in {ApiDir}, skipping", apiDir);
            return;
        }

        var apiYaml = await File.ReadAllTextAsync(apiYamlFile, ct);
        var apiEntry = deserializer.Deserialize<ApiEntry>(apiYaml);
        _apis.Add(apiEntry);

        // Load OpenAPI spec
        var openApiFile = Path.Combine(apiDir, "openapi.yaml");
        if (!File.Exists(openApiFile))
        {
            openApiFile = Path.Combine(apiDir, "openapi.json");
        }

        if (!File.Exists(openApiFile))
        {
            _logger.LogWarning("No openapi.yaml/json in {ApiDir}", apiDir);
            return;
        }

        await using var stream = File.OpenRead(openApiFile);
        var reader = new OpenApiStreamReader();
        var result = await reader.ReadAsync(stream, ct);

        if (result.OpenApiDiagnostic.Errors.Count > 0)
        {
            foreach (var error in result.OpenApiDiagnostic.Errors)
                _logger.LogError("OpenAPI parse error in {File}: {Error}", openApiFile, error.Message);
            if (_options.CatalogStrict)
                throw new InvalidOperationException($"OpenAPI parsing errors in {openApiFile}");
        }

        ExtractTools(result.OpenApiDocument, apiEntry);
    }

    private void ExtractTools(OpenApiDocument doc, ApiEntry apiEntry)
    {
        foreach (var path in doc.Paths)
        {
            foreach (var op in path.Value.Operations)
            {
                if (!op.Value.Extensions.TryGetValue("x-mcp", out var mcpExt))
                    continue;

                if (mcpExt is not Microsoft.OpenApi.Any.OpenApiObject mcpObj)
                    continue;

                if (!mcpObj.TryGetValue("enabled", out var enabledVal))
                    continue;

                if (enabledVal is not Microsoft.OpenApi.Any.OpenApiBoolean enabledBool || !enabledBool.Value)
                    continue;

                // Extract tool name
                string toolId;
                if (mcpObj.TryGetValue("name", out var nameVal) && nameVal is Microsoft.OpenApi.Any.OpenApiString nameStr)
                    toolId = nameStr.Value;
                else
                    toolId = op.Value.OperationId ?? $"{apiEntry.ApiId}.{op.Key}.{path.Key}".Replace("/", "_");

                // Extract safety
                string safety = "read";
                if (mcpObj.TryGetValue("safety", out var safetyVal) && safetyVal is Microsoft.OpenApi.Any.OpenApiString safetyStr)
                    safety = safetyStr.Value;

                // Extract requiredPermissions
                string[] requiredPerms = [];
                if (mcpObj.TryGetValue("requiredPermissions", out var permsVal) && permsVal is Microsoft.OpenApi.Any.OpenApiArray permsArr)
                {
                    requiredPerms = permsArr
                        .OfType<Microsoft.OpenApi.Any.OpenApiString>()
                        .Select(s => s.Value)
                        .ToArray();
                }

                // x-mcp.requiredPermissions is required for catalog hygiene (informational; Atlas does not
                // enforce downstream permissions). In strict mode this is a warning; a future policy option
                // could treat it as a fatal validation error.
                if (requiredPerms.Length == 0)
                {
                    _logger.LogWarning("Tool {ToolId} missing x-mcp.requiredPermissions (catalog hygiene warning)", toolId);
                    if (_options.CatalogStrict)
                        _logger.LogWarning("Set Atlas:CatalogStrict=false to suppress this warning, or add x-mcp.requiredPermissions to the operation");
                }

                // Extract description override
                string summary = op.Value.Summary ?? string.Empty;
                string description = op.Value.Description ?? string.Empty;
                if (mcpObj.TryGetValue("description", out var descVal) && descVal is Microsoft.OpenApi.Any.OpenApiString descStr)
                    description = descStr.Value;

                // Extract tags
                var tags = new List<string>();
                tags.AddRange(op.Value.Tags.Select(t => t.Name));
                if (mcpObj.TryGetValue("tags", out var tagsVal) && tagsVal is Microsoft.OpenApi.Any.OpenApiArray tagsArr)
                {
                    tags.AddRange(tagsArr.OfType<Microsoft.OpenApi.Any.OpenApiString>().Select(s => s.Value));
                }

                // Extract entitlementHint
                string? entitlementHint = null;
                if (mcpObj.TryGetValue("entitlementHint", out var hintVal) && hintVal is Microsoft.OpenApi.Any.OpenApiString hintStr)
                    entitlementHint = hintStr.Value;

                var tool = new ToolDefinition
                {
                    ToolId = toolId,
                    ApiId = apiEntry.ApiId,
                    DisplayName = summary.Length > 0 ? summary : toolId,
                    Summary = summary,
                    Description = description,
                    Method = op.Key.ToString().ToUpperInvariant(),
                    Path = path.Key,
                    Safety = safety,
                    RequiredPermissions = requiredPerms,
                    Tags = [..tags],
                    EntitlementHint = entitlementHint,
                    OperationId = op.Value.OperationId ?? string.Empty,
                };

                _tools.Add(tool);
                _logger.LogDebug("Extracted tool {ToolId} from {ApiId}", toolId, apiEntry.ApiId);
            }
        }
    }

    public Catalog? GetCatalog() => _catalog;
    public IReadOnlyList<ApiEntry> GetApis() => _apis;
    public IReadOnlyList<ToolDefinition> GetTools() => _tools;
}
