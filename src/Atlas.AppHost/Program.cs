var builder = DistributedApplication.CreateBuilder(args);

// Catalog directory - points to the repo's catalog/ directory during local development
var catalogPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "catalog"));

// Keycloak identity provider with atlas realm (replaces Atlas.StubIdp for local dev)
var keycloak = builder.AddKeycloak("keycloak")
    .WithRealmImport(Path.Combine(builder.AppHostDirectory, "keycloak"))
    .WithExternalHttpEndpoints();

// Sample APIs
var sampleApiToolEnabled = builder.AddProject<Projects.SampleApi_ToolEnabled>("sample-api-tool-enabled")
    .WithExternalHttpEndpoints();

var sampleApiNotToolEnabled = builder.AddProject<Projects.SampleApi_NotToolEnabled>("sample-api-not-tool-enabled")
    .WithExternalHttpEndpoints();

// Atlas Host - JWT issuer is the Keycloak "atlas" realm endpoint
var atlasHost = builder.AddProject<Projects.Atlas_Host>("atlas-host")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Atlas__CatalogPath", catalogPath)
    .WithEnvironment("Atlas__PlatformPermissions__Claim", "scope")
    .WithEnvironment("Atlas__Oidc__Issuer",
        ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/atlas"))
    .WaitFor(keycloak);

builder.Build().Run();
