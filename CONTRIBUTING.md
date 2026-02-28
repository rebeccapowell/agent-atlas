# Contributing to Agent Atlas

Thank you for your interest in contributing to Agent Atlas! This guide explains how to get started and what we expect from contributions.

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (preview)
- [Docker](https://docs.docker.com/get-docker/) (for integration and container testing)
- A running Keycloak instance **or** `Atlas__Mcp__AllowAnonymous=true` for local dev

### Running the stack locally

```bash
# Clone and restore
git clone https://github.com/rebeccapowell/agent-atlas.git
cd agent-atlas
dotnet restore

# Start everything via Aspire AppHost
dotnet run --project src/Atlas.AppHost
```

### Running tests

```bash
dotnet test src/Atlas.Host.Tests/Atlas.Host.Tests.csproj
```

To see coverage output locally:

```bash
dotnet test src/Atlas.Host.Tests/Atlas.Host.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults
```

---

## How to contribute

1. **Open an issue first** for non-trivial changes so we can discuss approach before you invest time.
2. **Fork** the repository and create a branch from `dev`:
   ```bash
   git checkout -b feat/my-feature dev
   ```
3. **Make your changes** following the coding style below.
4. **Add or update tests** – all code changes should be covered.
5. **Run the test suite** and ensure it passes:
   ```bash
   dotnet test
   ```
6. **Open a pull request** against the `dev` branch and fill in the PR template.

---

## Coding conventions

- Follow standard C# / .NET conventions (see [Microsoft docs](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)).
- Keep changes focused and minimal — one feature or fix per PR.
- Use `nullable` reference types (`#nullable enable` is set project-wide).
- Prefer `record` types for immutable data; use `init` properties where mutation isn't needed.
- All public APIs should include XML doc comments.

---

## Branch model

| Branch    | Purpose                                      |
|-----------|----------------------------------------------|
| `dev`     | Active development target — PR here          |
| `insider` | Nightly / insider builds                     |
| `preview` | Release candidates                           |
| `main`    | Stable releases — do not target directly     |

---

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add tag-based tool filtering
fix: correct YAML deserialization of catalog.yaml
docs: update deployment guide
chore: bump YamlDotNet to 16.3.0
test: add ToolIndex search coverage
```

---

## Reporting security vulnerabilities

**Please do not open a public issue for security vulnerabilities.**
See [SECURITY.md](../SECURITY.md) (if present) or contact the maintainers privately.

---

## Code of conduct

This project follows the [Contributor Covenant](https://www.contributor-covenant.org/) Code of Conduct. Be kind and respectful to everyone.
