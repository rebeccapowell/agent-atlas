namespace Atlas.Host.Services;

using Atlas.Host.Models;

public interface IToolIndex
{
    IReadOnlyList<ToolDefinition> Search(string? query, string? apiId, string[]? tags, string? safety);
    ToolDefinition? GetById(string toolId);
}

public class ToolIndex : IToolIndex
{
    private readonly ICatalogLoader _loader;

    public ToolIndex(ICatalogLoader loader)
    {
        _loader = loader;
    }

    public IReadOnlyList<ToolDefinition> Search(string? query, string? apiId, string[]? tags, string? safety)
    {
        var tools = _loader.GetTools().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(apiId))
            tools = tools.Where(t => t.ApiId == apiId);

        if (!string.IsNullOrWhiteSpace(safety))
            tools = tools.Where(t => t.Safety == safety);

        if (tags?.Length > 0)
            tools = tools.Where(t => tags.Any(tag => t.Tags.Contains(tag)));

        if (!string.IsNullOrWhiteSpace(query))
        {
            tools = tools.Where(t =>
                t.ToolId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Summary.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        return tools.ToList();
    }

    public ToolDefinition? GetById(string toolId)
        => _loader.GetTools().FirstOrDefault(t => t.ToolId == toolId);
}
