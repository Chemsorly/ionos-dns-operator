using IonosDnsOperator.Configuration;
using IonosDnsOperator.Entities;
using IonosDnsOperator.Mappers;
using IonosDns;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace IonosDnsOperator.Services;

public record DnsSyncResult
{
	public RecordStatus RecordStatus { get; set; }
}

public class DnsSyncService(IIonosDnsClient ionosDnsClient, DnsRecordMapper dnsRecordMapper) : IDnsSyncService
{
	public async Task<DnsSyncResult> EnsureCreated(IonosDnsRecord entity, CancellationToken cancellationToken)
	{
		var result = await ionosDnsClient.EnsureRecordMatchesAsync(dnsRecordMapper.Map(entity.Spec), cancellationToken: cancellationToken);

		return new DnsSyncResult
		{
			RecordStatus = result
		};
	}

	public async Task<DnsSyncResult> EnsureDeleted(IonosDnsRecord entity, CancellationToken cancellationToken)
	{
		var result = await ionosDnsClient.EnsureRecordAbsentAsync(dnsRecordMapper.Map(entity.Spec), cancellationToken: cancellationToken);

		return new DnsSyncResult
		{
			RecordStatus = result
		};
	}
}
