using IonosDns;
using IonosDnsOperator.Controllers;
using IonosDnsOperator.Entities;
using IonosDnsOperator.Services;
using IonosDnsOperator.Tests.TestHelpers;
using KubeOps.Abstractions.Events;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;

namespace IonosDnsOperator.Tests.Controllers;

public class IonosDnsRecordControllerTests
{
    private readonly Mock<ILogger<IonosDnsRecordController>> _loggerMock = new();
    private readonly Mock<IDnsSyncService> _dnsSyncServiceMock = new();
    private readonly Mock<IKubernetesClient> _kubernetesClientMock = new();
    private readonly Mock<EventPublisher> _eventPublisherMock = new();
    private readonly IonosDnsRecordController _controller;

    public IonosDnsRecordControllerTests()
    {
        _controller = new IonosDnsRecordController(
            _loggerMock.Object,
            _dnsSyncServiceMock.Object,
            _kubernetesClientMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RecordIsCreated()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureCreated(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.Created });

        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.RequeueAfter);
        Assert.Equal(RecordStatus.Created, entity.Status.DnsRecordStatus);
        Assert.True(entity.Status.LastChanged > DateTimeOffset.MinValue);
        _kubernetesClientMock.Verify(x => x.UpdateStatusAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x(entity, "Reconciled", It.IsAny<string>(), EventType.Normal, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RecordIsUpdated()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureCreated(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.Updated });

        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.RequeueAfter);
        Assert.Equal(RecordStatus.Updated, entity.Status.DnsRecordStatus);
        Assert.True(entity.Status.LastChanged > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_RecordIsUnchanged()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        var initialLastChanged = entity.Status.LastChanged;
        _dnsSyncServiceMock.Setup(x => x.EnsureCreated(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.Unchanged });

        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.RequeueAfter);
        Assert.Equal(RecordStatus.Unchanged, entity.Status.DnsRecordStatus);
        Assert.Equal(initialLastChanged, entity.Status.LastChanged);
        Assert.True(entity.Status.LastReconciled > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Should_ReturnFailure_When_DnsSyncServiceThrows()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureCreated(entity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        await Assert.ThrowsAsync<Exception>(() => _controller.ReconcileAsync(entity, CancellationToken.None));
    }

    [Fact]
    public async Task Should_UpdateLastReconciled_When_ReconciliationCompletes()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        var beforeReconcile = DateTimeOffset.UtcNow;
        _dnsSyncServiceMock.Setup(x => x.EnsureCreated(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.Unchanged });

        await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.True(entity.Status.LastReconciled >= beforeReconcile);
        Assert.True(entity.Status.LastReconciled <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Should_HandleStatusUpdateFailure_When_KubernetesClientFails()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureCreated(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.Created });
        _kubernetesClientMock.Setup(x => x.UpdateStatusAsync(entity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Kubernetes API error"));

        await Assert.ThrowsAsync<Exception>(() => _controller.ReconcileAsync(entity, CancellationToken.None));
    }

    [Fact]
    public async Task Should_ReturnFailure_When_RecordStatusIsUnexpected()
    {
        var entity = EntityFactory.CreateDefaultEntity();
        _dnsSyncServiceMock.Setup(x => x.EnsureCreated(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DnsSyncResult { RecordStatus = RecordStatus.NotFound });

        var result = await _controller.ReconcileAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
        _eventPublisherMock.Verify(x => x(entity, "Reconciled", It.IsAny<string>(), EventType.Warning, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_ReturnSuccess_When_DeletedEventFired()
    {
        var entity = EntityFactory.CreateDefaultEntity();

        var result = await _controller.DeletedAsync(entity, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result.RequeueAfter);
    }
}
