# ionos-dns-operator Helm Chart

Kubernetes operator for IONOS DNS management based on .NET 10 and KubeOps.

## Configuration

The following table lists the configurable parameters of the chart and their default values.

| Parameter | Description | Default |
|-----------|-------------|---------|
| `image.repository` | Container image repository | `chemsorly/ionos-dns-operator` |
| `image.tag` | Container image tag | `v0.0.1` |
| `image.pullPolicy` | Image pull policy | `IfNotPresent` |
| `replicaCount` | Number of replicas | `1` |
| `serviceAccount.create` | Create service account | `true` |
| `serviceAccount.name` | Service account name (auto-generated if empty) | `""` |
| `ionos.apiKey` | IONOS DNS API key | `""` |
| `ionos.existingSecret` | Name of existing secret containing apiKey | `""` |
| `certManager.enabled` | Use cert-manager for certificates | `true` |
| `certManager.issuerName` | cert-manager Issuer name | `selfsigned-issuer` |
| `webhook.caBundle` | Base64 CA bundle (when certManager disabled) | `""` |
| `webhook.secretName` | Secret name for certificates (when certManager disabled) | `""` |
| `webhook.config.httpUrl` | HTTP endpoint URL | `http://0.0.0.0:5000` |
| `webhook.config.httpsUrl` | HTTPS endpoint URL | `https://0.0.0.0:5001` |
| `webhook.config.certPath` | Certificate file path | `/certs/tls.crt` |
| `webhook.config.keyPath` | Private key file path | `/certs/tls.key` |
| `resources.limits.cpu` | CPU limit | `100m` |
| `resources.limits.memory` | Memory limit | `128Mi` |
| `resources.requests.cpu` | CPU request | `100m` |
| `resources.requests.memory` | Memory request | `64Mi` |

## IONOS API Key

### Option 1: Provide API key directly
```bash
helm install ionos-dns-operator ./charts/ionos-dns-operator \
  --set ionos.apiKey=YOUR_API_KEY \
  -n ionos-dns-operator --create-namespace
```

### Option 2: Use existing secret
```bash
# Create secret with apiKey key
kubectl create secret generic ionos-api-secret \
  --from-literal=apiKey=YOUR_API_KEY \
  -n ionos-dns-operator

# Install chart referencing the secret
helm install ionos-dns-operator ./charts/ionos-dns-operator \
  --set ionos.existingSecret=ionos-api-secret \
  -n ionos-dns-operator --create-namespace
```

## More Information

- [GitHub Repository](https://github.com/Chemsorly/ionos-dns-operator)
- [IONOS DNS API Documentation](https://developer.hosting.ionos.com/docs/dns)
