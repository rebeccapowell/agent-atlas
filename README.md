# Agent Atlas

**Agent Atlas** is a self-hosted, GitOps-driven tool catalog and MCP (Model Context Protocol) gateway for enterprise AI agents.

It bridges the gap between an AI agent (e.g. Claude, Copilot, a custom LLM orchestrator) and your organisation's existing REST APIs — without requiring those APIs to be MCP-aware themselves. You describe your APIs in a Git repository using OpenAPI + a thin `x-mcp` vendor extension, and Agent Atlas dynamically turns them into MCP tools that any compliant AI agent can discover, introspect, and invoke.

---

## What problem does it solve?

Enterprise organisations have hundreds of internal REST APIs. AI agents need a way to:

1. **Discover** which APIs exist and what they can do
2. **Understand** security requirements before calling them
3. **Execute** calls safely — respecting safety tiers, permission gates, and execution limits

Agent Atlas is the single, secured gateway that provides all three, without touching your existing APIs.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  AI Agent (Claude, Copilot, custom orchestrator, …)         │
│  ─ connects via MCP Streamable HTTP transport               │
└──────────────────────┬──────────────────────────────────────┘
                       │  Bearer JWT (Keycloak / any OIDC IdP)
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  Atlas.Host  (ASP.NET Core 10, .NET Aspire)                 │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  MCP Server  (/mcp — ModelContextProtocol SDK)      │   │
│  │  • SearchTools   • DescribeTool   • ExecutePlan     │   │
│  └──────────────────────┬──────────────────────────────┘   │
│                          │                                  │
│  ┌─────────────────────┐ │ ┌──────────────────────────┐    │
│  │  Catalog REST API   │ │ │  Execution Engine         │    │
│  │  /v1/apis           │ │ │  (plan DSL interpreter)   │    │
│  │  /v1/tools          │ │ │  call / foreach / if      │    │
│  └─────────────────────┘ │ └──────────────────────────┘    │
│                          │                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Catalog Loader  (reads GitOps data-plane repo)     │   │
│  │  OpenAPI + x-mcp vendor extension → ToolDefinition  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  React UI (browse APIs & tools, dark mode)                  │
└──────────────────────┬──────────────────────────────────────┘
                       │  Bearer JWT forwarded as-is
                       ▼
        Your existing REST APIs (unchanged)
```

---

## Key concepts

### The data-plane (GitOps catalog repo)

A Git repository is Atlas's database. It contains:

```
catalog/
├── catalog.yaml              # Global org metadata
├── apis/
│   ├── my-api/
│   │   ├── api.yaml          # API identity, base URL, environments
│   │   └── openapi.yaml      # OpenAPI 3.x spec + x-mcp extensions
│   └── ...
└── policies/
    └── defaults.yaml         # Platform-level policy overrides
```

Operations marked with `x-mcp.enabled: true` in the OpenAPI spec are automatically indexed as MCP tools at startup.

```yaml
# openapi.yaml excerpt
paths:
  /customers:
    get:
      operationId: ListCustomers
      x-mcp:
        enabled: true
        name: "my-api.customers.list"   # stable tool ID
        safety: read                    # read | write | destructive
        requiredPermissions:
          - "my-api:customers:read"     # informational — downstream API still enforces this
        entitlementHint: "Request access via the My API - Readers access package"
        tags: [customers, list]
```

### Two-layer authorization model

| Layer | Enforced by | What it controls |
|-------|-------------|-----------------|
| **Platform** | Atlas | Can the caller use Atlas at all? (`platform-code-mode:search`, `platform-code-mode:execute`) |
| **Downstream** | Your API | Can the caller call this specific operation? (e.g. `my-api:customers:read`) |

Atlas validates the incoming JWT (signature, issuer, audience, expiry) and checks platform permissions. It then **forwards the caller's JWT unchanged** to the downstream API, which is responsible for its own authorization. `requiredPermissions` in the catalog is **informational metadata** — Atlas surfaces it in the UI and can compute an access hint, but does not block execution based on it.

### MCP tools exposed to AI agents

Three tools are registered on the MCP server at `/mcp`:

| Tool | Purpose |
|------|---------|
| `SearchTools` | Search the catalog by query, API ID, safety tier, or access filter |
| `DescribeTool` | Get full metadata for a specific tool (schema, permissions, examples) |
| `ExecutePlan` | Run (or dry-run) a JSON plan against one or more tools |

#### Plan DSL (ExecutePlan)

```json
{
  "steps": [
    { "type": "call", "toolId": "my-api.customers.list", "args": {}, "saveAs": "customers" },
    { "type": "foreach", "in": "$customers", "do": [
        { "type": "call", "toolId": "my-api.customers.get", "args": { "id": "$item.id" }, "saveAs": "detail" }
    ]},
    { "type": "return", "value": "$customers" }
  ]
}
```

Supported step types: `call`, `foreach`, `if`, `return`. The engine enforces configurable limits on steps, HTTP calls, duration, and response body size.

---

## Projects in this solution

| Project | Description |
|---------|-------------|
| **Atlas.AppHost** | .NET Aspire orchestration host — wires all services together for local dev |
| **Atlas.Host** | The main service: MCP server, catalog REST API, React UI, execution engine |
| **Atlas.StubIdp** | Lightweight in-process JWT issuer for offline/CI dev (no Keycloak required) |
| **SampleApi.ToolEnabled** | Demo customer API whose operations are registered as MCP tools |
| **SampleApi.NotToolEnabled** | Demo products API intentionally *not* registered as tools |

---

## Local development

### Prerequisites

- .NET 10 SDK
- Docker Desktop (for Keycloak container)
- Node.js 20+ (only if you want to rebuild the React UI)

### Run with Aspire

```bash
dotnet run --project src/Atlas.AppHost
```

The Aspire dashboard opens automatically. Keycloak starts on a random port and the `atlas` realm is imported automatically. Atlas.Host waits for Keycloak to be ready before starting.

**Default Keycloak credentials for local dev**

| Client | Client ID | Secret | Scopes |
|--------|-----------|--------|--------|
| M2M (client_credentials) | `atlas-mcp-client` | `atlas-mcp-secret` | `platform-code-mode:search platform-code-mode:execute` |
| UI (PKCE) | `atlas-ui-client` | *(public)* | `platform-code-mode:search` |

### Run without Docker (StubIdp fallback)

Add `Atlas.StubIdp` back to `Atlas.AppHost/Program.cs` and point `Atlas__Oidc__Issuer` at it. This requires no containers and is suitable for pure offline dev or CI pipelines.

### Build

```bash
dotnet build src/Atlas.AppHost/Atlas.AppHost.csproj
```

---

## Production deployment

### Docker

```bash
docker build -t agent-atlas:latest .

docker run -d \
  -p 8080:8080 \
  -v /path/to/your/catalog-repo:/catalog:ro \
  -e Atlas__CatalogPath=/catalog \
  -e Atlas__Oidc__Issuer=https://your-idp.example.com/realms/your-realm \
  -e Atlas__Oidc__Audience=api://agent-atlas \
  -e Atlas__PlatformPermissions__Claim=scope \
  agent-atlas:latest
```

### Kubernetes / Helm

```bash
helm install agent-atlas ./helm/agent-atlas \
  --namespace agent-atlas \
  --create-namespace \
  --set oidc.issuer=https://your-idp.example.com/realms/your-realm \
  --set oidc.audience=api://agent-atlas
```

See [`docs/deploy-docker.md`](docs/deploy-docker.md) and [`docs/deploy-helm.md`](docs/deploy-helm.md) for full configuration options.

---

## Configuration reference

| Environment variable | Default | Description |
|---------------------|---------|-------------|
| `Atlas__CatalogPath` | `/catalog` | Path to the GitOps data-plane repo |
| `Atlas__CatalogStrict` | `true` | Fail hard on catalog parse errors |
| `Atlas__Oidc__Issuer` | *(required)* | OIDC issuer URL |
| `Atlas__Oidc__Audience` | `api://agent-atlas` | Expected JWT audience |
| `Atlas__PlatformPermissions__Claim` | `scp` | JWT claim name for permissions (`scp`, `scope`, `roles`, …) |
| `Atlas__ExecLimits__MaxSteps` | `50` | Max steps in a single plan |
| `Atlas__ExecLimits__MaxCalls` | `50` | Max downstream HTTP calls per plan |
| `Atlas__ExecLimits__MaxSeconds` | `30` | Wall-clock timeout for plan execution |
| `Atlas__ExecLimits__MaxBytes` | `10485760` | Max cumulative response bytes |
| `Atlas__Cors__AllowedOrigins` | *(localhost in dev)* | Allowed CORS origins for the UI |

---

## Further reading

- [`docs/security-model.md`](docs/security-model.md) — detailed explanation of the two-layer auth model
- [`docs/gitops-data-plane.md`](docs/gitops-data-plane.md) — catalog repo structure and `x-mcp` extension reference
- [`docs/deploy-docker.md`](docs/deploy-docker.md) — Docker deployment guide
- [`docs/deploy-helm.md`](docs/deploy-helm.md) — Kubernetes / Helm deployment guide with AKS, GitLab CI, and Calico examples
