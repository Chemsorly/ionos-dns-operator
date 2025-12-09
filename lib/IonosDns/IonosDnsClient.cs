using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IonosDns.Internal;

namespace IonosDns;

/// <summary>
/// Client for managing DNS records through the IONOS API with idempotent operations.
/// </summary>
public class IonosDnsClient : IIonosDnsClient
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the IonosDnsClient class.
    /// </summary>
    /// <param name="options">Configuration options including API key and base URL.</param>
    public IonosDnsClient(IonosDnsClientOptions options) : this(options, null) { }

    /// <summary>
    /// Initializes a new instance of the IonosDnsClient class with a custom HttpClient.
    /// </summary>
    /// <param name="options">Configuration options including API key and base URL.</param>
    /// <param name="httpClient">Optional HttpClient instance for testing or custom configuration.</param>
    public IonosDnsClient(IonosDnsClientOptions options, HttpClient? httpClient)
    {
        _ownsHttpClient = httpClient == null;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "IonosDns/1.0");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Ensures a DNS record exists and matches the specified configuration. Creates or updates the record as needed.
    /// </summary>
    /// <param name="record">The desired DNS record configuration.</param>
    /// <param name="dryRun">If true, simulates the operation without making changes.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>RecordStatus indicating the operation result (Created, Updated, Unchanged, Unauthorized, NotFound, or Conflict).</returns>
    /// <exception cref="IonosDnsException">Thrown when the API request fails with an unexpected error.</exception>
    public async Task<RecordStatus> EnsureRecordMatchesAsync(DnsRecord record, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var existing = await GetRecordAsync(record.RootName, record.Name, record.Type, cancellationToken);
        
        if (existing == null)
        {
            if (dryRun) return RecordStatus.Created;
            return await CreateRecordAsync(record, cancellationToken);
        }
        
        if (RecordsMatch(existing, record))
        {
            return RecordStatus.Unchanged;
        }
        
        if (dryRun) return RecordStatus.Updated;
        return await UpdateRecordAsync(existing, record, cancellationToken);
    }

    /// <summary>
    /// Ensures a DNS record does not exist. Deletes the record if it exists.
    /// </summary>
    /// <param name="record">The DNS record to delete.</param>
    /// <param name="dryRun">If true, simulates the operation without making changes.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>RecordStatus indicating the operation result (Deleted, AlreadyAbsent, Unauthorized, NotFound, or Conflict).</returns>
    /// <exception cref="IonosDnsException">Thrown when the API request fails with an unexpected error.</exception>
    public async Task<RecordStatus> EnsureRecordAbsentAsync(DnsRecord record, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var existing = await GetRecordAsync(record.RootName, record.Name, record.Type, cancellationToken);
        
        if (existing == null)
        {
            return RecordStatus.AlreadyAbsent;
        }
        
        if (dryRun) return RecordStatus.Deleted;
        return await DeleteRecordAsync(existing, cancellationToken);
    }

    /// <summary>
    /// Retrieves an existing DNS record from the IONOS API.
    /// </summary>
    /// <param name="rootName">The zone name (e.g., "example.com").</param>
    /// <param name="name">The record name (e.g., "www.example.com").</param>
    /// <param name="type">The record type (e.g., CNAME, A).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The DNS record if found, otherwise null.</returns>
    /// <exception cref="IonosDnsException">Thrown when the API request fails with an error.</exception>
    public async Task<DnsRecord?> GetRecordAsync(string rootName, string name, RecordType type, CancellationToken cancellationToken = default)
    {
        var zone = await GetZoneByNameAsync(rootName, cancellationToken);
        if (zone == null) return null;

        var response = await _httpClient.GetAsync($"/dns/v1/zones/{zone.Id}", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
            return null;
        }

        var zoneDetail = await response.Content.ReadFromJsonAsync<ZoneResponse>(_jsonOptions, cancellationToken)
            ?? throw new IonosDnsException("Failed to deserialize zone response");

        var record = zoneDetail.Records?.FirstOrDefault(r => 
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
            r.Type.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase));

        return record == null ? null : MapToDnsRecord(record);
    }

    private async Task<ZoneResponse?> GetZoneByNameAsync(string rootName, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync("/dns/v1/zones", cancellationToken);
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        
        if (!response.IsSuccessStatusCode)
        {
            await HandleErrorResponseAsync(response, cancellationToken);
            return null;
        }

        var zones = await response.Content.ReadFromJsonAsync<List<ZoneResponse>>(_jsonOptions, cancellationToken)
            ?? throw new IonosDnsException("Failed to deserialize zones response");

        return zones.FirstOrDefault(z => z.Name.Equals(rootName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<RecordStatus> CreateRecordAsync(DnsRecord record, CancellationToken cancellationToken)
    {
        var zone = await GetZoneByNameAsync(record.RootName, cancellationToken);
        if (zone == null) return RecordStatus.NotFound;

        var request = new RecordRequest
        {
            Name = record.Name,
            Type = record.Type.ToString(),
            Content = record.Content,
            Ttl = record.Ttl,
            Prio = record.Prio,
            Disabled = record.Disabled
        };

        var response = await _httpClient.PostAsJsonAsync($"/dns/v1/zones/{zone.Id}/records", new[] { request }, _jsonOptions, cancellationToken);
        
        if (response.IsSuccessStatusCode)
            return RecordStatus.Created;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return RecordStatus.Unauthorized;
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return RecordStatus.NotFound;
        
        if (response.StatusCode == HttpStatusCode.Conflict)
            return RecordStatus.Conflict;

        await HandleErrorResponseAsync(response, cancellationToken);
        throw new InvalidOperationException("Unreachable");
    }

    private async Task<RecordStatus> UpdateRecordAsync(DnsRecord existing, DnsRecord desired, CancellationToken cancellationToken)
    {
        var zone = await GetZoneByNameAsync(existing.RootName, cancellationToken);
        if (zone == null) return RecordStatus.NotFound;

        var existingRecord = await GetRecordResponseAsync(zone.Id, existing.Name, existing.Type, cancellationToken);
        if (existingRecord == null) return RecordStatus.NotFound;

        var request = new RecordUpdateRequest
        {
            Content = desired.Content,
            Ttl = desired.Ttl,
            Prio = desired.Prio,
            Disabled = desired.Disabled
        };

        var response = await _httpClient.PutAsJsonAsync($"/dns/v1/zones/{zone.Id}/records/{existingRecord.Id}", request, _jsonOptions, cancellationToken);
        
        if (response.IsSuccessStatusCode)
            return RecordStatus.Updated;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return RecordStatus.Unauthorized;
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return RecordStatus.NotFound;
        
        if (response.StatusCode == HttpStatusCode.Conflict)
            return RecordStatus.Conflict;

        await HandleErrorResponseAsync(response, cancellationToken);
        throw new InvalidOperationException("Unreachable");
    }

    private async Task<RecordStatus> DeleteRecordAsync(DnsRecord record, CancellationToken cancellationToken)
    {
        var zone = await GetZoneByNameAsync(record.RootName, cancellationToken);
        if (zone == null) return RecordStatus.NotFound;

        var existingRecord = await GetRecordResponseAsync(zone.Id, record.Name, record.Type, cancellationToken);
        if (existingRecord == null) return RecordStatus.AlreadyAbsent;

        var response = await _httpClient.DeleteAsync($"/dns/v1/zones/{zone.Id}/records/{existingRecord.Id}", cancellationToken);
        
        if (response.IsSuccessStatusCode)
            return RecordStatus.Deleted;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return RecordStatus.Unauthorized;
        
        if (response.StatusCode == HttpStatusCode.NotFound)
            return RecordStatus.NotFound;
        
        if (response.StatusCode == HttpStatusCode.Conflict)
            return RecordStatus.Conflict;

        await HandleErrorResponseAsync(response, cancellationToken);
        throw new InvalidOperationException("Unreachable");
    }

    private async Task<RecordResponse?> GetRecordResponseAsync(string zoneId, string name, RecordType type, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/dns/v1/zones/{zoneId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            return null;

        var zoneDetail = await response.Content.ReadFromJsonAsync<ZoneResponse>(_jsonOptions, cancellationToken);
        
        return zoneDetail?.Records?.FirstOrDefault(r => 
            r.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && 
            r.Type.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    private bool RecordsMatch(DnsRecord existing, DnsRecord desired)
    {
        return existing.Name.Equals(desired.Name, StringComparison.OrdinalIgnoreCase) &&
               existing.Type == desired.Type &&
               existing.Content.Equals(desired.Content, StringComparison.Ordinal) &&
		       existing.Ttl == desired.Ttl &&
		       existing.Prio == desired.Prio &&
		       existing.Disabled == desired.Disabled;
	}

    private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new IonosDnsException($"API request failed with status {response.StatusCode}: {errorContent}");
    }

    private DnsRecord MapToDnsRecord(RecordResponse response)
    {
        return new DnsRecord
        {
            RootName = response.RootName,
            Name = response.Name,
            Type = Enum.Parse<RecordType>(response.Type, ignoreCase: true),
            Content = response.Content,
            Ttl = response.Ttl,
            Prio = response.Prio,
            Disabled = response.Disabled
        };
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
