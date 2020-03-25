using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities
{
    public class EntityEntity : TableEntity
    {
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
    }
}