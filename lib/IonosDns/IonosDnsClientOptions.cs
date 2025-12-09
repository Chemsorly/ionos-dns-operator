namespace IonosDns;

/// <summary>
/// Configuration options for the IONOS DNS client.
/// </summary>
public class IonosDnsClientOptions
{
    /// <summary>Gets the IONOS API key for authentication.</summary>
    public required string ApiKey { get; init; }
    
    /// <summary>Gets the base URL for the IONOS API. Defaults to "https://api.hosting.ionos.com".</summary>
    public string BaseUrl { get; init; } = "https://api.hosting.ionos.com";
}
