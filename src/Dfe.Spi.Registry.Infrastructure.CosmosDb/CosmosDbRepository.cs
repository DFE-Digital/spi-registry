using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Data;
using Microsoft.Azure.Cosmos;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    public class CosmosDbRepository : IRepository
    {
        private readonly Container _container;

        public CosmosDbRepository(DataConfiguration configuration)
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
            _container = client.GetDatabase(configuration.DatabaseName).GetContainer(configuration.ContainerName);
        }
        
        public async Task StoreAsync(RegisteredEntity registeredEntity, CancellationToken cancellationToken)
        {
            await _container.UpsertItemAsync(registeredEntity, cancellationToken: cancellationToken);
        }
    }
}