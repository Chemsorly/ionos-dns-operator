using IonosDns;
using IonosDnsOperator.Entities;
using IonosDnsOperator.Tests.TestHelpers;
using IonosDnsOperator.Webhooks;

namespace IonosDnsOperator.Tests.Webhooks;

public class IonosDnsRecordValidationWebhookTests
{
    private readonly IonosDnsRecordValidationWebhook _webhook = new();

    [Fact]
    public void Should_ReturnSuccess_When_CreateCalled()
    {
        var entity = EntityFactory.CreateDefaultEntity();

        var result = _webhook.Create(entity, false);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Should_ReturnFailure_When_RootNameChanges()
    {
        var oldEntity = EntityFactory.CreateDefaultEntity();
        var newEntity = EntityFactory.CreateDefaultEntity();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = "different.com",
            Name = oldEntity.Spec.Name,
            Type = oldEntity.Spec.Type,
            Content = oldEntity.Spec.Content
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.False(result.Valid);
    }

    [Fact]
    public void Should_ReturnFailure_When_NameChanges()
    {
        var oldEntity = EntityFactory.CreateDefaultEntity();
        var newEntity = EntityFactory.CreateDefaultEntity();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = oldEntity.Spec.RootName,
            Name = "different",
            Type = oldEntity.Spec.Type,
            Content = oldEntity.Spec.Content
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.False(result.Valid);
    }

    [Fact]
    public void Should_ReturnFailure_When_TypeChanges()
    {
        var oldEntity = EntityFactory.CreateDefaultEntity();
        var newEntity = EntityFactory.CreateDefaultEntity();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = oldEntity.Spec.RootName,
            Name = oldEntity.Spec.Name,
            Type = RecordType.AAAA,
            Content = oldEntity.Spec.Content
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.False(result.Valid);
    }

    [Fact]
    public void Should_ReturnSuccess_When_ContentChanges()
    {
        var oldEntity = EntityFactory.CreateDefaultEntity();
        var newEntity = EntityFactory.CreateDefaultEntity();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = oldEntity.Spec.RootName,
            Name = oldEntity.Spec.Name,
            Type = oldEntity.Spec.Type,
            Content = "192.168.1.2"
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Should_ReturnSuccess_When_TtlChanges()
    {
        var oldEntity = EntityFactory.CreateDefaultEntity();
        var newEntity = EntityFactory.CreateDefaultEntity();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = oldEntity.Spec.RootName,
            Name = oldEntity.Spec.Name,
            Type = oldEntity.Spec.Type,
            Content = oldEntity.Spec.Content,
            Ttl = 7200
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Should_ReturnSuccess_When_PrioChanges()
    {
        var oldEntity = EntityFactory.CreateEntityWithAllFields();
        var newEntity = EntityFactory.CreateEntityWithAllFields();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = oldEntity.Spec.RootName,
            Name = oldEntity.Spec.Name,
            Type = oldEntity.Spec.Type,
            Content = oldEntity.Spec.Content,
            Ttl = oldEntity.Spec.Ttl,
            Prio = 20
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Should_ReturnSuccess_When_DisabledChanges()
    {
        var oldEntity = EntityFactory.CreateDefaultEntity();
        var newEntity = EntityFactory.CreateDefaultEntity();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = oldEntity.Spec.RootName,
            Name = oldEntity.Spec.Name,
            Type = oldEntity.Spec.Type,
            Content = oldEntity.Spec.Content,
            Disabled = true
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Should_ReturnSuccess_When_DeleteCalled()
    {
        var entity = EntityFactory.CreateDefaultEntity();

        var result = _webhook.Delete(entity, false);

        Assert.True(result.Valid);
    }

    [Fact]
    public void Should_HandleNullValues_When_OptionalFieldsAreNull()
    {
        var oldEntity = EntityFactory.CreateMinimalEntity();
        var newEntity = EntityFactory.CreateMinimalEntity();
        newEntity.Spec = new IonosDnsRecord.EntitySpec
        {
            RootName = oldEntity.Spec.RootName,
            Name = oldEntity.Spec.Name,
            Type = oldEntity.Spec.Type,
            Content = "10.0.0.2"
        };

        var result = _webhook.Update(oldEntity, newEntity, false);

        Assert.True(result.Valid);
    }
}
