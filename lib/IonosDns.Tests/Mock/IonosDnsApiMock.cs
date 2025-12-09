using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IonosDns.Internal;

namespace IonosDns.Tests.Mock;

/// <summary>
/// Spec-compliant mock for IONOS DNS API with stateful behavior and validation.
/// </summary>
public class IonosDnsApiMock
{
    private readonly ConcurrentDictionary<string, Zone> _zones = new();
    private readonly string _validApiKey;

    public IonosDnsApiMock(string validApiKey = "test-api-key")
    {
        _validApiKey = validApiKey;
    }

    public void AddZone(string id, string name, string type = "NATIVE")
    {
        _zones[id] = new Zone { Id = id, Name = name, Type = type, Records = new() };
    }

    public HttpClient CreateClient()
    {
        var handler = new MockHandler(this);
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.hosting.ionos.com") };
    }

    internal HttpResponseMessage HandleRequest(HttpRequestMessage request)
    {
        if (!ValidateApiKey(request))
            return ErrorResponse(HttpStatusCode.Unauthorized, "UNAUTHORIZED", "The customer is not authorized to do this operation.");

        var path = request.RequestUri?.AbsolutePath ?? request.RequestUri?.PathAndQuery ?? "";
        var method = request.Method.Method;

        try
        {
            return method switch
            {
                "GET" when path == "/dns/v1/zones" => GetZones(),
                "GET" when Regex.IsMatch(path, @"^/dns/v1/zones/[^/]+$") => GetZone(request),
                "POST" when Regex.IsMatch(path, @"^/dns/v1/zones/[^/]+/records$") => CreateRecords(request).Result,
                "GET" when Regex.IsMatch(path, @"^/dns/v1/zones/[^/]+/records/[^/]+$") => GetRecord(request),
                "PUT" when Regex.IsMatch(path, @"^/dns/v1/zones/[^/]+/records/[^/]+$") => UpdateRecord(request).Result,
                "DELETE" when Regex.IsMatch(path, @"^/dns/v1/zones/[^/]+/records/[^/]+$") => DeleteRecord(request),
                "PATCH" when Regex.IsMatch(path, @"^/dns/v1/zones/[^/]+/records$") => PatchRecords(request).Result,
                "POST" when path == "/dns/v1/dyn-dns" => CreateDynDns(request).Result,
                "GET" when path == "/dns/v1/dyn-dns" => GetDynDns(),
                "DELETE" when Regex.IsMatch(path, @"^/dns/v1/dyn-dns/[^/]+$") => DeleteDynDns(request),
                _ => ErrorResponse(HttpStatusCode.NotFound, "NOT_FOUND", "Endpoint not found.")
            };
        }
        catch (Exception ex)
        {
            return ErrorResponse(HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", ex.Message);
        }
    }

    private bool ValidateApiKey(HttpRequestMessage request)
    {
        return request.Headers.TryGetValues("X-API-Key", out var values) && values.Contains(_validApiKey);
    }

    private HttpResponseMessage GetZones()
    {
        var zones = _zones.Values.Select(z => new { id = z.Id, name = z.Name, type = z.Type }).ToList();
        return JsonResponse(zones);
    }

    private HttpResponseMessage GetZone(HttpRequestMessage request)
    {
        var zoneId = ExtractZoneId(request.RequestUri!.AbsolutePath);
        if (!_zones.TryGetValue(zoneId, out var zone))
            return ErrorResponse(HttpStatusCode.NotFound, "ZONE_NOT_FOUND", "Zone does not exist.");

        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
        var suffix = query["suffix"];
        var recordType = query["recordType"];

        var records = zone.Records.Values.AsEnumerable();
        if (!string.IsNullOrEmpty(suffix))
            records = records.Where(r => r.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(recordType))
            records = records.Where(r => r.Type.Equals(recordType, StringComparison.OrdinalIgnoreCase));

        var response = new
        {
            id = zone.Id,
            name = zone.Name,
            type = zone.Type,
            records = records.Select(r => new
            {
                id = r.Id,
                name = r.Name,
                rootName = r.RootName,
                type = r.Type,
                content = r.Content,
                changeDate = r.ChangeDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                ttl = r.Ttl,
                prio = r.Prio,
                disabled = r.Disabled
            }).ToList()
        };

        return JsonResponse(response);
    }

    private async Task<HttpResponseMessage> CreateRecords(HttpRequestMessage request)
    {
        var zoneId = ExtractZoneId(request.RequestUri!.AbsolutePath);
        if (!_zones.TryGetValue(zoneId, out var zone))
            return ErrorResponse(HttpStatusCode.NotFound, "ZONE_NOT_FOUND", "Zone does not exist.");

        var body = await request.Content!.ReadAsStringAsync();
        var records = JsonSerializer.Deserialize<List<RecordRequest>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (records == null || records.Count == 0)
            return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_DATA", "The request body is invalid or not supported by the endpoint.");

        var created = new List<object>();
        foreach (var record in records)
        {
            var validation = ValidateRecord(record);
            if (validation != null)
                return validation;

            var normalizedType = NormalizeRecordType(record.Type);
            var contentValidation = ValidateContent(normalizedType, record.Content, record.Prio);
            if (contentValidation != null)
                return contentValidation;

            var id = Guid.NewGuid().ToString();
            var newRecord = new Record
            {
                Id = id,
                Name = record.Name,
                RootName = zone.Name,
                Type = normalizedType,
                Content = record.Content,
                Ttl = record.Ttl > 0 ? record.Ttl : 3600,
                Prio = record.Prio,
                Disabled = record.Disabled,
                ChangeDate = DateTime.UtcNow
            };

            zone.Records[id] = newRecord;

            created.Add(new
            {
                id = newRecord.Id,
                name = newRecord.Name,
                rootName = newRecord.RootName,
                type = newRecord.Type,
                content = newRecord.Content,
                changeDate = newRecord.ChangeDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
                ttl = newRecord.Ttl,
                prio = newRecord.Prio,
                disabled = newRecord.Disabled
            });
        }

        return JsonResponse(created);
    }

    private HttpResponseMessage GetRecord(HttpRequestMessage request)
    {
        var (zoneId, recordId) = ExtractZoneAndRecordId(request.RequestUri!.AbsolutePath);
        if (!_zones.TryGetValue(zoneId, out var zone))
            return ErrorResponse(HttpStatusCode.NotFound, "ZONE_NOT_FOUND", "Zone does not exist.");

        if (!zone.Records.TryGetValue(recordId, out var record))
            return ErrorResponse(HttpStatusCode.NotFound, "RECORD_NOT_FOUND", "Record does not exist.");

        var response = new
        {
            id = record.Id,
            name = record.Name,
            rootName = record.RootName,
            type = record.Type,
            content = record.Content,
            changeDate = record.ChangeDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            ttl = record.Ttl,
            prio = record.Prio,
            disabled = record.Disabled
        };

        return JsonResponse(response);
    }

    private async Task<HttpResponseMessage> UpdateRecord(HttpRequestMessage request)
    {
        var (zoneId, recordId) = ExtractZoneAndRecordId(request.RequestUri!.AbsolutePath);
        if (!_zones.TryGetValue(zoneId, out var zone))
            return ErrorResponse(HttpStatusCode.NotFound, "ZONE_NOT_FOUND", "Zone does not exist.");

        if (!zone.Records.TryGetValue(recordId, out var record))
            return ErrorResponse(HttpStatusCode.NotFound, "RECORD_NOT_FOUND", "Record does not exist.");

        var body = await request.Content!.ReadAsStringAsync();
        var update = JsonSerializer.Deserialize<RecordUpdateRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (update == null)
            return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_DATA", "The request body is invalid or not supported by the endpoint.");

        var contentValidation = ValidateContent(record.Type, update.Content, update.Prio);
        if (contentValidation != null)
            return contentValidation;

        record.Content = update.Content;
        record.Ttl = update.Ttl > 0 ? update.Ttl : record.Ttl;
        record.Prio = update.Prio;
        record.Disabled = update.Disabled;
        record.ChangeDate = DateTime.UtcNow;

        var response = new
        {
            id = record.Id,
            name = record.Name,
            rootName = record.RootName,
            type = record.Type,
            content = record.Content,
            changeDate = record.ChangeDate.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"),
            ttl = record.Ttl,
            prio = record.Prio,
            disabled = record.Disabled
        };

        return JsonResponse(response);
    }

    private HttpResponseMessage DeleteRecord(HttpRequestMessage request)
    {
        var (zoneId, recordId) = ExtractZoneAndRecordId(request.RequestUri!.AbsolutePath);
        if (!_zones.TryGetValue(zoneId, out var zone))
            return ErrorResponse(HttpStatusCode.NotFound, "ZONE_NOT_FOUND", "Zone does not exist.");

        if (!zone.Records.Remove(recordId, out _))
            return ErrorResponse(HttpStatusCode.NotFound, "RECORD_NOT_FOUND", "Record does not exist.");

        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    private async Task<HttpResponseMessage> PatchRecords(HttpRequestMessage request)
    {
        var zoneId = ExtractZoneId(request.RequestUri!.AbsolutePath);
        if (!_zones.TryGetValue(zoneId, out var zone))
            return ErrorResponse(HttpStatusCode.NotFound, "ZONE_NOT_FOUND", "Zone does not exist.");

        var body = await request.Content!.ReadAsStringAsync();
        var records = JsonSerializer.Deserialize<List<RecordRequest>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (records == null || records.Count == 0)
            return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_DATA", "The request body is invalid or not supported by the endpoint.");

        foreach (var record in records)
        {
            var normalizedType = NormalizeRecordType(record.Type);
            var toRemove = zone.Records.Values.Where(r => r.Name == record.Name && r.Type == normalizedType).ToList();
            foreach (var r in toRemove)
                zone.Records.Remove(r.Id, out _);
        }

        return await CreateRecords(request);
    }

    private async Task<HttpResponseMessage> CreateDynDns(HttpRequestMessage request)
    {
        var body = await request.Content!.ReadAsStringAsync();
        var dynDns = JsonSerializer.Deserialize<Dictionary<string, object>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (dynDns == null || !dynDns.ContainsKey("domains"))
            return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_DATA", "The request body is invalid or not supported by the endpoint.");

        var bulkId = Guid.NewGuid().ToString();
        var response = new
        {
            bulkId,
            updateUrl = $"https://ipv4.api.hosting.ionos.com/dns/v1/dyn-dns/{bulkId}",
            domains = dynDns["domains"],
            description = dynDns.ContainsKey("description") ? dynDns["description"] : null
        };

        return JsonResponse(response);
    }

    private HttpResponseMessage GetDynDns()
    {
        return JsonResponse(new List<object>());
    }

    private HttpResponseMessage DeleteDynDns(HttpRequestMessage request)
    {
        return new HttpResponseMessage(HttpStatusCode.NoContent);
    }

    private HttpResponseMessage? ValidateRecord(RecordRequest record)
    {
        var missing = new List<string>();
        if (string.IsNullOrEmpty(record.Name)) missing.Add("name");
        if (string.IsNullOrEmpty(record.Type)) missing.Add("type");
        if (string.IsNullOrEmpty(record.Content)) missing.Add("content");

        if (missing.Count > 0)
            return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_RECORD", "Record is invalid.", new { requiredFields = missing });

        return null;
    }

    private HttpResponseMessage? ValidateContent(string type, string content, int? prio)
    {
        var valid = type switch
        {
            "A" => Regex.IsMatch(content, @"^(\d{1,3}\.){3}\d{1,3}$"),
            "AAAA" => Regex.IsMatch(content, @"^([0-9a-fA-F]{0,4}:){2,7}[0-9a-fA-F]{0,4}$"),
            "CNAME" or "NS" => Regex.IsMatch(content, @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}\.?$"),
            "MX" => Regex.IsMatch(content, @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}\.?$") && prio.HasValue,
            "TXT" => true,
            "CAA" => Regex.IsMatch(content, @"^\d+\s+\w+\s+""[^""]+""$"),
            "SRV" => Regex.IsMatch(content, @"^\d+\s+\d+\s+\d+\s+\S+$") && prio.HasValue,
            _ => true
        };

        if (!valid)
            return ErrorResponse(HttpStatusCode.BadRequest, "INVALID_DATA", $"Invalid content format for {type} record.");

        return null;
    }

    private string NormalizeRecordType(string type) => type.ToUpperInvariant();

    private string ExtractZoneId(string path) => path.Split('/')[4];

    private (string zoneId, string recordId) ExtractZoneAndRecordId(string path)
    {
        var parts = path.Split('/');
        return (parts[4], parts[6]);
    }

    private HttpResponseMessage JsonResponse(object data, HttpStatusCode status = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(data);
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private HttpResponseMessage ErrorResponse(HttpStatusCode status, string code, string message, object? parameters = null)
    {
        var error = new { code, message, parameters };
        var errors = new[] { error };
        return JsonResponse(errors, status);
    }

    private class MockHandler : HttpMessageHandler
    {
        private readonly IonosDnsApiMock _mock;

        public MockHandler(IonosDnsApiMock mock) => _mock = mock;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_mock.HandleRequest(request));
    }

    private class Zone
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Type { get; init; }
        public required ConcurrentDictionary<string, Record> Records { get; init; }
    }

    private class Record
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string RootName { get; set; }
        public required string Type { get; set; }
        public required string Content { get; set; }
        public required int Ttl { get; set; }
        public int? Prio { get; set; }
        public bool Disabled { get; set; }
        public DateTime ChangeDate { get; set; }
    }
}
