using IonosDns;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace IonosDnsOperator.Entities;

[KubernetesEntity(Group = "ionos-dns-operator.chemsorly.com", ApiVersion = "v1", Kind = "IonosDnsRecord")]
[EntityScope(EntityScope.Namespaced)]
public class IonosDnsRecord : CustomKubernetesEntity<IonosDnsRecord.EntitySpec, IonosDnsRecord.EntityStatus>
{
	public override string ToString() => $"IonosDnsRecord ({Metadata.Name}): {Spec.Name} ({Spec.RootName}) {Spec.Type} {Spec.Content}";

	public class EntitySpec
	{
		[Required]
		[Description("The root zone name")]
		public string RootName { get; init; } = String.Empty;

		[Required]
		[Description("The record name")]
		public string Name { get; init; } = String.Empty;

		[Required]
		[Description("The record type")]
		public RecordType Type { get; init; } = RecordType.UNKNOWN;

		[Required]
		[Description("The content of the record")]
		public string Content { get; init; } = String.Empty;

		[Description("The time to live for the record")]
		public int? Ttl { get; init; } = 3600;

		public int? Prio { get; init; }

		public bool Disabled { get; init; }
	}

	public class EntityStatus
	{
		[Description("Last time the entity was changed")]
		public DateTimeOffset LastChanged { get; set; }

		[Description("Last time the entity was reconciled")]
		public DateTimeOffset LastReconciled { get; set; }

		[Description("Last known DNS record status")]
		[AdditionalPrinterColumn]
		public RecordStatus DnsRecordStatus { get; set; } 
	}
}
