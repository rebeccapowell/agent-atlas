---
title: Configuration Guide
nav_order: 8
---

# Configuration Steps for GitHub Pages & Agentic Documentation

This file contains the steps you need to take in the GitHub repository settings to enable:

1. [GitHub Pages documentation site](#1-enable-github-pages)
2. [Agentic documentation update workflow](#2-enable-the-agentic-documentation-workflow)
3. [Optional: gh-aw CLI for customising the workflow](#3-optional-install-gh-aw-and-customise-the-workflow)

---

## 1. Enable GitHub Pages

The `squad-docs.yml` workflow builds the `/docs` Jekyll site and deploys it to GitHub Pages
using the `actions/jekyll-build-pages` + `actions/deploy-pages` actions.

### Steps

1. **Enable GitHub Pages in the repository settings**

   - Go to **Settings → Pages**
   - Under **Source**, select **GitHub Actions**
   - Save

   > **Note:** Do **not** select a branch or folder — selecting "GitHub Actions" is what
   > allows the `deploy-pages` action to publish the site.

2. **Ensure workflow permissions allow Pages deployment**

   - Go to **Settings → Actions → General → Workflow permissions**
   - Select **Read and write permissions**
   - Save

3. **Trigger the first deployment**

   Either merge a change to `docs/` into `main`, or manually trigger the workflow:

   - Go to **Actions → Squad Docs — Build & Deploy → Run workflow**

4. **Verify the site is live**

   Once the workflow completes, the site will be available at:

   ```
   https://<your-github-username>.github.io/agent-atlas/
   ```

   For example: `https://rebeccapowell.github.io/agent-atlas/`

---

## 2. Enable the Agentic Documentation Workflow

The `update-docs.yml` workflow runs on every PR and push to `main`. It detects code
changes that may require documentation updates and assigns **GitHub Copilot** to review
the `/docs` folder and open a draft documentation PR if needed.

### Steps

1. **Set workflow permissions (if not done in step 1 above)**

   - **Settings → Actions → General → Workflow permissions**
   - Select **Read and write permissions**
   - Enable **Allow GitHub Actions to create and approve pull requests**
   - Save

2. **Enable GitHub Copilot coding agent**

   - Go to **Settings → Copilot → Coding agent**
   - Enable the **GitHub Copilot coding agent**
   - Save

   > **Why?** The `update-docs.yml` workflow creates a documentation review issue and
   > mentions `@github-copilot` in the body. The coding agent picks up this issue, analyses the
   > code changes, and opens a draft PR with documentation updates.
   >
   > If the coding agent is **not** enabled, the issue will still be created but will
   > not be automatically actioned by Copilot — a human reviewer will need to act on it.

3. **Ensure the `documentation` and `automation` labels exist** *(optional)*

   The workflow attempts to create these labels if they don't exist. If it lacks
   permissions, create them manually:

   - **Issues → Labels → New label**
   - Create `documentation` (colour: `#0075ca`)
   - Create `automation` (colour: `#e4e669`)

4. **Verify the workflow runs**

   - Open a PR against `dev`, `preview`, or `main` that changes source code
   - A comment will appear on the PR linking to the new documentation review issue
   - `@github-copilot` is mentioned in the issue body (coding agent picks it up automatically if enabled)

---

## 3. Optional: Install `gh-aw` and Customise the Workflow

The `.github/workflows/update-docs.md` file is the **source workflow** in
[GitHub Agentic Workflow](https://github.github.com/gh-aw/) (gh-aw) Markdown format.
The `.github/workflows/update-docs.yml` is its compiled equivalent.

If you want to customise the natural-language instructions the agent follows and
recompile the workflow:

1. **Install the `gh` CLI** — https://cli.github.com/

2. **Install the `gh aw` extension**

   ```bash
   gh extension install github/gh-aw
   ```

3. **Edit `.github/workflows/update-docs.md`** to customise the agent instructions

4. **Compile to GitHub Actions YAML**

   ```bash
   gh aw compile .github/workflows/update-docs.md
   ```

   This regenerates `.github/workflows/update-docs.yml`.

5. **Commit and push both files**

   ```bash
   git add .github/workflows/update-docs.md .github/workflows/update-docs.yml
   git commit -m "docs(workflow): update agentic docs workflow"
   git push
   ```

For more information see:
- https://github.com/githubnext/agentics/blob/main/docs/update-docs.md
- https://github.github.com/gh-aw/setup/creating-workflows/

---

## 4. Taking screenshots in the Copilot agent environment

When Copilot runs as the documentation agent it can capture live screenshots of the
running application and embed them in the docs or the repository README. The approach
that works reliably in the GitHub Copilot agent sandbox (and any headless CI context)
is to **run Atlas.Host directly** — no Docker, no Keycloak, no Aspire AppHost needed.

### Exact commands used (verified in the Copilot agent environment)

```bash
# 1. Build Atlas.Host (skip if already done in copilot-setup-steps)
dotnet build src/Atlas.Host/Atlas.Host.csproj --no-restore

# 2. Start Atlas.Host standalone in the background
# Omitting Atlas__Oidc__Issuer disables JWT auth — the React UI loads fine without a token.
Atlas__CatalogPath=$(pwd)/catalog \
dotnet run --project src/Atlas.Host --no-build &
APP_PID=$!

# 3. Wait for the app to start (~3–4 seconds)
sleep 4
curl -sf http://localhost:5063/healthz   # → "Healthy"

# 4. Use the Playwright MCP to navigate and take screenshots
#    Navigate to: http://localhost:5063

# 5. Stop the app when done
# kill $APP_PID
```

### Why this works — key facts

| Fact | Detail |
|------|--------|
| Port 5063 | `dotnet run` reads `src/Atlas.Host/Properties/launchSettings.json`. The `http` profile sets `applicationUrl: http://localhost:5063`. When using `dotnet run`, launchSettings.json takes precedence over `ASPNETCORE_URLS` — always use port 5063. |
| `Atlas__CatalogPath=$(pwd)/catalog` | Points to the bundled sample catalog already in the repository root. |
| No `Atlas__Oidc__Issuer` | `Program.cs` skips the entire JWT Bearer middleware when this variable is absent. The UI endpoints (`/v1/apis`, `/v1/tools`) are `AllowAnonymous`, so the React UI loads without a token. `/mcp` is non-functional in this mode. |
| React UI | Pre-built into `src/Atlas.Host/wwwroot/` — no Node.js build step needed. |
| Catalog REST API | `/v1/apis` and `/v1/tools` are `AllowAnonymous` — the UI loads data without any Bearer token. |

### Why the full Aspire AppHost does not work in agent/CI environments

The full stack (`aspire run --project src/Atlas.AppHost`) requires Docker to pull the
Keycloak and MCP Inspector images. In the Copilot agent sandbox these images either
are not available or take too long to pull, causing the `atlas-host` resource to remain
in a `Waiting` state rather than `Running`. The Atlas.Host standalone approach sidesteps
all of this.

### Screenshot targets

After starting Atlas.Host, use the **Playwright MCP** to navigate to `http://localhost:5063`
and capture the following screenshots:

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

Toggle dark/light mode with the moon/sun icon in the top-right of the navigation bar.

After saving screenshots, update `docs/walkthrough.md`, `docs/index.md`, and `README.md`
to reference them.

### Full-stack screenshots (when Docker is available)

If you are running locally with Docker available, the full Aspire AppHost provides a
richer environment (Keycloak, MCP Inspector, OTel). In that case:

```bash
# Terminal 1 — start the full stack
aspire run --project src/Atlas.AppHost

# Terminal 2 — start the Aspire MCP server (exposes OTel/resource data to the agent)
aspire mcp start
```

Use the **Aspire MCP** (`aspire mcp start`) to discover the URL of the running
`atlas-host` resource, then use the **Playwright MCP** to navigate and capture
screenshots. MCP server configuration is committed at:
- `.copilot/mcp-config.json` — shared team config (Aspire MCP + Playwright MCP)
- `.vscode/mcp.json` — VS Code workspace config

---

## Summary checklist

| Step | Where | Required for |
|---|---|---|
| Pages source → **GitHub Actions** | Settings → Pages | GitHub Pages site |
| Workflow permissions → **Read and write** | Settings → Actions → General | Pages deployment + doc PRs |
| Allow Actions to **create PRs** | Settings → Actions → General | Agentic doc update PRs |
| **Copilot coding agent** enabled | Settings → Copilot → Coding agent | Automated doc updates |
| Labels: `documentation`, `automation` | Issues → Labels | Clean issue tagging (auto-created) |
| `.copilot/mcp-config.json` committed | Already in repo | Aspire MCP + Playwright MCP for screenshots |
