# Copilot Instructions for Agent Atlas

## Project Overview

Agent Atlas is an enterprise **MCP (Model Context Protocol) gateway** built with ASP.NET Core 10 and .NET Aspire. It turns a messy internal API estate into a governed, searchable catalog of MCP tools that both humans and AI agents can discover and invoke safely.

- **Catalog**: Reads OpenAPI specs from a GitOps data-plane repository. Teams annotate operations with `x-mcp` vendor extensions to mark them as agent tools.
- **MCP Server**: Exposes three MCP tools — `SearchTools`, `DescribeTool`, `ExecutePlan` — via Streamable HTTP transport at `/mcp`.
- **Execution Engine**: Interprets a JSON plan DSL (`call`, `foreach`, `if`, `return` steps) and proxies requests to downstream APIs, forwarding the caller's JWT.
- **React UI**: A read-only capability map for developers and operators (served from `wwwroot`).

## Repository Layout

```
src/
  Atlas.AppHost/         # .NET Aspire orchestration host (local dev entry point)
  Atlas.Host/            # Main service: MCP server, catalog API, execution engine, React UI
  Atlas.StubIdp/         # Lightweight RSA JWT issuer for offline/CI dev (no Keycloak needed)
  SampleApi.ToolEnabled/ # Demo API whose operations ARE registered as MCP tools
  SampleApi.NotToolEnabled/ # Demo API intentionally NOT registered as tools
catalog/                 # GitOps data-plane: catalog.yaml, apis/, policies/
docs/                    # Architecture and deployment documentation
helm/                    # Kubernetes / Helm chart
```

## Tech Stack

- **.NET 10** / **ASP.NET Core 10**
- **.NET Aspire 13.x** (orchestration, service discovery)
- **ModelContextProtocol.AspNetCore 1.0.0** (MCP SDK)
- **Keycloak** (OIDC provider for local dev, via Docker)
- **React** (UI, pre-built into `wwwroot`)
- **YamlDotNet** (catalog YAML parsing)
- **Microsoft.OpenApi.Readers** (OpenAPI spec loading)

## Build

```bash
# Build entire solution
dotnet build src/Atlas.AppHost/Atlas.AppHost.csproj

# Run with full Aspire stack (requires Docker for Keycloak)
dotnet run --project src/Atlas.AppHost

# Run without Docker (StubIdp fallback)
Atlas__CatalogPath=$(pwd)/catalog \
Atlas__Oidc__Issuer=http://localhost:5200 \
dotnet run --project src/Atlas.Host
```

## Testing

- **Framework**: xUnit with **Shouldly** (assertions) and **NSubstitute** (mocking). Use **AutoFixture** when fixture generation is beneficial.
- **Banned libraries**: Moq and FluentAssertions are **not permitted** in this repository. Use NSubstitute for mocking and Shouldly for assertions to keep the test style consistent.
- Test projects follow the naming convention `<ProjectName>.Tests`.
- Run all tests with: `dotnet test`

## Key Coding Conventions

- **Nullable reference types** are enabled (`<Nullable>enable</Nullable>`); always handle nullability correctly.
- **Implicit usings** are enabled.
- Keep `Atlas__` configuration sections in `appsettings.json`; see `AtlasOptions` for the full configuration model.
- The `/mcp` endpoint always requires JWT authorization (`RequireAuthorization()`). The `Atlas__Mcp__AllowAnonymous` flag is only for local dev and bypasses both endpoint auth and tool permission checks — never expose this in production.
- Catalog read endpoints (`/v1/apis`, `/v1/tools`) are intentionally `AllowAnonymous` (discovery/read is open).
- Platform permission checks (`platform-code-mode:search`, `platform-code-mode:execute`) are enforced inside `AtlasMcpTools` via `PlatformAuth` + `IHttpContextAccessor`.
- Use `ICatalogLoader` and `IToolIndex` abstractions; avoid depending on concrete implementations in new code.
- The execution engine enforces limits via `Atlas__ExecLimits__*` configuration; always respect these in plan execution code.

## Configuration Reference

| Variable | Default | Purpose |
|---|---|---|
| `Atlas__CatalogPath` | `/catalog` | Path to GitOps data-plane repo |
| `Atlas__Oidc__Issuer` | *(required)* | OIDC issuer URL |
| `Atlas__Oidc__Audience` | `api://agent-atlas` | Expected JWT audience |
| `Atlas__PlatformPermissions__Claim` | `scp` | JWT claim for platform permissions |
| `Atlas__ExecLimits__MaxSteps` | `50` | Max plan steps |
| `Atlas__ExecLimits__MaxCalls` | `50` | Max downstream HTTP calls per plan |
| `Atlas__ExecLimits__MaxSeconds` | `30` | Plan execution timeout |
| `Atlas__ExecLimits__MaxBytes` | `10485760` | Max cumulative response bytes |
| `Atlas__Mcp__AllowAnonymous` | `false` | Bypass auth (local dev only) |

## MCP Tool Catalog (`x-mcp` extension)

Teams annotate OpenAPI operations to publish them as MCP tools:

```yaml
paths:
  /customers:
    get:
      operationId: ListCustomers
      x-mcp:
        enabled: true
        name: "my-api.customers.list"
        safety: read                   # read | write | destructive
        requiredPermissions:
          - "my-api:customers:read"
        entitlementHint: "Request access via the My API - Readers access package"
        tags: [customers, list]
```
