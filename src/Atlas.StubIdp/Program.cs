using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Generate a stable RSA key for this instance
var rsa = RSA.Create(2048);
var rsaSecurityKey = new RsaSecurityKey(rsa) { KeyId = "stub-key-1" };
var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

var issuer = builder.Configuration["StubIdp:Issuer"] ?? $"http://localhost:{builder.Configuration["ASPNETCORE_HTTP_PORTS"] ?? "5200"}";

// /.well-known/openid-configuration
app.MapGet("/.well-known/openid-configuration", () => Results.Json(new
{
    issuer = issuer,
    jwks_uri = $"{issuer}/.well-known/jwks.json",
    token_endpoint = $"{issuer}/token",
    grant_types_supported = new[] { "client_credentials", "password" },
    response_types_supported = new[] { "token" },
    subject_types_supported = new[] { "public" },
    id_token_signing_alg_values_supported = new[] { "RS256" },
}));

// /.well-known/jwks.json
app.MapGet("/.well-known/jwks.json", () =>
{
    var parameters = rsa.ExportParameters(false);
    var jwk = new
    {
        keys = new[]
        {
            new
            {
                kty = "RSA",
                use = "sig",
                kid = "stub-key-1",
                alg = "RS256",
                n = Base64UrlEncoder.Encode(parameters.Modulus!),
                e = Base64UrlEncoder.Encode(parameters.Exponent!),
            }
        }
    };
    return Results.Json(jwk);
});

// /token - issue tokens
app.MapPost("/token", async (HttpContext ctx) =>
{
    string? clientId = null, subject = null, scopes = null, audience = null;

    if (ctx.Request.HasFormContentType)
    {
        var form = await ctx.Request.ReadFormAsync();
        clientId = form["client_id"].FirstOrDefault();
        subject = form["username"].FirstOrDefault() ?? form["sub"].FirstOrDefault() ?? clientId;
        scopes = form["scope"].FirstOrDefault();
        audience = form["audience"].FirstOrDefault() ?? "api://agent-atlas";
    }
    else
    {
        var body = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(ctx.Request.Body);
        if (body != null)
        {
            body.TryGetValue("client_id", out clientId);
            body.TryGetValue("sub", out subject);
            body.TryGetValue("scope", out scopes);
            body.TryGetValue("audience", out audience);
            if (string.IsNullOrEmpty(audience)) audience = "api://agent-atlas";
        }
    }

    if (string.IsNullOrEmpty(clientId))
        return Results.BadRequest(new { error = "client_id is required" });

    subject ??= clientId;
    scopes ??= "platform-code-mode:search platform-code-mode:execute";

    var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, subject),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("client_id", clientId),
        new Claim("scp", scopes),
    };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: signingCredentials);

    var tokenHandler = new JwtSecurityTokenHandler();
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Json(new
    {
        access_token = tokenString,
        token_type = "Bearer",
        expires_in = 3600,
        scope = scopes,
    });
});

// Health
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy", issuer }));

app.Run();
