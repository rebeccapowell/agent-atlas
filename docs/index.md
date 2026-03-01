---
title: Home
layout: home
nav_order: 1
---

# Agent Atlas — MCP Mesh

{: .note }
Agent Atlas is an enterprise **MCP (Model Context Protocol) gateway** built with ASP.NET Core 10 and .NET Aspire. It turns a messy internal API estate into a governed, searchable catalog of tools that both humans and AI agents can discover and invoke safely.

---

## What it does

| Capability | Description |
|---|---|
| **Catalog** | Reads OpenAPI specs from a GitOps data-plane repository. Teams annotate operations with `x-mcp` vendor extensions to mark them as agent tools. |
| **MCP Server** | Exposes three MCP tools — `SearchTools`, `DescribeTool`, `ExecutePlan` — via Streamable HTTP transport at `/mcp`. |
| **Execution Engine** | Interprets a JSON plan DSL (`call`, `foreach`, `if`, `return` steps) and proxies requests to downstream APIs, forwarding the caller's JWT. |
| **React UI** | A read-only capability map for developers and operators. |

---

## Quick start

```bash
# Run with Aspire (starts all services including Keycloak)
aspire run --project src/Atlas.AppHost

# Run without Docker (StubIdp fallback)
Atlas__CatalogPath=$(pwd)/catalog \
Atlas__Oidc__Issuer=http://localhost:5200 \
aspire run --project src/Atlas.Host
```

---

## Documentation

{: .highlight }
Browse the full documentation using the navigation on the left (or above on mobile).

| Page | Description |
|---|---|
| [Security Model](security-model) | Two-layer auth model — platform permissions + downstream API auth |
| [GitOps Data Plane](gitops-data-plane) | Catalog repo structure and `x-mcp` extension reference |
| [Deploy with Docker](deploy-docker) | Docker deployment guide including published image details |
| [Deploy with Helm](deploy-helm) | Kubernetes / Helm deployment guide including AKS, GitLab CI, Calico, and Key Vault |
| [CI/CD Pipelines](pipelines) | Full pipeline setup guide — secrets, branch model, GitHub Pages, and first release |

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
│  │  /v1/tools          │ │ └──────────────────────────┘    │
│  └─────────────────────┘ │                                  │
│                          │                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Catalog Loader  (reads GitOps data-plane repo)     │   │
│  │  OpenAPI + x-mcp vendor extension → ToolDefinition  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  React UI (browse APIs & tools, dark/light mode)            │
└──────────────────────┬──────────────────────────────────────┘
                       │  Bearer JWT forwarded as-is
                       ▼
        Your existing REST APIs (unchanged)
```

---

## MCP tools exposed to AI agents

| Tool | Purpose |
|---|---|
| `SearchTools` | Search the catalog by query, API ID, safety tier, or access filter |
| `DescribeTool` | Get full metadata for a specific tool (schema, permissions, examples) |
| `ExecutePlan` | Run (or dry-run) a JSON plan against one or more tools |

### Example plan

```json
{
  "steps": [
    { "type": "call",    "toolId": "my-api.customers.list", "args": {}, "saveAs": "customers" },
    { "type": "foreach", "items": "customers", "as": "item", "do": [
        { "type": "call", "toolId": "my-api.customers.get",
          "args": { "id": "{% raw %}{{item.id}}{% endraw %}" }, "saveAs": "detail" }
    ]},
    { "type": "return",  "from": "customers" }
  ]
}
```

---

## Projects in this solution

| Project | Description |
|---|---|
| **Atlas.AppHost** | .NET Aspire orchestration host — wires all services together for local dev |
| **Atlas.Host** | Main service: MCP server, catalog REST API, React UI, execution engine |
| **Atlas.StubIdp** | Lightweight in-process RSA JWT issuer for offline/CI dev (no Keycloak required) |
| **SampleApi.ToolEnabled** | Demo customer API whose operations are registered as MCP tools |
| **SampleApi.NotToolEnabled** | Demo products API intentionally *not* registered as tools |

---

## Configuration reference

| Variable | Default | Description |
|---|---|---|
| `Atlas__CatalogPath` | `/catalog` | Path to the GitOps data-plane repo |
| `Atlas__CatalogStrict` | `true` | Fail hard on catalog parse errors |
| `Atlas__Oidc__Issuer` | *(required)* | OIDC issuer URL |
| `Atlas__Oidc__Audience` | `api://agent-atlas` | Expected JWT audience |
| `Atlas__PlatformPermissions__Claim` | `scp` | JWT claim for permissions |
| `Atlas__ExecLimits__MaxSteps` | `50` | Max steps per plan |
| `Atlas__ExecLimits__MaxCalls` | `50` | Max downstream HTTP calls per plan |
| `Atlas__ExecLimits__MaxSeconds` | `30` | Wall-clock timeout for plan execution |
| `Atlas__ExecLimits__MaxBytes` | `10485760` | Max cumulative response bytes |
| `Atlas__Cors__AllowedOrigins` | *(localhost in dev)* | Allowed CORS origins for the UI |

---

## Further reading

- [GitHub repository](https://github.com/rebeccapowell/agent-atlas)
- [Docker Hub / GHCR image](https://github.com/rebeccapowell/agent-atlas/pkgs/container/agent-atlas)
