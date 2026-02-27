using Atlas.Host.Auth;
using Atlas.Host.Configuration;
using Atlas.Host.Execution;
using Atlas.Host.Models;
using Atlas.Host.Services;
using Microsoft.Extensions.Options;

namespace Atlas.Host.Mcp;

public static class McpEndpoints
{
    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder app)
    {
        var mcp = app.MapGroup("/mcp/v1").RequireAuthorization();

        mcp.MapPost("/search", SearchAsync);
        mcp.MapPost("/describe", DescribeAsync);
        mcp.MapPost("/execute", ExecuteAsync);

        return app;
    }

    private static IResult SearchAsync(
        McpSearchRequest req,
        IToolIndex toolIndex,
        HttpContext ctx,
        IOptions<AtlasOptions> opts)
    {
        if (!PlatformAuth.HasPermission(ctx, opts.Value.PlatformPermissions.Search, opts.Value.PlatformPermissions.Claim))
            return Results.Forbid();

        var tools = toolIndex.Search(req.Query, req.Filters?.ApiId, req.Filters?.Tags, req.Filters?.Safety);

        var callerPerms = PlatformAuth.GetPermissions(ctx, opts.Value.PlatformPermissions.Claim);

        var results = tools.Select(t =>
        {
            string? access = null;
            string[]? missing = null;
            if (req.Filters?.AccessFilter is "granted" or "missing" or "unknown")
            {
                (access, missing) = ComputeAccess(t, callerPerms);
            }
            return new McpSearchResult(
                t.ToolId, t.ApiId, t.DisplayName, t.Summary, t.Method, t.Path,
                t.Safety, t.RequiredPermissions, t.EntitlementHint, access, missing);
        }).ToArray();

        if (req.Filters?.AccessFilter is "granted")
            results = results.Where(r => r.Access == "granted").ToArray();
        else if (req.Filters?.AccessFilter is "missing")
            results = results.Where(r => r.Access == "missing").ToArray();

        return Results.Ok(results);
    }

    private static IResult DescribeAsync(
        McpDescribeRequest req,
        IToolIndex toolIndex,
        HttpContext ctx,
        IOptions<AtlasOptions> opts)
    {
        if (!PlatformAuth.HasPermission(ctx, opts.Value.PlatformPermissions.Search, opts.Value.PlatformPermissions.Claim))
            return Results.Forbid();

        var tool = toolIndex.GetById(req.ToolId);
        if (tool is null)
            return Results.NotFound(new { error = $"Tool '{req.ToolId}' not found" });

        return Results.Ok(new McpDescribeResult(
            tool.ToolId, tool.ApiId, tool.DisplayName, tool.Summary, tool.Description,
            tool.Method, tool.Path, tool.Safety, tool.RequiredPermissions, tool.Tags,
            tool.EntitlementHint, tool.RequestSchema, tool.ResponseSchema, tool.Examples));
    }

    private static async Task<IResult> ExecuteAsync(
        McpExecuteRequest req,
        IToolIndex toolIndex,
        ICatalogLoader catalog,
        IExecutionEngine engine,
        HttpContext ctx,
        IOptions<AtlasOptions> opts)
    {
        if (!PlatformAuth.HasPermission(ctx, opts.Value.PlatformPermissions.Execute, opts.Value.PlatformPermissions.Claim))
            return Results.Forbid();

        var token = ctx.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var result = await engine.ExecuteAsync(req.Plan, req.Mode, req.Environment, token, ctx.RequestAborted);
        return Results.Ok(result);
    }

    private static (string access, string[]? missing) ComputeAccess(ToolDefinition tool, HashSet<string> callerPerms)
    {
        if (tool.RequiredPermissions.Length == 0)
            return ("unknown", null);

        var missing = tool.RequiredPermissions.Where(p => !callerPerms.Contains(p)).ToArray();
        if (missing.Length == 0)
            return ("granted", null);

        return ("missing", missing);
    }
}
