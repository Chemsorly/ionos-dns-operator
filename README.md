# ionos-dns-operator
Little kubernetes operator based on .NET 10 and [Kubeops dotnet-operator](https://github.com/dotnet/dotnet-operator-sdk). It brings its own CRD [C#](./IonosDnsOperator/Entities/IonosDnsRecord.cs)/[YAML](./IonosDnsOperator/example-ionosdnsrecord.yaml), which is synced with the [IONOS DNS Api](https://developer.hosting.ionos.com/docs/dns). Provides reconciliation and finalizer support. Uses ValidatingWebhookConfiguration to enforce immutability on Spec.RootName, Spec.Name and Spec.Type.

# Requirements
- .NET 10.0 or later
- ([Get your IONOS API key here](https://developer.hosting.ionos.com/docs/getstarted))

# Disclaimer
Parts of the operator and most parts of library have been written with Kiro / Claude Sonnet 4.5.