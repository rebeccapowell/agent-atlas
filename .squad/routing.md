# Agent Atlas — Routing Rules

## Primary Routing Table

| Domain | Keywords / Signals | Agent |
|--------|--------------------|-------|
| Architecture, scope, code review, big decisions | "review", "design", "architect", "should we", "trade-off" | Holden |
| .NET, ASP.NET Core, Aspire, MCP server, JWT/OIDC, execution engine | "dotnet", "aspire", "csproj", "jwt", "oidc", "execution", "catalog loader", "tool index", "AtlasMcpTools" | Naomi |
| React UI, shadcn/ui, components, wwwroot, frontend | "react", "shadcn", "component", "ui", "wwwroot", "frontend", "css", "tsx", "jsx" | Alex |
| MCP mesh, GitOps catalog, plan DSL, OpenAPI, x-mcp, tool discovery | "mcp", "catalog.yaml", "x-mcp", "openapi", "plan", "foreach", "call step", "gitops", "tool mesh" | Amos |
| GitHub Actions, CI/CD, Helm, Docker, workflows, releases | "workflow", "pipeline", "ci", "cd", "helm", "docker", "release", "github actions", ".github" | Bobbie |
| Tests, quality, edge cases, test coverage | "test", "xunit", "shouldly", "nsubstitute", "spec", "coverage", "edge case", "regression" | Drummer |

## Multi-Agent Triggers

| Situation | Agents |
|-----------|--------|
| New feature end-to-end | Holden (arch) + Naomi (backend) + Alex (frontend) + Drummer (tests) |
| New MCP tool type or plan DSL change | Holden + Amos + Naomi + Drummer |
| CI/CD changes affecting all services | Bobbie + Holden |
| Catalog schema change | Amos + Naomi + Drummer |
| Major React UI work | Alex + Holden (review) + Drummer |

## Fallback

When ambiguous, route to **Holden** who will sub-delegate or hand off.
