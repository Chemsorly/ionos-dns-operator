using IonosDns;
using IonosDnsOperator.Entities;
using IonosDnsOperator.Finalizers;
using IonosDnsOperator.Services;
using IonosDnsOperator.Tests.TestHelpers;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;

namespace IonosDnsOperator.Tests.Finalizers;

public class IonosDnsRecordFinalizerTests
{
    private readonly Mock<ILogger<IonosDnsRecordFinalizer>> _loggerMock = new();
    private readonly Mock<IDnsSyncService> _dnsSyncServiceMock = new();
    private readonly Mock<IKubernetesClient> _kubernetesClientMock = new();
    private readonly IonosDnsRecordFinalizer _finalizer;

    public IonosDnsRecordFinalizerTests()
    {
        _finalizer = new IonosDnsRecordFinalizer(_loggerMock.Object, _dnsSyncServiceMock.Object, _kubernetesClientMock.Object);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RecordIsDeleted()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureDeleted(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.Deleted });

        var result = await _finalizer.FinalizeAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.RequeueAfter);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RecordIsAlreadyAbsent()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureDeleted(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.AlreadyAbsent });

        var result = await _finalizer.FinalizeAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.RequeueAfter);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_DeletionFails()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureDeleted(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.NotFound });

        var result = await _finalizer.FinalizeAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_DnsSyncServiceThrows()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureDeleted(entity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        await Assert.ThrowsAsync<Exception>(() => _finalizer.FinalizeAsync(entity, CancellationToken.None));
    }
}
