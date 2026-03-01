---
title: CI/CD Pipelines
nav_order: 6
---

# CI/CD Pipeline Setup Guide

This document explains every GitHub repository setting, secret, and one-off configuration step needed to make the Agent Atlas CI/CD pipelines work correctly.

---

## Contents

1. [Overview](#overview)
2. [Branch model](#branch-model)
3. [Workflow summary](#workflow-summary)
4. [Required GitHub settings](#required-github-settings)
   - [Actions: workflow permissions](#1-actions-workflow-permissions)
   - [Actions: allow Actions to create PRs](#2-actions-allow-actions-to-create-prs)
   - [GitHub Container Registry](#3-github-container-registry-ghcrio)
   - [GitHub Pages (optional)](#4-github-pages-optional)
   - [Branch protection rules (recommended)](#5-branch-protection-rules-recommended)
5. [Secrets and tokens](#secrets-and-tokens)
6. [.NET SDK version](#net-sdk-version)
7. [First release — bootstrapping](#first-release--bootstrapping)
8. [Troubleshooting](#troubleshooting)

---

## Overview

The pipelines cover four concerns:

| Concern | Workflow file |
|---|---|
| Build, test, coverage on PRs and dev/insider pushes | `squad-ci.yml` |
| Stable release — tag + GitHub release | `squad-release.yml` |
| Insider pre-release — timestamped tag | `squad-insider-release.yml` |
| Preview validation before final release | `squad-preview.yml` |
| Docker image publish to ghcr.io | `docker-publish.yml` |
| Branch promotion (dev → preview → main) | `squad-promote.yml` |
| Docs site build & deploy | `squad-docs.yml` |

The issue management workflows (`squad-triage.yml`, `squad-heartbeat.yml`, `squad-issue-assign.yml`, `squad-label-enforce.yml`, `sync-squad-labels.yml`) are driven entirely by `GITHUB_TOKEN` and require no extra configuration.

---

## Branch model

```
dev ──────────────────────────────────────► insider (nightly pre-releases)
 │                                           │
 └─► promote (workflow_dispatch) ──► preview ──► main (stable release + Docker push)
```

| Branch | Purpose |
|---|---|
| `dev` | Active development — target for pull requests |
| `insider` | Opt-in early builds; produces timestamped pre-releases |
| `preview` | Release candidate; runs preview validation |
| `main` | Stable releases; triggers release tagging and Docker publish |

Use the **Promote** workflow (`Actions → Promote → Run workflow`) to merge `dev → preview → main` in a controlled, reviewable way.

---

## Workflow summary

### `squad-ci.yml` — CI (pull requests & dev/insider pushes)
- Cross-platform matrix: **ubuntu-latest**, **windows-latest**, **macos-latest**
- Restores, builds, and tests `src/Atlas.Host.Tests`
- Collects XPlat code coverage (Cobertura XML)
- Publishes TRX test results as a check via `dorny/test-reporter`
- Uploads coverage XML as a workflow artifact (7-day retention)

### `squad-release.yml` — Stable release
- Triggered by a push to `main`
- Builds and tests, then auto-increments the patch segment of the latest `v*.*.*` tag (creates `v0.1.0` if no tag exists)
- Creates a GitHub release with auto-generated notes

### `squad-insider-release.yml` — Insider pre-release
- Triggered by a push to `insider`
- Builds and tests, then creates a timestamped pre-release tag: `insider-YYYYMMDDHHMMSS-<sha7>`

### `squad-preview.yml` — Preview validation
- Triggered by a push to `preview`
- Builds and tests; acts as a quality gate before promotion to `main`

### `docker-publish.yml` — Docker image
- Triggered when a GitHub release is **published** (or via `workflow_dispatch` with an optional tag override)
- Pushes a multi-arch (`linux/amd64`, `linux/arm64`) image to `ghcr.io/rebeccapowell/agent-atlas`
- Images include an SBOM and a provenance attestation
- Tag strategy (from `docker/metadata-action`):
  - `1.2.3` — exact version
  - `1.2` — major.minor
  - `1` — major
  - `latest` — stable releases only (not pre-releases)
  - `sha-<short>` — traceability

### `squad-promote.yml` — Branch promotion
- Manual only (`workflow_dispatch` with optional dry-run mode)
- Merges `dev → preview` (stripping internal squad/AI-team files), then `preview → main`

### `squad-docs.yml` — Docs site build & deploy
- Triggered on pushes to `main` that touch `docs/**` or the workflow file itself, and via `workflow_dispatch`
- Builds the `/docs` Jekyll site using `actions/jekyll-build-pages@v1`
- Uploads the built site as a GitHub Pages artifact and deploys it via `actions/deploy-pages@v4`
- Requires **Settings → Pages → Source → GitHub Actions** to be enabled (see [step 4](#4-github-pages-optional))
- Only one deployment runs at a time; in-progress deployments are never cancelled

---

## Required GitHub settings

All settings live under **Repository → Settings**.

---

### 1. Actions: workflow permissions

**Path:** Settings → Actions → General → Workflow permissions

Set to **Read and write permissions**.

This is required for:
- `squad-release.yml` and `squad-insider-release.yml` to push tags and create releases
- `docker-publish.yml` to push images to GitHub Container Registry
- `squad-promote.yml` to push branch merges

> **Screenshot reference:** the toggle is labelled _"Read and write permissions"_ under the _"Workflow permissions"_ heading.

---

### 2. Actions: allow Actions to create PRs

**Path:** Settings → Actions → General → Workflow permissions

Enable **Allow GitHub Actions to create and approve pull requests**.

This is needed if you ever want an automated workflow to open a PR (the promote workflow currently does a direct push, but enabling this future-proofs the setup).

---

### 3. GitHub Container Registry (ghcr.io)

No dedicated secret is needed — the Docker publish workflow authenticates with `secrets.GITHUB_TOKEN` and the `packages: write` permission declared in the workflow.

**Two things to verify:**

#### a. Improved container visibility (public repos)

For public repositories, packages created by the workflow default to **private** unless the package has been previously published with a visibility setting. After the first push, go to:

**Your profile → Packages → agent-atlas → Package settings → Change visibility → Public**

This ensures users can pull the image without authentication.

#### b. Link package to repository

After the first publish, GitHub may not automatically link the `agent-atlas` package to this repository. Link it manually:

**Your profile → Packages → agent-atlas → Package settings → Connect repository → `<owner>/agent-atlas`**

This allows the package to inherit the repository's access control and appear on the repository page.

---

### 4. GitHub Pages (optional)

Only needed if you want `squad-docs.yml` to publish the documentation site.

**Path:** Settings → Pages

| Field | Value |
|---|---|
| Source | **GitHub Actions** |
| Branch | _(not applicable — source is set to GitHub Actions)_ |

The `squad-docs.yml` workflow builds the `/docs` folder as a **Jekyll** site using the official `actions/jekyll-build-pages@v1` action and deploys it to GitHub Pages via `actions/deploy-pages@v4`. No additional configuration is required beyond enabling the GitHub Actions source above.

Each Markdown file under `/docs` must include Jekyll front matter with at least a `title` and `nav_order` field for correct site navigation. For example:

```markdown
---
title: CI/CD Pipelines
nav_order: 6
---
```

The workflow fires automatically on any push to `main` that touches `docs/**` or the workflow file itself. You can also trigger it manually from **Actions → Squad Docs — Build & Deploy → Run workflow**.

---

### 5. Branch protection rules (recommended)

Configure these under **Settings → Branches → Add branch protection rule**.

#### `main`

| Setting | Value |
|---|---|
| Require a pull request before merging | ✅ |
| Require status checks to pass | ✅ — add `Build & Test (.NET 10.0.x)` for all three OS runners |
| Require branches to be up to date | ✅ |
| Do not allow bypassing the above settings | ✅ |
| Restrict who can push to matching branches | Recommended: admins only |

This prevents anyone from pushing directly to `main` and ensures the release workflow only fires on reviewed, tested code.

#### `preview`

Same as `main` but you may relax the direct-push restriction to allow the promote workflow's automated merge.

---

## Secrets and tokens

| Secret | Required | Where to add | Purpose |
|---|---|---|---|
| `GITHUB_TOKEN` | **Automatic** — no setup needed | N/A | Tags, releases, GHCR push, issue management |
| `COPILOT_ASSIGN_TOKEN` | Optional | Settings → Secrets → Actions → New | PAT for assigning `@copilot` to issues in `squad-heartbeat.yml`. Falls back to `GITHUB_TOKEN` if absent. Needs `repo` and `issues` scopes. |

No other secrets are required. All pipelines use the built-in `GITHUB_TOKEN`.

### Creating `COPILOT_ASSIGN_TOKEN` (if needed)

1. Go to **github.com → Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token**
2. Set repository access to **Only select repositories → `<owner>/agent-atlas`**
3. Grant permissions: **Issues: Read and write**, **Contents: Read**
4. Copy the token
5. Add it as a repository secret: **Settings → Secrets and variables → Actions → New repository secret** → Name: `COPILOT_ASSIGN_TOKEN`

---

## .NET SDK version

All workflows use `.NET 10.0.x` with `dotnet-quality: preview`.

.NET 10 is currently in preview. When it reaches GA, remove the `dotnet-quality: preview` line from all four workflow files (`squad-ci.yml`, `squad-release.yml`, `squad-insider-release.yml`, `squad-preview.yml`).

Files to update:

```
.github/workflows/squad-ci.yml
.github/workflows/squad-release.yml
.github/workflows/squad-insider-release.yml
.github/workflows/squad-preview.yml
```

In each, change:

```yaml
- uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'
    dotnet-quality: preview   # ← remove this line when .NET 10 is GA
```

---

## First release — bootstrapping

The `squad-release.yml` workflow auto-increments the latest semver tag. On a fresh repository with no tags, it starts at `v0.1.0`. To start at a different version, create the tag manually before merging to `main` for the first time:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The next push to `main` will then produce `v1.0.1`.

---

## Troubleshooting

### Docker push fails with "permission denied"

Ensure workflow permissions are set to **Read and write** (see [step 1](#1-actions-workflow-permissions)). Also confirm the `packages: write` permission is present in `docker-publish.yml` (it is, by default).

### Tags are not pushed — "refusing to allow a GitHub Actions workflow to create or update a release"

This is the same workflow-permissions issue. Set permissions to **Read and write** in Settings → Actions → General.

### `dorny/test-reporter` check does not appear

`squad-ci.yml` requires `checks: write` permission (already declared in the workflow). If the check still doesn't appear, confirm that the forked PR originates from the same repository — external fork PRs have restricted token permissions by design.

### Promote workflow fails on version detection

The `squad-promote.yml` workflow previously used `node -e "require('./package.json').version"` inherited from the squad template. This has been fixed — it now reads the latest semver git tag instead. No action required.

### `squad-insider-release.yml` creates too many pre-release tags

Tags are created on every push to `insider`. To reduce noise, add a path filter to the trigger:

```yaml
on:
  push:
    branches: [insider]
    paths-ignore:
      - 'docs/**'
      - '**.md'
```
