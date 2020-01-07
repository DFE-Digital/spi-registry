namespace Dfe.Spi.Registry.Domain.Configuration
{
    public class RegistryConfiguration
    {
        public GiasAdapterConfiguration GiasAdapter { get; set; }
    }

    public class GiasAdapterConfiguration
    {
        public string CacheConnectionString { get; set; }
    }
}