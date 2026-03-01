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
```

> **Prefer `aspire run` over `dotnet run`** when starting any project.
> The Aspire CLI wires service discovery, injects environment variables, and makes
> OTel data visible through the Aspire dashboard and the Aspire MCP server.

## GitHub Copilot Agent Environment

The `.github/workflows/copilot-setup-steps.yml` file runs before every Copilot agent
session and pre-provisions the sandbox. **You do not need to repeat any of these steps
— they are already done when your session starts.**

### What is already set up

| What | Detail |
|------|--------|
| **.NET 10 SDK** | Installed via `actions/setup-dotnet@v4` |
| **NuGet packages** | Restored via `dotnet restore AgentAtlas.slnx` — use `--no-restore` on all `dotnet build` / `dotnet run` calls |
| **Aspire CLI** | Installed from `https://aspire.dev/install.sh` and on `$PATH` — `aspire run` and `aspire mcp start` work immediately |
| **HTTPS dev cert** | Generated and trusted as far as Linux allows (`dotnet dev-certs https --trust || true`). Non-zero exit from the trust step is expected on headless runners and is safe to ignore. |
| **Node.js 20** | Installed via `actions/setup-node@v4` |
| **Playwright + Chromium** | Installed via `npx playwright install --with-deps chromium` — the Playwright MCP can open a browser without any additional setup |
| **`.copilot/mcp-config.json`** | Already committed to the repo — **do not run `aspire agent init` or `aspire mcp init`**; doing so would overwrite the hand-tuned config |

### Aspire CLI version constraints

The installed Aspire CLI is **13.1.x**. Be aware of these version-specific limitations:

- **`aspire mcp start`** — correct command to start the Aspire MCP server ✓
- **`aspire agent mcp`** — does **not** exist in 13.1.x (main-branch only); do not attempt
- **`aspire agent init`** — does **not** exist in 13.1.x; do not attempt
- **`aspire mcp init --non-interactive`** — crashes in 13.1.2 with an unhandled exception even with the flag; do not attempt

### Practical build commands in agent sessions

```bash
# Build a single project (fast — packages already restored)
dotnet build src/Atlas.Host/Atlas.Host.csproj --no-restore

# Build entire solution
dotnet build AgentAtlas.slnx --no-restore

# Run tests
dotnet test src/Atlas.Host.Tests/ --no-build

# Run Atlas.Host for UI SCREENSHOTS ONLY (no auth — /mcp is non-functional in this mode)
# Use this only to take Playwright screenshots of the React UI.
# Do NOT use this to test MCP tools — the /mcp endpoint requires a real JWT.
Atlas__CatalogPath=$(pwd)/catalog \
dotnet run --project src/Atlas.Host --no-build &
```

> **Do not run `dotnet restore` manually** — it is already done and repeating it wastes
> time. Always pass `--no-restore` to `dotnet build` and `--no-build` to `dotnet run`
> after the initial build.

## Running Atlas.Host for Screenshots (Agent / CI Environments)

In the GitHub Copilot agent sandbox (and any headless CI context) the full Aspire
AppHost requires Docker for Keycloak and MCP Inspector images, which may not be
available or may take too long to pull. For screenshot and UI documentation tasks,
**run Atlas.Host directly** — no Docker, no Keycloak, no OIDC required:

```bash
# Build Atlas.Host if not already done
dotnet build src/Atlas.Host/Atlas.Host.csproj --no-restore

# Start Atlas.Host standalone — UI ready at http://localhost:5063 within ~3 s
# Omitting Atlas__Oidc__Issuer disables JWT auth (no Bearer token needed for the UI)
Atlas__CatalogPath=$(pwd)/catalog \
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
| No `Atlas__Oidc__Issuer` set | When this variable is absent, Atlas.Host registers no JWT Bearer scheme. The React UI endpoints (`/v1/apis`, `/v1/tools`) are `AllowAnonymous` so the UI loads fine. The `/mcp` endpoint still requires authorization, but that is not needed for UI screenshots. |
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

## Keycloak and OAuth2.0

Keycloak is included in the Aspire AppHost specifically to provide **proper OAuth2.0
authorization flows** for every type of Atlas consumer — human developers, AI agents,
and automated pipelines. This is not just an OIDC token validator; it is the full
authorization server that issues scoped access tokens and supports the browser-based
PKCE flow that MCP Inspector uses for interactive developer authentication.

### Why Keycloak is part of the Aspire setup

Atlas.Host enforces that every `/mcp` caller holds explicit, scoped consent:

- `platform-code-mode:search` — required to search the tool catalog
- `platform-code-mode:execute` — required to execute plans against downstream APIs
- Downstream permissions (e.g. `someapi:customers:read`) — forwarded in the caller's JWT
  to the target API

These scopes are real OAuth2.0 scopes defined in the Keycloak realm. Granting them
requires an authorization server — hence Keycloak.

### Keycloak OAuth2.0 clients in the `atlas` realm

Three clients are pre-configured in `src/Atlas.AppHost/keycloak/atlas-realm.json`:

| Client ID | Type | Flow | Purpose |
|-----------|------|------|---------|
| `atlas-mcp-client` | Confidential | Client credentials (M2M) | Programmatic agent / pipeline access to Atlas MCP. Secret: `atlas-mcp-secret`. Gets `platform-code-mode:search`, `platform-code-mode:execute`, and `someapi:customers:read` by default. |
| `mcp-inspector` | Public (PKCE) | Authorization code + PKCE | MCP Inspector interactive developer authentication. Redirect URI: `http://localhost:6274/*`. Gets all three scopes by default. |
| `atlas-ui-client` | Public (PKCE) | Authorization code + PKCE | Atlas React UI (future use). Gets `platform-code-mode:search` by default; `platform-code-mode:execute` is optional. |

### How Atlas.Host advertises Keycloak to MCP Inspector

Atlas.Host uses the MCP OAuth2.0 Resource Metadata spec to advertise Keycloak as
the authorization server. When an unauthenticated request reaches `/mcp`, Atlas
responds with:

```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="https://<keycloak>/realms/atlas/.well-known/openid-configuration"
```

MCP Inspector reads this header and can auto-discover the Keycloak token endpoint,
authorization endpoint, and supported scopes — enabling its built-in guided OAuth2.0
PKCE flow without any manual configuration. This is wired in `Atlas.Host/Program.cs`:

```csharp
options.ForwardChallenge = McpAuthenticationDefaults.AuthenticationScheme;
// ...
options.ResourceMetadata = new ProtectedResourceMetadata
{
    AuthorizationServers = [atlasOpts.Oidc.Issuer],  // points at Keycloak realm
};
```

## Testing MCP Tools with JWT Authentication

Testing MCP tools requires the full Aspire stack with Keycloak. Atlas is a **proxy
gateway** — `execute_plan` forwards the caller's JWT verbatim to downstream APIs
(`ExecutionEngine.cs:178`), so the downstream sample APIs must be running and the token
must be issued by an authority they trust. Only the full Aspire stack provides this.

### Full Aspire stack with Keycloak

When the full Aspire AppHost is running:

1. Open MCP Inspector — the Aspire dashboard shows its URL (typically `http://localhost:6274`)
2. Set **Transport Type** → `Streamable HTTP`
3. Set **URL** → the `atlas-host` `/mcp` endpoint from the Aspire dashboard
4. Set **Connection Type** → `Direct`
5. Expand **Authentication → OAuth 2.0 Flow** and fill in:
   - **Client ID**: `mcp-inspector`
   - **Client Secret**: *(leave empty — public PKCE client)*
   - **Redirect URL**: `http://localhost:6274/oauth/callback`
   - **Scope**: `openid platform-code-mode:search platform-code-mode:execute someapi:customers:read`
6. Click **Connect** — MCP Inspector reads the `WWW-Authenticate` challenge from
   Atlas.Host, auto-discovers the Keycloak authorization endpoint, and opens a browser
   window for you to log in (or completes the PKCE exchange automatically).
7. Click **List Tools** to verify all three Atlas tools appear.

For M2M / scripted access using the `atlas-mcp-client` service account:

```bash
# Get token from Keycloak — use the URL shown in the Aspire dashboard for keycloak
TOKEN=$(curl -s -X POST \
  "https://<keycloak-host>/realms/atlas/protocol/openid-connect/token" \
  -d "grant_type=client_credentials" \
  -d "client_id=atlas-mcp-client" \
  -d "client_secret=atlas-mcp-secret" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")
```

## Known Gotchas

These are real issues that have caused failures in previous agent sessions:

### JWT claim name remapping (`MapInboundClaims = false`)

`JwtSecurityTokenHandler` remaps well-known JWT claim names to long WS-Federation URIs
by default (e.g. `scp` → `http://schemas.microsoft.com/identity/claims/scope`).
`PlatformAuth.GetPermissions` looks up the original claim name (`scp`), so without
`MapInboundClaims = false` it always returns an empty set — even for a valid JWT with
the right scopes. This means every MCP tool call throws `UnauthorizedAccessException`
with the message *"Missing platform permission 'platform-code-mode:search'"*.

**Fix already applied** in `src/Atlas.Host/Program.cs`:
```csharp
options.MapInboundClaims = false;  // preserve scp, sub, etc. as-is
```

If this option is ever removed, all authenticated MCP calls will fail silently.

### MCP Inspector Direct mode needs `Mcp-Session-Id` exposed in CORS

Browsers restrict which response headers JavaScript can read. `Mcp-Session-Id` is a
custom header that MCP Inspector reads cross-origin to track sessions. Without
`WithExposedHeaders("Mcp-Session-Id")` in the CORS policy, every Direct-mode connect
attempt silently fails — the session ID is set server-side but the browser cannot
read it, so MCP Inspector reports a connection error.

**Fix already applied** in `src/Atlas.Host/Program.cs`:
```csharp
policy.WithExposedHeaders("Mcp-Session-Id");
```

### `Atlas__PlatformPermissions__Claim` must be `scope` for Keycloak

The AppHost sets `Atlas__PlatformPermissions__Claim` to `scope` automatically
(`WithEnvironment("Atlas__PlatformPermissions__Claim", "scope")`). Keycloak places
scopes in the `scope` claim. The default value in `AtlasOptions` is `scp` — always
ensure the AppHost override is in place when using Keycloak.

### `Atlas__Mcp__AllowAnonymous` is not a real configuration key — and anonymous MCP access is wrong by design

Previous documentation incorrectly referenced this flag. It does not exist in
`AtlasOptions` and should never be added.

**Why anonymous MCP access would be wrong:**
Agent Atlas is a governed execution gateway. When an agent calls `execute_plan`, Atlas
proxies requests to downstream APIs using the **caller's JWT**. The platform permission
checks (`platform-code-mode:search`, `platform-code-mode:execute`) exist to ensure the
caller has explicitly been granted consent to use the gateway. Bypassing auth would mean:
- Any unauthenticated caller could enumerate every tool in the catalog
- Any unauthenticated caller could execute plans against downstream production APIs
- Downstream permission checks (e.g. `someapi:customers:read`) would have no subject to enforce against

The catalog UI (`/v1/apis`, `/v1/tools`) is intentionally `AllowAnonymous` — catalog
*discovery* is open. Tool *execution* is not, and must not be.

**The actual anonymous-mode mechanism** (and when it is acceptable):
When `Atlas__Oidc__Issuer` is not set, Atlas.Host registers no JWT Bearer scheme.
The React UI endpoints load fine because they are `AllowAnonymous`. This mode is
**only acceptable for taking UI screenshots** — the `/mcp` endpoint is not usable.

**For UI screenshots**: omit `Atlas__Oidc__Issuer` entirely — the UI works, `/mcp` does not.  
**For MCP tool testing**: always use the full Aspire stack (`aspire run --project src/Atlas.AppHost`) with Keycloak. This provides a real JWT with the required scopes and exercises the full proxy path including `execute_plan`.

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
- The `/mcp` endpoint always requires JWT authorization (`RequireAuthorization()`). When `Atlas__Oidc__Issuer` is not set no JWT Bearer scheme is registered (anonymous dev mode — UI-only). Never deploy without an issuer in production.
- Catalog read endpoints (`/v1/apis`, `/v1/tools`) are intentionally `AllowAnonymous` (discovery/read is open).
- Platform permission checks (`platform-code-mode:search`, `platform-code-mode:execute`) are enforced inside `AtlasMcpTools` via `PlatformAuth` + `IHttpContextAccessor`.
- Use `ICatalogLoader` and `IToolIndex` abstractions; avoid depending on concrete implementations in new code.
- The execution engine enforces limits via `Atlas__ExecLimits__*` configuration; always respect these in plan execution code.

## Configuration Reference

| Variable | Default | Purpose |
|---|---|---|
| `Atlas__CatalogPath` | `/catalog` | Path to GitOps data-plane repo |
| `Atlas__Oidc__Issuer` | *(not set = anonymous UI-only mode)* | OIDC issuer URL; omit to disable JWT auth |
| `Atlas__Oidc__Audience` | `api://agent-atlas` | Expected JWT audience |
| `Atlas__PlatformPermissions__Claim` | `scp` | JWT claim for platform permissions (`scope` for Keycloak — AppHost sets this automatically) |
| `Atlas__ExecLimits__MaxSteps` | `50` | Max plan steps |
| `Atlas__ExecLimits__MaxCalls` | `50` | Max downstream HTTP calls per plan |
| `Atlas__ExecLimits__MaxSeconds` | `30` | Plan execution timeout |
| `Atlas__ExecLimits__MaxBytes` | `10485760` | Max cumulative response bytes |

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
