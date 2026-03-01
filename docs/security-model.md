---
title: Security Model
nav_order: 3
---

# Security Model: Platform Permissions vs Downstream Auth

## Overview

Agent Atlas uses a two-layer authorization model:

1. **Platform-level authorization** (enforced by Atlas)
2. **Downstream API authorization** (enforced by the downstream API)

## Platform-Level Authorization

Atlas enforces whether a caller can *use* Atlas at all.

### Required permissions

| Permission | Required for |
|-----------|-------------|
| `platform-code-mode:search` | Searching/browsing the tool catalog |
| `platform-code-mode:execute` | Executing tool plans |
| `platform-code-mode:execute:write` | Executing write/destructive tools (optional gate) |

### Token validation

Atlas validates incoming JWTs:
- Signature verified against OIDC provider's JWKS
- `iss` in configured allowlist
- `aud` matches configured audience
- `exp` not expired (with 30s clock skew)

### Permission claim mapping

Configure which JWT claim contains permissions:

```yaml
# appsettings.json or environment variables
Atlas:
  PlatformPermissions:
    Claim: "scp"  # or "roles", or custom claim name
```

The claim can be space-delimited (`"search execute"`) or array-style.

## Downstream Authorization

Atlas does **not** enforce downstream API permissions.

- `x-mcp.requiredPermissions` is **informational metadata only**
- Atlas displays required permissions in the UI for visibility
- Atlas computes an access hint (`granted/missing/unknown`) from the caller's token claims
- The downstream API is responsible for enforcing authorization
- Atlas forwards the caller's JWT as-is: `Authorization: Bearer <token>`

### Downstream 401/403 handling

When a downstream API returns 401 or 403:
- Atlas surfaces the error as a downstream error (not an Atlas auth error)
- The error message distinguishes between Atlas authorization failures and downstream failures

## Security Boundaries

```
Caller → [Atlas JWT validation] → [Platform permission check] → [Execute plan]
                                                                      ↓
                                                          [Downstream API]
                                                          [Enforces its own auth]
```

Atlas only calls base URLs declared in the catalog repo. Arbitrary hostnames in plans are rejected.
