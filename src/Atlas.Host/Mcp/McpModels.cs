namespace Atlas.Host.Mcp;

public record McpSearchRequest(
    string? Query = null,
    McpSearchFilters? Filters = null);

public record McpSearchFilters(
    string? ApiId = null,
    string[]? Tags = null,
    string? Safety = null,
    string? AccessFilter = null);

public record McpSearchResult(
    string ToolId,
    string ApiId,
    string DisplayName,
    string Summary,
    string Method,
    string Path,
    string Safety,
    string[] RequiredPermissions,
    string? EntitlementHint = null,
    string? Access = null,
    string[]? MissingPermissions = null);

public record McpDescribeRequest(string ToolId);

public record McpDescribeResult(
    string ToolId,
    string ApiId,
    string DisplayName,
    string Summary,
    string Description,
    string Method,
    string Path,
    string Safety,
    string[] RequiredPermissions,
    string[] Tags,
    string? EntitlementHint = null,
    object? RequestSchema = null,
    object? ResponseSchema = null,
    object? Examples = null);

public record McpExecuteRequest(
    object Plan,
    string Mode = "dryRun",
    string? Environment = null);

public record McpExecuteResult(
    bool Success,
    string Mode,
    object? Result = null,
    string? Error = null,
    McpExecuteStep[]? Steps = null);

public record McpExecuteStep(
    string ToolId,
    string Method,
    string Url,
    int? StatusCode = null,
    long? DurationMs = null,
    string? Error = null,
    bool DryRun = false);
