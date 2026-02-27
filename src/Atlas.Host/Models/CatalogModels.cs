namespace Atlas.Host.Models;

public class Catalog
{
    public int Version { get; set; } = 1;
    public string OrganizationName { get; set; } = string.Empty;
    public string? DefaultSafetyTierPolicy { get; set; }
}

public class ApiEntry
{
    public string ApiId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Environments { get; set; } = new();
    public string BaseUrl { get; set; } = string.Empty;
}

public class ToolDefinition
{
    public string ToolId { get; set; } = string.Empty;
    public string ApiId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Safety { get; set; } = "read";
    public string[] RequiredPermissions { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public string? EntitlementHint { get; set; }
    public object? RequestSchema { get; set; }
    public object? ResponseSchema { get; set; }
    public object? Examples { get; set; }
    public object? Pagination { get; set; }
    public string OperationId { get; set; } = string.Empty;
}
