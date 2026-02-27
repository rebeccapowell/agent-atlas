using Atlas.Host.Api;
using Atlas.Host.Configuration;
using Atlas.Host.Execution;
using Atlas.Host.Mcp;
using Atlas.Host.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<AtlasOptions>(builder.Configuration.GetSection("Atlas"));

// Auth
var atlasOpts = builder.Configuration.GetSection("Atlas").Get<AtlasOptions>() ?? new AtlasOptions();

if (!string.IsNullOrEmpty(atlasOpts.Oidc.Issuer))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = atlasOpts.Oidc.Issuer;
            options.Audience = atlasOpts.Oidc.Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuers = atlasOpts.Oidc.AllowedIssuers.Length > 0
                    ? atlasOpts.Oidc.AllowedIssuers
                    : [atlasOpts.Oidc.Issuer],
                ValidAudience = atlasOpts.Oidc.Audience,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            options.RequireHttpsMetadata = !atlasOpts.Oidc.Issuer.StartsWith("http://");
        });
}
else
{
    // No OIDC configured - allow anonymous for dev
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

// Catalog
builder.Services.AddSingleton<ICatalogLoader, CatalogLoader>();
builder.Services.AddSingleton<IToolIndex, ToolIndex>();

// Execution
builder.Services.AddSingleton<IExecutionEngine, ExecutionEngine>();
builder.Services.AddHttpClient("atlas-exec");

// HTTP context accessor - required for MCP tools to read caller claims
builder.Services.AddHttpContextAccessor();

// MCP server using the official ModelContextProtocol.AspNetCore SDK (Streamable HTTP transport)
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<AtlasMcpTools>();

// CORS for UI - always restrict to localhost in development; use Atlas:Cors:AllowedOrigins in production
var allowedOrigins = builder.Configuration.GetSection("Atlas:Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(opts => opts.AddDefaultPolicy(policy =>
{
    if (allowedOrigins is { Length: > 0 })
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    else if (builder.Environment.IsDevelopment())
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5000")
              .AllowAnyHeader().AllowAnyMethod();
    else
        // No origins configured in production - deny cross-origin requests by default
        policy.AllowAnyHeader().AllowAnyMethod();
}));

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Load catalog on startup
var loader = app.Services.GetRequiredService<ICatalogLoader>();
await loader.LoadAsync();

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Health
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

// MCP server endpoint (Streamable HTTP transport - used by AI agents / Claude / etc.)
// RequireAuthorization() ensures JWT validation before any tool call is processed.
app.MapMcp("/mcp").RequireAuthorization();

// Catalog REST API (used by the UI and direct API clients)
app.MapCatalogApiEndpoints();

// Serve React UI from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapFallbackToFile("index.html");

app.Run();
