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

## Build and Run

> **IMPORTANT — trust the HTTPS dev certificate first.**
> Aspire uses a local HTTPS certificate for service-to-service communication via DCP
> (its internal process manager). If the certificate is not trusted, the DCP handshake
> times out after 20 seconds and the AppHost fails to start. Always run the three cert
> commands below once before the first `aspire run` in any new environment.

```bash
# 1. Trust the HTTPS development certificate (required once per environment)
dotnet dev-certs https --clean
dotnet dev-certs https
dotnet dev-certs https --trust

# 2. Build entire solution
dotnet build src/Atlas.AppHost/Atlas.AppHost.csproj

# 3. Run with Aspire (preferred — starts all services with Keycloak, MCP Inspector, OTel)
aspire run --project src/Atlas.AppHost

# Run without Docker (StubIdp fallback)
Atlas__CatalogPath=$(pwd)/catalog \
Atlas__Oidc__Issuer=http://localhost:5200 \
aspire run --project src/Atlas.Host
```

> **Prefer `aspire run` over `dotnet run`** when starting any project.
> The Aspire CLI wires service discovery, injects environment variables, and makes
> OTel data visible through the Aspire dashboard and the Aspire MCP server.

## Running Atlas.Host for Screenshots (Agent / CI Environments)

In the GitHub Copilot agent sandbox (and any headless CI context) the full Aspire
AppHost requires Docker for Keycloak and MCP Inspector images, which may not be
available or may take too long to pull. For screenshot and UI documentation tasks,
**run Atlas.Host directly** — no Docker, no Keycloak, no OIDC required:

```bash
# Build Atlas.Host if not already done
dotnet build src/Atlas.Host/Atlas.Host.csproj --no-restore

# Start Atlas.Host standalone — UI ready at http://localhost:5063 within ~3 s
Atlas__CatalogPath=$(pwd)/catalog \
Atlas__Mcp__AllowAnonymous=true \
dotnet run --project src/Atlas.Host --no-build &
APP_PID=$!

sleep 4
curl -sf http://localhost:5063/healthz   # should return "Healthy"

# Use the Playwright MCP to take screenshots, then stop the app:
# kill $APP_PID
```

### Why this works in the agent environment

| Fact | Detail |
|------|--------|
| `Atlas__CatalogPath=$(pwd)/catalog` | Uses the bundled sample catalog already in the repo |
| `Atlas__Mcp__AllowAnonymous=true` | Bypasses all OIDC auth — no Keycloak token needed |
| `dotnet run` reads `launchSettings.json` | The `http` profile specifies `applicationUrl: http://localhost:5063`. When using `dotnet run`, launchSettings.json takes precedence over `ASPNETCORE_URLS` — do not rely on the env var |
| React UI pre-built in `wwwroot/` | Served as static files; no Node.js build step needed |
| `/v1/apis` and `/v1/tools` are `AllowAnonymous` | The UI loads tool/API data without any Bearer token |

### Screenshot targets

After the app is running, use the **Playwright MCP** to capture:

| File | Page | Mode |
|------|------|------|
| `docs/screenshots/01-tools-list-light.png` | Tools tab | Light |
| `docs/screenshots/02-tool-detail-light.png` | Tools tab — detail panel open | Light |
| `docs/screenshots/03-apis-list-light.png` | APIs tab | Light |
| `docs/screenshots/04-tools-list-dark.png` | Tools tab | Dark |
| `docs/screenshots/05-apis-list-dark.png` | APIs tab | Dark |
| `docs/screenshots/06-tool-detail-dark.png` | Tools tab — detail panel open | Dark |
| `docs/screenshots/07-use-mcp-light.png` | Use MCP tab | Light |
| `docs/screenshots/07-use-mcp-dark.png` | Use MCP tab | Dark |
| `docs/screenshots/08-about-dark.png` | About tab | Dark |

Toggle dark mode with the moon/sun icon in the navigation bar.

---

## Waiting for Aspire Resources to Start

> **This section applies when running the full AppHost stack with Docker.**
> For screenshot tasks in agent/CI environments prefer the Atlas.Host standalone
> approach above — it starts in seconds and needs no Docker.

Aspire starts several services that may take time to become ready, especially on first run
when Docker images for Keycloak and MCP Inspector must be pulled.

**Always use the `aspire` MCP server to verify resource readiness before navigating.**
Do not use Playwright to open any URL until the relevant resource shows `Running` state.

```
# Typical startup sequence (poll with aspire MCP until all show "Running"):
#   keycloak          — pulls quay.io/keycloak image; may take 2–5 min on first run
#   sample-api-tool-enabled
#   sample-api-not-tool-enabled
#   atlas-host        — waits for keycloak; starts last
#   mcp-inspector     — pulls ghcr.io mcp-inspector image
```

Workflow for tasks that need a running application:
1. Start Aspire in the background: `aspire run --project src/Atlas.AppHost`
2. Poll the **aspire MCP** `getResources` (or equivalent) tool repeatedly — wait until
   `atlas-host` **and** `mcp-inspector` both report `Running` before proceeding.
3. Use the **aspire MCP** to retrieve the URL for each resource (endpoints vary per run).
4. Only then use the **playwright MCP** to navigate and interact with the UI.

## MCP Tools Available to the Coding Agent

Two MCP servers are configured in `.copilot/mcp-config.json`:

| Server | Command | Capabilities |
|--------|---------|--------------|
| **aspire** | `aspire mcp start` | Read OTel traces/logs/metrics, list running resources, get resource URLs, inspect structured application data from the running Aspire application |
| **playwright** | `npx @playwright/mcp` | Navigate and interact with the running React UI in a browser — useful for verifying UI changes and end-to-end behaviour |

Use the **aspire** MCP server to inspect OTel telemetry and resource URLs instead of
reading log files directly. Use the **playwright** MCP server to navigate the Atlas UI
at the URL provided by the Aspire dashboard.

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
