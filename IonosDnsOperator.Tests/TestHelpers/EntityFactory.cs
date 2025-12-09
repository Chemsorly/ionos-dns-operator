using IonosDns;
using IonosDnsOperator.Entities;
using k8s.Models;

namespace IonosDnsOperator.Tests.TestHelpers;

public static class EntityFactory
{
    public static IonosDnsRecord CreateDefaultEntity()
    {
        return new IonosDnsRecord
        {
            Metadata = new V1ObjectMeta
            {
                Name = "test-record",
                NamespaceProperty = "default"
            },
            Spec = new IonosDnsRecord.EntitySpec
            {
                RootName = "example.com",
                Name = "test",
                Type = RecordType.A,
                Content = "192.168.1.1",
                Ttl = 3600,
                Disabled = false
            },
            Status = new IonosDnsRecord.EntityStatus()
        };
    }

    public static IonosDnsRecord CreateEntityWithAllFields()
    {
        return new IonosDnsRecord
        {
            Metadata = new V1ObjectMeta
            {
                Name = "full-record",
                NamespaceProperty = "default"
            },
            Spec = new IonosDnsRecord.EntitySpec
            {
                RootName = "example.com",
                Name = "full",
                Type = RecordType.MX,
                Content = "mail.example.com",
                Ttl = 7200,
                Prio = 10,
                Disabled = false
            },
            Status = new IonosDnsRecord.EntityStatus()
        };
    }

    public static IonosDnsRecord CreateMinimalEntity()
    {
        return new IonosDnsRecord
        {
            Metadata = new V1ObjectMeta
            {
                Name = "minimal-record",
                NamespaceProperty = "default"
            },
            Spec = new IonosDnsRecord.EntitySpec
            {
                RootName = "example.com",
                Name = "minimal",
                Type = RecordType.A,
                Content = "10.0.0.1"
            },
            Status = new IonosDnsRecord.EntityStatus()
        };
    }

    public static DnsRecord CreateDefaultDnsRecord()
    {
        return new DnsRecord
        {
            RootName = "example.com",
            Name = "test",
            Type = RecordType.A,
            Content = "192.168.1.1",
            Ttl = 3600,
            Disabled = false
        };
    }
}
