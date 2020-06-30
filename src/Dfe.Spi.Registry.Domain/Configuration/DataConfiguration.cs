namespace Dfe.Spi.Registry.Domain.Configuration
{
    public class DataConfiguration
    {
        public string CosmosDbUri { get; set; }
        public string CosmosDbKey { get; set; }
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
    }
}