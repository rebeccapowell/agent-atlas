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

---

## Keycloak OAuth2 clients

The local development Keycloak realm (`src/Atlas.AppHost/keycloak/atlas-realm.json`) ships with three pre-configured clients:

| Client ID | Type | Flows | Purpose |
|-----------|------|-------|---------|
| `atlas-mcp-client` | Confidential | `client_credentials` **and** authorization code + PKCE | M2M service-account access **and** guided/interactive auth via MCP Inspector. PKCE (`S256`) is enforced even on this confidential client for defence-in-depth. |
| `mcp-inspector` | Public (PKCE) | Authorization code + PKCE | Dedicated public client for interactive MCP Inspector sessions. |
| `atlas-ui-client` | Public (PKCE) | Authorization code + PKCE | Atlas React UI (future use). |

`atlas-mcp-client` carries three default scopes: `platform-code-mode:search`, `platform-code-mode:execute`, and `someapi:customers:read`.

### Seeded group and developer account

The realm import also creates an `atlas-developers` group and a single pre-configured user for local development and demos:

| Field | Value |
|-------|-------|
| **Username** | `developer` |
| **Password** | `developer` |
| **Email** | `developer@example.test` |
| **Group** | `atlas-developers` |

This account is created automatically when Aspire starts Keycloak — no manual setup is required. When signing in through MCP Inspector's guided PKCE flow the account receives all three default scopes (`platform-code-mode:search`, `platform-code-mode:execute`, `someapi:customers:read`) from the `mcp-inspector` client's default scope configuration.

{: .warning }
The seeded `developer` account is **for local development and demos only**. Do not rely on it in any shared, staging, or production environment. For non-local deployments, remove this user from the realm import or replace its credentials and group membership before exposing the system to other users.

### Guided OAuth2 discovery via `ProtectedResourceMetadata`

When an unauthenticated request reaches `/mcp`, Atlas returns:

```
HTTP/1.1 401 Unauthorized
WWW-Authenticate: Bearer resource_metadata="<issuer>/.well-known/openid-configuration"
```

The `ProtectedResourceMetadata` advertises:
- **`AuthorizationServers`** — the Keycloak realm URL, so MCP Inspector can auto-discover the token and authorization endpoints.
- **`ScopesSupported`** — `platform-code-mode:search` and `platform-code-mode:execute`, so MCP Inspector pre-fills the required scopes in its guided/quick OAuth flow without manual input.

This means MCP Inspector can complete the entire OAuth2 PKCE exchange automatically — no manual token copying required.
