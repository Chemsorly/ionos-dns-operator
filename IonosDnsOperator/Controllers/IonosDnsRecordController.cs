using IonosDnsOperator.Entities;
using IonosDnsOperator.Services;
using KubeOps.Abstractions.Events;
using KubeOps.Abstractions.Rbac;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace IonosDnsOperator.Controllers;

[EntityRbac(typeof(IonosDnsRecord), Verbs = RbacVerb.All)]
public class IonosDnsRecordController(ILogger<IonosDnsRecordController> logger, 
	IDnsSyncService dnsSyncService, 
	IKubernetesClient kubernetesClient,
	EventPublisher eventPublisher) : IEntityController<IonosDnsRecord>
{	public async Task<ReconciliationResult<IonosDnsRecord>> ReconcileAsync(IonosDnsRecord entity, CancellationToken cancellationToken)
	{
		// Implement your reconciliation logic here
		logger.LogTrace("Reconciliation loop start");

		var result = await dnsSyncService.EnsureCreated(entity, cancellationToken);
		entity.Status.DnsRecordStatus = result.RecordStatus;
		entity.Status.LastReconciled = DateTime.UtcNow;

		switch (result.RecordStatus)
		{
			case IonosDns.RecordStatus.Created:
			case IonosDns.RecordStatus.Updated:
				entity.Status.LastChanged = DateTime.UtcNow;
				await kubernetesClient.UpdateStatusAsync(entity, cancellationToken);
				await eventPublisher(entity,"Reconciled", $"IonosDnsRecord '{entity.Metadata.NamespaceProperty}/{entity.Metadata.Name}' was successfully reconciled", EventType.Normal, cancellationToken);
				return ReconciliationResult<IonosDnsRecord>.Success(entity);
			case IonosDns.RecordStatus.Unchanged:
				await kubernetesClient.UpdateStatusAsync(entity, cancellationToken);
				await eventPublisher(entity, "Reconciled", $"IonosDnsRecord '{entity.Metadata.NamespaceProperty}/{entity.Metadata.Name}' was successfully reconciled", EventType.Normal, cancellationToken);
				return ReconciliationResult<IonosDnsRecord>.Success(entity);
			default:
				await kubernetesClient.UpdateStatusAsync(entity, cancellationToken);
				await eventPublisher(entity, "Reconciled", $"IonosDnsRecord '{entity.Metadata.NamespaceProperty}/{entity.Metadata.Name}' reconciliation failed.", EventType.Warning, cancellationToken);
				return ReconciliationResult<IonosDnsRecord>.Failure(entity, $"Failed EnsureCreated with RecordStatus {result.RecordStatus}");
		}
	}

	public async Task<ReconciliationResult<IonosDnsRecord>> DeletedAsync(IonosDnsRecord entity, CancellationToken cancellationToken)
	{		
		// Handle deletion event
		logger.LogTrace("Deletion event fired. Do nothing.");
		return ReconciliationResult<IonosDnsRecord>.Success(entity);
	}
}
