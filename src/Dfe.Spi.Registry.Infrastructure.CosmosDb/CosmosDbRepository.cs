using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Data;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    public class CosmosDbRepository : IRepository
    {
        private readonly ILoggerWrapper _logger;
        private readonly Container _container;

        public CosmosDbRepository(DataConfiguration configuration, ILoggerWrapper logger)
        {
            _logger = logger;
            
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
                .AddPointInTimeConditions(pointInTime);
            var results = await RunQuery(query, cancellationToken);
            return results.SingleOrDefault();
        }

        public async Task<SearchResult> SearchAsync(SearchRequest request, string entityType, DateTime pointInTime, CancellationToken cancellationToken)
        {
            // Build the query
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

            // Take a copy for the count
            var countQuery = query.Clone();

            // Add the skip and take
            query.TakeResultsBetween(request.Skip, request.Take);
            
            // Get the results
            var results = await RunQuery(query, cancellationToken);
            var count = await RunCountQuery(countQuery, cancellationToken);
            
            return new SearchResult
            {
                Results = results,
                TotalNumberOfRecords = count,
            };
        }


        private async Task<RegisteredEntity[]> RunQuery(CosmosQuery query, CancellationToken cancellationToken)
        {
            var queryDefinition = new QueryDefinition(query.ToString());
            return await RunQuery<RegisteredEntity>(queryDefinition, cancellationToken);
        }

        private async Task<long> RunCountQuery(CosmosQuery query, CancellationToken cancellationToken)
        {
            var queryDefinition = new QueryDefinition(query.ToString(true));
            var results = await RunQuery<JObject>(queryDefinition, cancellationToken);

            return (long) ((JValue) results[0]["$1"]).Value;
        }
        private async Task<T[]> RunQuery<T>(QueryDefinition query, CancellationToken cancellationToken)
        {
            _logger.Debug($"Running CosmosDB query: {query.QueryText}");
            var iterator = _container.GetItemQueryIterator<T>(query);
            var results = new List<T>();

            while (iterator.HasMoreResults)
            {
                var batch = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(batch);
            }

            return results.ToArray();
        }
    }
}