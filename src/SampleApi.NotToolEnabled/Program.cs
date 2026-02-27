using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI docs
builder.Services.AddOpenApi("v1");

var app = builder.Build();

// OpenAPI + Scalar UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// These endpoints are NOT marked as tools in the catalog
app.MapGet("/products", () => Results.Ok(new[]
{
    new { id = "p1", name = "Widget A", price = 9.99 },
    new { id = "p2", name = "Widget B", price = 19.99 },
}));

app.MapGet("/products/{id}", (string id) =>
{
    var products = new Dictionary<string, object>
    {
        ["p1"] = new { id = "p1", name = "Widget A", price = 9.99, description = "A simple widget" },
        ["p2"] = new { id = "p2", name = "Widget B", price = 19.99, description = "A premium widget" },
    };

    if (!products.TryGetValue(id, out var product))
        return Results.NotFound();

    return Results.Ok(product);
});

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.Run();
