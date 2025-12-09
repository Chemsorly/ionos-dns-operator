using IonosDnsOperator.Entities;

namespace IonosDnsOperator.Services;

public interface IDnsSyncService
{
	public Task<DnsSyncResult> EnsureCreated(IonosDnsRecord record, CancellationToken cancellationToken);

	public Task<DnsSyncResult> EnsureDeleted(IonosDnsRecord entity, CancellationToken cancellationToken);
}