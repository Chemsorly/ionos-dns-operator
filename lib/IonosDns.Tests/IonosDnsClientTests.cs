using IonosDns.Internal;
using IonosDns.Tests.Mock;

namespace IonosDns.Tests;

public class IonosDnsClientTests
{
    private readonly IonosDnsClientOptions _options = new() { ApiKey = "test-api-key" };

    [Fact]
    public async Task EnsureRecordMatchesAsync_RecordDoesNotExist_ReturnsCreated()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var client = new IonosDnsClient(_options, mock.CreateClient());
        
        var record = new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "target.com",
            Ttl = 3600
        };

        var result = await client.EnsureRecordMatchesAsync(record);
        
        Assert.Equal(RecordStatus.Created, result);
    }

    [Fact]
    public async Task EnsureRecordMatchesAsync_RecordExists_ReturnsUnchanged()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var httpClient = mock.CreateClient();
        using var client = new IonosDnsClient(_options, httpClient);
        
        await client.EnsureRecordMatchesAsync(new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "target.com",
            Ttl = 3600
        });
        
        var record = new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "target.com",
            Ttl = 3600
        };

        var result = await client.EnsureRecordMatchesAsync(record);
        
        Assert.Equal(RecordStatus.Unchanged, result);
    }

    [Fact]
    public async Task EnsureRecordMatchesAsync_RecordDiffers_ReturnsUpdated()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var httpClient = mock.CreateClient();
        using var client = new IonosDnsClient(_options, httpClient);
        
        await client.EnsureRecordMatchesAsync(new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "old-target.com",
            Ttl = 3600
        });
        
        var record = new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "target.com",
            Ttl = 3600
        };

        var result = await client.EnsureRecordMatchesAsync(record);
        
        Assert.Equal(RecordStatus.Updated, result);
    }

    [Fact]
    public async Task EnsureRecordMatchesAsync_DryRun_DoesNotCreate()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var client = new IonosDnsClient(_options, mock.CreateClient());
        
        var record = new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "target.com",
            Ttl = 3600
        };

        var result = await client.EnsureRecordMatchesAsync(record, dryRun: true);
        
        Assert.Equal(RecordStatus.Created, result);
        
        var existing = await client.GetRecordAsync("example.com", "test.example.com", RecordType.CNAME);
        Assert.Null(existing);
    }

    [Fact]
    public async Task EnsureRecordAbsentAsync_RecordExists_ReturnsDeleted()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var httpClient = mock.CreateClient();
        using var client = new IonosDnsClient(_options, httpClient);
        
        await client.EnsureRecordMatchesAsync(new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "target.com",
            Ttl = 3600
        });

        var result = await client.EnsureRecordAbsentAsync(new DnsRecord() { RootName = "example.com", Name = "test.example.com", Type = RecordType.CNAME, Content = "", Ttl = 3600 });
        
        Assert.Equal(RecordStatus.Deleted, result);
    }

    [Fact]
    public async Task EnsureRecordAbsentAsync_RecordDoesNotExist_ReturnsAlreadyAbsent()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var client = new IonosDnsClient(_options, mock.CreateClient());

        var result = await client.EnsureRecordAbsentAsync(new DnsRecord() { RootName = "example.com", Name = "test.example.com", Type = RecordType.CNAME, Content = "", Ttl = 3600 });
        
        Assert.Equal(RecordStatus.AlreadyAbsent, result);
    }

    [Fact]
    public async Task GetRecordAsync_RecordExists_ReturnsRecord()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var httpClient = mock.CreateClient();
        using var client = new IonosDnsClient(_options, httpClient);
        
        await client.EnsureRecordMatchesAsync(new DnsRecord
        {
            RootName = "example.com",
            Name = "test.example.com",
            Type = RecordType.CNAME,
            Content = "target.com",
            Ttl = 3600
        });

        var result = await client.GetRecordAsync("example.com", "test.example.com", RecordType.CNAME);
        
        Assert.NotNull(result);
        Assert.Equal("test.example.com", result.Name);
        Assert.Equal("target.com", result.Content);
    }

    [Fact]
    public async Task GetRecordAsync_RecordDoesNotExist_ReturnsNull()
    {
        var mock = new IonosDnsApiMock();
        mock.AddZone("zone1", "example.com");

        using var client = new IonosDnsClient(_options, mock.CreateClient());

        var result = await client.GetRecordAsync("example.com", "test.example.com", RecordType.CNAME);
        
        Assert.Null(result);
    }
}
