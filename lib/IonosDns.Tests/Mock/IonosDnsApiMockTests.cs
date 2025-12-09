using System.Net.Http.Json;

namespace IonosDns.Tests.Mock;

public class IonosDnsApiMockTests
{
    [Fact]
    public async Task GetZones_ReturnsZones()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");
        
        using var httpClient = mock.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        var response = await httpClient.GetAsync("/dns/v1/zones");
        
        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("zone1", content);
        Assert.Contains("example.com", content);
    }

    [Fact]
    public async Task GetZone_ReturnsZoneWithRecords()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");
        
        using var httpClient = mock.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        var response = await httpClient.GetAsync("/dns/v1/zones/zone1");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Content: {content}");
        Assert.Contains("zone1", content);
        Assert.Contains("records", content);
    }

    [Fact]
    public async Task CreateRecords_ValidatesApiKey()
    {
        var mock = new IonosDnsApiMock("valid-key");
        mock.AddZone("zone1", "example.com");
        
        using var httpClient = mock.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", "invalid-key");
        
        var response = await httpClient.PostAsJsonAsync("/dns/v1/zones/zone1/records", new[] { new { name = "test.example.com", type = "A", content = "1.2.3.4" } });
        
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecords_CreatesRecord()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");
        
        using var httpClient = mock.CreateClient();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        
        var response = await httpClient.PostAsJsonAsync("/dns/v1/zones/zone1/records", new[] { new { name = "test.example.com", type = "A", content = "1.2.3.4", ttl = 3600 } });
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Status: {response.StatusCode}, Content: {content}");
        Assert.Contains("test.example.com", content);
        Assert.Contains("1.2.3.4", content);
    }
}
