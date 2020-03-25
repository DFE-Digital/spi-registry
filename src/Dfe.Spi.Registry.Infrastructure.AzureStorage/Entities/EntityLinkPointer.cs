using Dfe.Spi.Registry.Domain.Entities;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities
{
    internal class EntityLinkPointer : LinkPointer
    {
        public string EntityType { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
    }
}