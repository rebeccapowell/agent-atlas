# Deploy Agent Atlas with Docker

## Prerequisites
- Docker 20.10+
- A data-plane repo checked out locally

## Quick Start

### 1. Build the Docker image
```bash
docker build -t agent-atlas:latest .
```

### 2. Run with a mounted catalog directory
```bash
docker run -d \
  -p 8080:8080 \
  -v /path/to/your/data-plane-repo:/catalog:ro \
  -e Atlas__CatalogPath=/catalog \
  -e Atlas__Oidc__Issuer=https://your-idp.example.com \
  -e Atlas__Oidc__Audience=api://agent-atlas \
  -e Atlas__PlatformPermissions__Claim=scp \
  --name agent-atlas \
  agent-atlas:latest
```

### 3. Verify it's running
```bash
curl http://localhost:8080/healthz
curl http://localhost:8080/v1/tools
```

## Configuration

All configuration can be passed as environment variables using the `__` separator for nested config:

| Variable | Default | Description |
|----------|---------|-------------|
| `Atlas__CatalogPath` | `/catalog` | Path to the mounted data-plane repo |
| `Atlas__CatalogStrict` | `true` | Fail on catalog parse errors |
| `Atlas__Oidc__Issuer` | - | OIDC issuer URL |
| `Atlas__Oidc__Audience` | `api://agent-atlas` | Expected JWT audience |
| `Atlas__PlatformPermissions__Claim` | `scp` | JWT claim for permissions |
| `Atlas__ExecLimits__MaxSteps` | `50` | Max plan steps per execution |
| `Atlas__ExecLimits__MaxCalls` | `50` | Max HTTP calls per execution |
| `Atlas__ExecLimits__MaxSeconds` | `30` | Max wall-clock seconds per execution |

## Baking the catalog into the image

Alternatively, copy your data-plane repo into the image at build time:

```dockerfile
FROM your-registry/agent-atlas:latest
COPY --chown=app:app ./my-data-plane-repo /catalog
```
