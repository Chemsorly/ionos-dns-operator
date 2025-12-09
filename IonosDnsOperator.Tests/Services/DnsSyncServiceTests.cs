using IonosDns;
using IonosDnsOperator.Configuration;
using IonosDnsOperator.Mappers;
using IonosDnsOperator.Services;
using IonosDnsOperator.Tests.TestHelpers;
using Microsoft.Extensions.Options;

namespace IonosDnsOperator.Tests.Services;

public class DnsSyncServiceTests
{
    private readonly Mock<IIonosDnsClient> _clientMock = new();
    private readonly DnsRecordMapper _mapper = new();
    private readonly DnsSyncService _service;

    public DnsSyncServiceTests()
    {
        _service = new DnsSyncService(_clientMock.Object, _mapper);
    }

    [Fact]
    public async Task Should_ReturnCreatedStatus_When_EnsureCreatedSucceeds()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _clientMock.Setup(x => x.EnsureRecordMatchesAsync(It.IsAny<DnsRecord>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RecordStatus.Created);

        var result = await _service.EnsureCreated(entity, CancellationToken.None);

        Assert.Equal(RecordStatus.Created, result.RecordStatus);
    }

    [Fact]
    public async Task Should_ReturnUpdatedStatus_When_EnsureCreatedUpdatesRecord()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _clientMock.Setup(x => x.EnsureRecordMatchesAsync(It.IsAny<DnsRecord>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RecordStatus.Updated);

        var result = await _service.EnsureCreated(entity, CancellationToken.None);

        Assert.Equal(RecordStatus.Updated, result.RecordStatus);
    }

    [Fact]
    public async Task Should_ReturnUnchangedStatus_When_EnsureCreatedFindsMatchingRecord()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _clientMock.Setup(x => x.EnsureRecordMatchesAsync(It.IsAny<DnsRecord>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RecordStatus.Unchanged);

        var result = await _service.EnsureCreated(entity, CancellationToken.None);

        Assert.Equal(RecordStatus.Unchanged, result.RecordStatus);
    }

    [Fact]
    public async Task Should_ReturnDeletedStatus_When_EnsureDeletedSucceeds()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _clientMock.Setup(x => x.EnsureRecordAbsentAsync(It.IsAny<DnsRecord>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RecordStatus.Deleted);

        var result = await _service.EnsureDeleted(entity, CancellationToken.None);

        Assert.Equal(RecordStatus.Deleted, result.RecordStatus);
    }

    [Fact]
    public async Task Should_ReturnAlreadyAbsentStatus_When_EnsureDeletedFindsNoRecord()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _clientMock.Setup(x => x.EnsureRecordAbsentAsync(It.IsAny<DnsRecord>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(RecordStatus.AlreadyAbsent);

        var result = await _service.EnsureDeleted(entity, CancellationToken.None);

        Assert.Equal(RecordStatus.AlreadyAbsent, result.RecordStatus);
    }

    [Fact]
    public async Task Should_PassCorrectDnsRecord_When_EnsureCreatedCalled()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        DnsRecord? capturedRecord = null;
        _clientMock.Setup(x => x.EnsureRecordMatchesAsync(It.IsAny<DnsRecord>(), false, It.IsAny<CancellationToken>()))
            .Callback<DnsRecord, bool, CancellationToken>((record, _, _) => capturedRecord = record)
            .ReturnsAsync(RecordStatus.Created);

        await _service.EnsureCreated(entity, CancellationToken.None);

        Assert.NotNull(capturedRecord);
        Assert.Equal(entity.Spec.RootName, capturedRecord.RootName);
        Assert.Equal(entity.Spec.Name, capturedRecord.Name);
        Assert.Equal(entity.Spec.Type, capturedRecord.Type);
        Assert.Equal(entity.Spec.Content, capturedRecord.Content);
    }

    [Fact]
    public async Task Should_ThrowException_When_ClientFails()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _clientMock.Setup(x => x.EnsureRecordMatchesAsync(It.IsAny<DnsRecord>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IonosDnsException("Test exception"));

        await Assert.ThrowsAsync<IonosDnsException>(() => _service.EnsureCreated(entity, CancellationToken.None));
    }
}
