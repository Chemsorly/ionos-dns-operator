using IonosDnsOperator.Entities;
using IonosDns;
using Riok.Mapperly.Abstractions;

namespace IonosDnsOperator.Mappers;

[Mapper]
public partial class DnsRecordMapper
{
	[MapProperty(nameof(IonosDnsRecord.EntitySpec.Ttl), nameof(DnsRecord.Ttl))]
	public partial DnsRecord Map(IonosDnsRecord.EntitySpec spec);
}
