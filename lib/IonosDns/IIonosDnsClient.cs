namespace IonosDns;

/// <summary>
/// Interface for managing DNS records through the IONOS API with idempotent operations.
/// </summary>
public interface IIonosDnsClient : IDisposable
{
    /// <summary>
    /// Ensures a DNS record exists and matches the specified configuration. Creates or updates the record as needed.
    /// </summary>
    Task<RecordStatus> EnsureRecordMatchesAsync(DnsRecord record, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a DNS record does not exist. Deletes the record if it exists.
    /// </summary>
    Task<RecordStatus> EnsureRecordAbsentAsync(DnsRecord record, bool dryRun = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an existing DNS record from the IONOS API.
    /// </summary>
    Task<DnsRecord?> GetRecordAsync(string rootName, string name, RecordType type, CancellationToken cancellationToken = default);
}
