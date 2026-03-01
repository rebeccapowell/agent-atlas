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

# Run without Docker (StubIdp fallback — two terminals required)
# Terminal 1: StubIdp JWT issuer runs on http://localhost:5172
dotnet run --project src/Atlas.StubIdp

# Terminal 2: Atlas.Host pointed at StubIdp
Atlas__CatalogPath=$(pwd)/catalog \
Atlas__Oidc__Issuer=http://localhost:5172 \
dotnet run --project src/Atlas.Host
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

# Run Atlas.Host with StubIdp auth — the CORRECT way to test MCP tools (no Docker needed)
# This is the only acceptable approach for agent/CI MCP testing without Keycloak.
# First start StubIdp: dotnet run --project src/Atlas.StubIdp --no-build &
Atlas__CatalogPath=$(pwd)/catalog \
Atlas__Oidc__Issuer=http://localhost:5172 \
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

## Testing MCP Tools with JWT Authentication (StubIdp — Agent / CI)

When Docker is unavailable but you need to test the `/mcp` endpoint (not just the UI),
use Atlas.StubIdp as a lightweight JWT issuer. This avoids the need for Keycloak.

```bash
# 1. Start StubIdp — JWT issuer on http://localhost:5172
dotnet run --project src/Atlas.StubIdp --no-build &
STUB_PID=$!
sleep 3

# 2. Start Atlas.Host with StubIdp as the OIDC issuer
Atlas__CatalogPath=$(pwd)/catalog \
Atlas__Oidc__Issuer=http://localhost:5172 \
dotnet run --project src/Atlas.Host --no-build &
ATLAS_PID=$!
sleep 6
curl -sf http://localhost:5063/healthz   # should return "Healthy"

# 3. Get an access token from StubIdp (form POST, not JSON)
TOKEN=$(curl -s -X POST http://localhost:5172/token \
  -F "client_id=atlas-mcp-client" \
  -F "scope=platform-code-mode:search platform-code-mode:execute" \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])")

# 4. Initialize an MCP session
RESP=$(curl -s -X POST http://localhost:5063/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"jsonrpc":"2.0","method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}},"id":1}' \
  -D -)
SID=$(echo "$RESP" | grep -i "mcp-session-id:" | awk '{print $2}' | tr -d '\r')

# 5. Call search_tools
curl -s -X POST http://localhost:5063/mcp \
  -H "Content-Type: application/json" \
  -H "Accept: application/json, text/event-stream" \
  -H "Authorization: Bearer $TOKEN" \
  -H "mcp-session-id: $SID" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"search_tools","arguments":{}},"id":2}'

# 6. Cleanup
# kill $ATLAS_PID $STUB_PID
```

### Connecting MCP Inspector to Atlas.Host (Direct mode)

When the full Aspire stack is running (or when using Atlas.Host + StubIdp as above),
connect MCP Inspector at `http://localhost:6274` using **Direct** mode:

1. Set **Transport Type** → `Streamable HTTP`
2. Set **URL** → `http://localhost:5063/mcp` (or the URL from the Aspire dashboard)
3. Set **Connection Type** → `Direct`  
   *(Via Proxy mode requires the MCP Inspector proxy's own session token which may not
   be available or may have expired; Direct mode avoids this dependency entirely.)*
4. Expand **Authentication → Custom Headers**, add:
   - Header name: `Authorization`
   - Header value: `Bearer <token from StubIdp or Keycloak>`
5. Click **Connect**, then **List Tools** to verify `search_tools`, `describe_tool`,
   `execute_plan` are listed.

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

### `Atlas__PlatformPermissions__Claim` differs between StubIdp and Keycloak

| Identity provider | Claim name in token | Config value needed |
|---|---|---|
| StubIdp | `scp` | `scp` (the **default** — no override needed) |
| Keycloak (via AppHost) | `scope` | `scope` (set automatically by AppHost) |

When running Atlas.Host with StubIdp manually, do **not** set
`Atlas__PlatformPermissions__Claim` — the default `scp` is correct.
When running via the full Aspire AppHost with Keycloak, the AppHost sets it to `scope`
automatically (`WithEnvironment("Atlas__PlatformPermissions__Claim", "scope")`).

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
**For MCP tool testing**: always use `Atlas__Oidc__Issuer=http://localhost:5172` with StubIdp
and obtain a real JWT that carries the required scopes. This is not a workaround — it
is the correct, intentional development workflow.

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
| `Atlas__PlatformPermissions__Claim` | `scp` | JWT claim for platform permissions (`scp` for StubIdp; `scope` for Keycloak — AppHost sets this automatically) |
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
