var builder = DistributedApplication.CreateBuilder(args);

// Stub IdP
var idp = builder.AddProject<Projects.Atlas_StubIdp>("stub-idp")
    .WithExternalHttpEndpoints();

// Sample APIs
var sampleApiToolEnabled = builder.AddProject<Projects.SampleApi_ToolEnabled>("sample-api-tool-enabled")
    .WithExternalHttpEndpoints();

var sampleApiNotToolEnabled = builder.AddProject<Projects.SampleApi_NotToolEnabled>("sample-api-not-tool-enabled")
    .WithExternalHttpEndpoints();

// Atlas Host
var atlasHost = builder.AddProject<Projects.Atlas_Host>("atlas-host")
    .WithExternalHttpEndpoints()
    .WithEnvironment("Atlas__CatalogPath", "/catalog-data")
    .WithEnvironment("Atlas__Oidc__Issuer", idp.GetEndpoint("http"));

builder.Build().Run();
