using Atlas.Host.Services;

namespace Atlas.Host.Api;

public static class CatalogApiEndpoints
{
    public static IEndpointRouteBuilder MapCatalogApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/v1");

        api.MapGet("/apis", (ICatalogLoader catalog) => Results.Ok(catalog.GetApis())).AllowAnonymous();
        api.MapGet("/tools", (ICatalogLoader catalog) => Results.Ok(catalog.GetTools())).AllowAnonymous();
        api.MapGet("/tools/{toolId}", (string toolId, IToolIndex index) =>
        {
            var tool = index.GetById(toolId);
            return tool is null ? Results.NotFound() : Results.Ok(tool);
        }).AllowAnonymous();

        return app;
    }
}
