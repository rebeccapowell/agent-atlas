# Agent Atlas — Squad

## Project Context

**Project:** Agent Atlas
**Description:** Open source enterprise MCP (Model Context Protocol) gateway and tool server mesh. Combines a governed API catalog (GitOps-driven via `catalog/`), MCP tool discovery/execution engine, and a React/shadcn UI capability map. Stack: .NET 10, ASP.NET Core 10, .NET Aspire 13.x, React, shadcn/ui, xUnit + Shouldly + NSubstitute.
**Analogues:** Apollo GraphQL Mesh + Backstage + Calico
**Universe:** The Expanse
**User:** Rebecca Powell

## Members

| Name | Role | Model | Emoji |
|------|------|-------|-------|
| Holden | Lead | auto | 🏗️ |
| Naomi | .NET/Aspire Dev | claude-sonnet-4.5 | 🔧 |
| Alex | React/UI Dev | claude-sonnet-4.5 | ⚛️ |
| Amos | Platform Specialist | claude-sonnet-4.5 | 🕸️ |
| Bobbie | GitHub Pipelines | claude-sonnet-4.5 | ⚙️ |
| Drummer | Tester | claude-sonnet-4.5 | 🧪 |
| Scribe | Session Logger | claude-haiku-4.5 | 📋 |
| Ralph | Work Monitor | — | 🔄 |

## Key Files

| Path | Purpose |
|------|---------|
| `src/Atlas.Host/` | Main service: MCP server, catalog API, execution engine, React UI |
| `src/Atlas.AppHost/` | .NET Aspire orchestration host |
| `src/SampleApi.ToolEnabled/` | Demo API with MCP tools registered |
| `src/SampleApi.NotToolEnabled/` | Demo API intentionally NOT registered |
| `catalog/` | GitOps data-plane: catalog.yaml, apis/, policies/ |
| `helm/` | Kubernetes / Helm chart |

## Conventions

- Test framework: xUnit + Shouldly + NSubstitute (+ AutoFixture). **Moq and FluentAssertions are banned.**
- Test projects: `<ProjectName>.Tests` naming convention
- Nullable reference types enabled — always handle nullability
- Implicit usings enabled
- `/mcp` endpoint always requires JWT auth
- Catalog read endpoints (`/v1/apis`, `/v1/tools`) are `AllowAnonymous`
- Build: `dotnet build src/Atlas.AppHost/Atlas.AppHost.csproj`
- Run: `aspire run --project src/Atlas.AppHost` (preferred)
