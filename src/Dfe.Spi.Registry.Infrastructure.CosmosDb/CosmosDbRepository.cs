using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Models;
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

        public async Task StoreAsync(RegisteredEntity[] registeredEntitiesToUpsert, RegisteredEntity[] registeredEntitiesToDelete, CancellationToken cancellationToken)
        {
            var joinedEntities =
                registeredEntitiesToUpsert.Select(x => new {ToDelete = false, Entity = x})
                    .Concat(registeredEntitiesToDelete.Select(x => new {ToDelete = true, Entity = x}));
            var partitionedEntities = joinedEntities
                .GroupBy(e => e.Entity.Type)
                .Select(g => g.ToArray())
                .ToArray();
            foreach (var partition in partitionedEntities)
            {
                var batch = _container.CreateTransactionalBatch(new PartitionKey(partition[0].Entity.Type));
                foreach (var update in partition)
                {
                    if (update.ToDelete)
                    {
                        batch = batch.DeleteItem(update.Entity.Id);
                    }
                    else
                    {
                        batch = batch.UpsertItem(update.Entity);
                    }
                }

                using (var batchResponse = await batch.ExecuteAsync(cancellationToken))
                {
                    if (!batchResponse.IsSuccessStatusCode)
                    {
                        throw new Exception($"Failed to store batch of entities in {partition[0].Entity.Type} (Response code: {batchResponse.StatusCode}): {batchResponse.ErrorMessage}");
                    }
                }
            }
        }

        public async Task<RegisteredEntity> RetrieveAsync(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddRegisteredEntityCondition("type", DataOperator.Equals, entityType)
                .AddEntityCondition("sourceSystemName", DataOperator.Equals, sourceSystemName)
                .AddEntityCondition("sourceSystemId", DataOperator.Equals, sourceSystemId)
                .AddPointInTimeConditions(pointInTime)
                .ToString();
            var queryDefinition = new QueryDefinition(query);
            var results = await RunQuery(queryDefinition, cancellationToken);
            return results.SingleOrDefault();
        }

        public async Task<RegisteredEntity[]> SearchAsync(SearchRequest request, string entityType, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var query = new CosmosQuery(request.CombinationOperator.Equals("or") ? CosmosCombinationOperator.Or : CosmosCombinationOperator.And)
                .AddRegisteredEntityCondition("type", DataOperator.Equals, entityType)
                .AddPointInTimeConditions(pointInTime);
            foreach (var group in request.Groups)
            {
                var groupQuery = new CosmosQuery(group.CombinationOperator.Equals("or") ? CosmosCombinationOperator.Or : CosmosCombinationOperator.And);
                
                foreach (var filter in group.Filter)
                {
                    groupQuery.AddEntityCondition(filter.Field, filter.Operator, filter.Value);
                }

                query.AddGroup(groupQuery);
            }
            
            var queryDefinition = new QueryDefinition(query.ToString());
            var results = await RunQuery(queryDefinition, cancellationToken);
            return results;
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