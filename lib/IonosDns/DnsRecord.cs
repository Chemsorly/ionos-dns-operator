namespace IonosDns;

/// <summary>
/// Represents a DNS record configuration.
/// </summary>
public class DnsRecord
{
    /// <summary>Gets the zone name (e.g., "example.com").</summary>
    public required string RootName { get; init; }
    
    /// <summary>Gets the full record name (e.g., "www.example.com").</summary>
    public required string Name { get; init; }
    
    /// <summary>Gets the DNS record type.</summary>
    public required RecordType Type { get; init; }
    
    /// <summary>Gets the record content/value (e.g., IP address or target hostname).</summary>
    public required string Content { get; init; }
    
    /// <summary>Gets the time-to-live in seconds.</summary>
    public required int Ttl { get; init; }
    
    /// <summary>Gets the priority for MX and SRV records.</summary>
    public int? Prio { get; init; }
    
    /// <summary>Gets whether the record is disabled.</summary>
    public bool Disabled { get; init; }
}
