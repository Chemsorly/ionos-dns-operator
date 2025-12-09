using IonosDnsOperator.Controllers;
using IonosDnsOperator.Entities;
using IonosDnsOperator.Services;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Finalizer;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace IonosDnsOperator.Finalizers;

public class IonosDnsRecordFinalizer(ILogger<IonosDnsRecordFinalizer> logger, IDnsSyncService dnsSyncService, IKubernetesClient kubernetesClient) : IEntityFinalizer<IonosDnsRecord>
{
	public async Task<ReconciliationResult<IonosDnsRecord>> FinalizeAsync(IonosDnsRecord entity, CancellationToken cancellationToken)
	{
		// Implement your cleanup logic here
		logger.LogTrace("Finalizer loop start");
		var result = await dnsSyncService.EnsureDeleted(entity, cancellationToken);

		switch (result.RecordStatus)
		{
			case IonosDns.RecordStatus.AlreadyAbsent:
			case IonosDns.RecordStatus.Deleted:
				return ReconciliationResult<IonosDnsRecord>.Success(entity);
			default:
				return ReconciliationResult<IonosDnsRecord>.Failure(entity, $"Failed FinalizeAsync with RecordStatus {result.RecordStatus}");
		}
	}
}
