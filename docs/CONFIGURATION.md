---
title: Configuration Guide
nav_order: 7
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

## 4. Taking screenshots with Aspire MCP and Playwright MCP

When Copilot runs as the documentation agent, it can capture live screenshots of the
running application and embed them in the docs or the repository README. This requires
the **Aspire MCP server** and the **Playwright MCP server** to be active.

### How it works

```
aspire mcp start          ← exposes OTel data, resource URLs, and structured app state
npx @playwright/mcp       ← allows the agent to navigate and screenshot the Atlas UI
```

Copilot (or any coding agent) connects to both servers and can:

1. Use the Aspire MCP to discover the URL of the running `atlas-host` resource.
2. Use the Playwright MCP to navigate to the Atlas UI at that URL.
3. Capture screenshots of the tools list, tool detail, APIs list, dark/light modes, etc.
4. Embed those screenshots into `docs/` pages or the repository `README.md`.

### Usage in non-interactive / CI mode

> **Note:** In Aspire CLI 13.1.2, start the MCP server with `aspire mcp start`. The
> `aspire agent mcp` command is not yet available in the published NuGet package
> (it is in the main branch only).

```bash
# Terminal 1 — start the Aspire application
aspire run --project src/Atlas.AppHost

# Terminal 2 — start the Aspire MCP server (exposes OTel/resource data to the agent)
aspire mcp start

# The Playwright MCP server is started on demand by the agent via npx:
# npx @playwright/mcp@latest
```

Configuration for both MCP servers is already committed to the repository:
- `.copilot/mcp-config.json` — shared team config (Aspire MCP + Playwright MCP)
- `.vscode/mcp.json` — VS Code workspace config

### Instructing Copilot to take screenshots

When filing a documentation update issue or editing the `update-docs.md` workflow
instructions, include a task like:

```
- [ ] Use the Aspire MCP to get the atlas-host URL.
- [ ] Use the Playwright MCP to navigate to the Atlas UI.
- [ ] Take a screenshot of the tools list (light mode) and save to docs/screenshots/.
- [ ] Take a screenshot of the tool detail panel and save to docs/screenshots/.
- [ ] Update docs/index.md to reference the new screenshots.
```

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
