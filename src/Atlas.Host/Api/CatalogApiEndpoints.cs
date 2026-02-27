using Atlas.Host.Services;

namespace Atlas.Host.Api;

public static class CatalogApiEndpoints
{
    public static IEndpointRouteBuilder MapCatalogApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/v1").RequireAuthorization();

        api.MapGet("/apis", (ICatalogLoader catalog) => Results.Ok(catalog.GetApis()));
        api.MapGet("/tools", (ICatalogLoader catalog) => Results.Ok(catalog.GetTools()));
        api.MapGet("/tools/{toolId}", (string toolId, IToolIndex index) =>
        {
            var tool = index.GetById(toolId);
            return tool is null ? Results.NotFound() : Results.Ok(tool);
        });

        return app;
    }
}
