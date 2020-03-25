using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities
{
    public class LinkPointerEntity : TableEntity
    {
        public string Type { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
        public string LinkType { get; set; }
        public string LinkId { get; set; }
    }
}