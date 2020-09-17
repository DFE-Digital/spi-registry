using Dfe.Spi.Registry.Domain.Configuration;
using Microsoft.Azure.Cosmos;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    /// <summary>
    /// Should be used as singleton - https://docs.microsoft.com/en-us/azure/azure-functions/manage-connections
    /// </summary>
    public class CosmosDbConnection
    {
        internal CosmosDbConnection(Container container)
        {
            Container = container;
        }
        public CosmosDbConnection(DataConfiguration configuration)
        {
            var client = new CosmosClient(
                configuration.CosmosDbUri,
                configuration.CosmosDbKey,
                new CosmosClientOptions
                {
                    SerializerOptions = new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    },
                });
            Container = client.GetDatabase(configuration.DatabaseName).GetContainer(configuration.ContainerName);
        }
        
        public Container Container { get; }
    }
}