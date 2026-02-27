using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI docs
builder.Services.AddOpenApi("v1");

// WARNING: This sample API is for LOCAL DEVELOPMENT AND DEMO PURPOSES ONLY.
// Token validation is intentionally relaxed so the Aspire demo works without a real IdP.
// In production, you MUST configure proper JWT validation with issuer, audience, and
// signing-key validation. NEVER use these settings in a production environment.
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// OpenAPI + Scalar UI (development)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

// GET /customers - requires someapi:customers:read
app.MapGet("/customers", (HttpContext ctx) =>
{
    // Check for required permission in token (downstream auth)
    var auth = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrEmpty(auth))
        return Results.Unauthorized();

    // In a real API, validate the JWT and check claims
    // For demo purposes, return sample data
    return Results.Ok(new[]
    {
        new { id = "c1", name = "Acme Corp", status = "active" },
        new { id = "c2", name = "Beta Inc", status = "active" },
        new { id = "c3", name = "Gamma Ltd", status = "inactive" },
    });
});

// GET /customers/{id}
app.MapGet("/customers/{id}", (string id, HttpContext ctx) =>
{
    var auth = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrEmpty(auth))
        return Results.Unauthorized();

    var customers = new Dictionary<string, object>
    {
        ["c1"] = new { id = "c1", name = "Acme Corp", status = "active", email = "contact@acme.com" },
        ["c2"] = new { id = "c2", name = "Beta Inc", status = "active", email = "info@beta.com" },
        ["c3"] = new { id = "c3", name = "Gamma Ltd", status = "inactive", email = "hello@gamma.com" },
    };

    if (!customers.TryGetValue(id, out var customer))
        return Results.NotFound();

    return Results.Ok(customer);
});

// Health
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.Run();
