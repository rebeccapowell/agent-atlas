namespace Atlas.Host.Mcp;

/// <summary>Execution result returned by the plan engine and surfaced via the MCP execute tool.</summary>
public record McpExecuteResult(
    bool Success,
    string Mode,
    object? Result = null,
    string? Error = null,
    McpExecuteStep[]? Steps = null);

/// <summary>Per-step metadata within an <see cref="McpExecuteResult"/>.</summary>
public record McpExecuteStep(
    string ToolId,
    string Method,
    string Url,
    int? StatusCode = null,
    long? DurationMs = null,
    string? Error = null,
    bool DryRun = false);
