namespace Dfe.Spi.Registry.Domain.Configuration
{
    public class RegistryConfiguration
    {
        public EntitiesConfiguration Entities { get; set; }
        public LinksConfiguration Links { get; set; }
        public QueueConfiguration Queue { get; set; }
    }

    public class EntitiesConfiguration
    {
        public string TableStorageConnectionString { get; set; }
        public string TableStorageTableName { get; set; }
    }

    public class LinksConfiguration
    {
        public string TableStorageConnectionString { get; set; }
        public string TableStorageTableName { get; set; }
    }

    public class QueueConfiguration
    {
        public string StorageQueueConnectionString { get; set; }
    }
}