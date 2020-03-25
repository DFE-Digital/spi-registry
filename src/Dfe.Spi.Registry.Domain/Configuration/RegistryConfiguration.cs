namespace Dfe.Spi.Registry.Domain.Configuration
{
    public class RegistryConfiguration
    {
        public EntitiesConfiguration Entities { get; set; }
        public LinksConfiguration Links { get; set; }
        public QueueConfiguration Queue { get; set; }
        public SearchConfiguration Search { get; set; }
    }
}