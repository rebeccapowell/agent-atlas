# Deploy Agent Atlas with Helm

## Prerequisites
- Kubernetes 1.25+
- Helm 3.x
- kubectl configured

## Install

```bash
helm install agent-atlas ./helm/agent-atlas \
  --namespace agent-atlas \
  --create-namespace \
  --set oidc.issuer=https://your-idp.example.com \
  --set oidc.audience=api://agent-atlas
```

## Configuration

See `helm/agent-atlas/values.yaml` for all configurable values.

### Required values

```yaml
oidc:
  issuer: "https://your-idp.example.com"
  audience: "api://agent-atlas"
```

### Catalog mount

The catalog (data-plane repo) must be mounted into the Atlas pod. Two strategies are supported:

#### Option 1: ConfigMap (small catalogs)
```yaml
catalog:
  useConfigMap: true
  configMapName: atlas-catalog
```

Create the ConfigMap from your data-plane repo:
```bash
kubectl create configmap atlas-catalog \
  --from-file=./catalog/ \
  -n agent-atlas
```

#### Option 2: Git-sync sidecar (recommended for production)
```yaml
catalog:
  gitSync:
    enabled: true
    repo: "https://github.com/your-org/your-data-plane-repo.git"
    branch: "main"
    period: "60s"
```

## AKS + GitLab + Calico + KeyVault Profile

### Calico Network Policies
Apply default-deny and allow-from-runner policies:

```yaml
# default-deny.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: default-deny-ingress
  namespace: agent-atlas
spec:
  podSelector: {}
  policyTypes:
    - Ingress
    - Egress
---
# allow-atlas-ingress.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-atlas-ingress
  namespace: agent-atlas
spec:
  podSelector:
    matchLabels:
      app: agent-atlas
  policyTypes:
    - Ingress
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              name: gitlab-runners
      ports:
        - port: 8080
```

### Azure Key Vault (CSI Driver)
```yaml
# values-aks.yaml
secrets:
  provider: azure-keyvault
  keyVaultName: "your-keyvault-name"
  tenantId: "your-tenant-id"
  clientId: "your-managed-identity-client-id"
```

### GitLab CI pipeline for redeploy on merge
```yaml
# .gitlab-ci.yml in your data-plane repo
deploy-atlas:
  stage: deploy
  script:
    - helm upgrade agent-atlas ./helm/agent-atlas
        --namespace agent-atlas
        --reuse-values
        --set image.tag=$CI_COMMIT_SHA
  only:
    - main
```
