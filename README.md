# ionos-dns-operator
Little kubernetes operator based on .NET 10 and [Kubeops dotnet-operator](https://github.com/dotnet/dotnet-operator-sdk). It brings its own CRD [C#](./IonosDnsOperator/Entities/IonosDnsRecord.cs)/[YAML](./IonosDnsOperator/example-ionosdnsrecord.yaml), which is synced with the [IONOS DNS Api](https://developer.hosting.ionos.com/docs/dns). Provides reconciliation and finalizer support. Uses ValidatingWebhookConfiguration to enforce immutability on Spec.RootName, Spec.Name and Spec.Type.

# Getting started
Helm chart requires a cert-manager deployment or manually added certificate for the webhook.  

# Installing
```
helm repo add helm-charts https://chemsorly.github.io/ionos-dns-operator/
helm repo update
helm search repo helm-charts
```
or via dependency
```
# Chart.yaml
dependencies:
- name: ionos-dns-operator
  version: '0.0.5'
  repository: https://chemsorly.github.io/ionos-dns-operator/
```


# Development
- .NET 10.0 or later
- ([Get your IONOS API key here](https://developer.hosting.ionos.com/docs/getstarted))

# Disclaimer
Parts of the operator and most parts of library have been written with Kiro / Claude Sonnet 4.5.