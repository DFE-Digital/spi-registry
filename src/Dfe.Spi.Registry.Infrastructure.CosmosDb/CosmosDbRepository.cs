using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task StoreAsync(RegisteredEntity[] registeredEntities, CancellationToken cancellationToken)
        {
            var partitionedEntities = registeredEntities
                .GroupBy(e => e.Type)
                .Select(g => g.ToArray())
                .ToArray();
            foreach (var partition in partitionedEntities)
            {
                var batch = _container.CreateTransactionalBatch(new PartitionKey(partition[0].Type));
                foreach (var entity in partition)
                {
                    batch = batch.UpsertItem(entity);
                }

                using (var batchResponse = await batch.ExecuteAsync(cancellationToken))
                {
                    if (!batchResponse.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to store batch of entities in {partition[0].Type} (Response code: {batchResponse.StatusCode}): {batchResponse.ErrorMessage}");
                    }
                }
            }
        }

        public async Task<RegisteredEntity> RetrieveAsync(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE EXISTS (" +
                "SELECT VALUE n FROM n IN c.entities " +
                $"WHERE UPPER(c.type)='{entityType.ToUpper()}' AND UPPER(n.sourceSystemName)='{sourceSystemName.ToUpper()}' AND UPPER(n.sourceSystemId)='{sourceSystemId.ToUpper()}' " +
                $"AND c.validFrom <= '{pointInTime.ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z' AND (IS_NULL(c.validTo) OR c.validTo > '{pointInTime.ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z'))");
            var results = await RunQuery(query, cancellationToken);
            return results.SingleOrDefault();
        }
        
        
        

        private async Task<RegisteredEntity[]> RunQuery(QueryDefinition query, CancellationToken cancellationToken)
        {
            var iterator = _container.GetItemQueryIterator<RegisteredEntity>(query);
            var results = new List<RegisteredEntity>();

            while (iterator.HasMoreResults)
            {
                var batch = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(batch);
            }

            return results.ToArray();
        }
    }
}