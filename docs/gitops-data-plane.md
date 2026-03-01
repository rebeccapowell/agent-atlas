---
title: GitOps Data Plane
nav_order: 4
---

# GitOps Data Plane Repo Structure

Agent Atlas uses a Git repository as its "data plane database". All catalog metadata and OpenAPI specs live in this repo.

## Directory Structure

```
/
├── catalog.yaml              # Global catalog metadata
├── apis/
│   ├── <api-id>/
│   │   ├── api.yaml          # API metadata
│   │   └── openapi.yaml      # OpenAPI 3.x spec
│   └── ...
└── policies/
    └── defaults.yaml         # Platform-level policy overrides
```

## catalog.yaml

```yaml
version: 1
organizationName: "Your Organization"
defaultSafetyTierPolicy: read-only
```

## apis/<api-id>/api.yaml

```yaml
apiId: my-api            # Must match directory name
displayName: "My API"
owner: "my-team"
description: "What this API does"
baseUrl: "https://api.example.com"
environments:
  development: "https://api-dev.example.com"
  production: "https://api.example.com"
```

## apis/<api-id>/openapi.yaml

Use standard OpenAPI 3.x with the `x-mcp` vendor extension to mark operations as tools:

```yaml
openapi: "3.0.3"
info:
  title: My API
  version: "1.0.0"
paths:
  /items:
    get:
      operationId: ListItems
      summary: List items
      x-mcp:
        enabled: true
        name: "my-api.items.list"
        safety: read
        requiredPermissions:
          - "my-api:items:read"
        entitlementHint: "Request Access Package: My API - Readers"
```

## x-mcp Extension Reference

| Field | Required | Description |
|-------|----------|-------------|
| `enabled` | Yes | Set to `true` to expose as a tool |
| `name` | Recommended | Stable tool ID (e.g., `api.resource.action`) |
| `safety` | Yes | `read`, `write`, or `destructive` |
| `requiredPermissions` | Yes | Array of permission strings (informational) |
| `entitlementHint` | No | Human-readable access request instructions |
| `tags` | No | Additional tags for search/filtering |
| `description` | No | Override the operation description |

## GitOps Workflow

1. Developer submits PR to add/modify a tool definition
2. PR is reviewed and approved
3. Merge triggers deployment pipeline
4. Pipeline runs `helm upgrade` which causes Atlas pod to restart
5. Atlas loads the new catalog on startup
