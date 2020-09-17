using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Data;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    public class CosmosDbRepository : IRepository
    {
        private readonly CosmosDbConnection _connection;
        private readonly IMapper _mapper;
        private readonly Func<CosmosCombinationOperator, CosmosQuery> _queryFactory;
        private readonly ILoggerWrapper _logger;

        internal CosmosDbRepository(
            CosmosDbConnection connection, 
            IMapper mapper, 
            Func<CosmosCombinationOperator, CosmosQuery> queryFactory, 
            ILoggerWrapper logger)
        {
            _connection = connection;
            _mapper = mapper;
            _queryFactory = queryFactory;
            _logger = logger;
        }

        public CosmosDbRepository(CosmosDbConnection connection, ILoggerWrapper logger)
            : this(connection, new Mapper(), (@operator) => new CosmosQuery(@operator), logger)
        {
        }


        public async Task StoreAsync(RegisteredEntity registeredEntity, CancellationToken cancellationToken)
        {
            var cosmosEntity = _mapper.Map(registeredEntity);
            await _connection.Container.UpsertItemAsync(cosmosEntity, new PartitionKey(cosmosEntity.PartitionableId), null, cancellationToken);
        }

        public async Task StoreAsync(
            RegisteredEntity[] registeredEntitiesToUpsert,
            RegisteredEntity[] registeredEntitiesToDelete,
            CancellationToken cancellationToken)
        {
            var joinedEntities = registeredEntitiesToUpsert
                .Select(x => new {ToDelete = false, Entity = _mapper.Map(x)})
                .Concat(registeredEntitiesToDelete.Select(x => new {ToDelete = true, Entity = _mapper.Map(x)}));
            var partitionedEntities = joinedEntities
                .GroupBy(e => e.Entity.PartitionableId)
                .Select(g => g.ToArray())
                .ToArray();
            foreach (var partition in partitionedEntities)
            {
                var batch = _connection.Container.CreateTransactionalBatch(new PartitionKey(partition[0].Entity.PartitionableId));
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
                        throw new Exception(
                            $"Failed to store batch of entities in {partition[0].Entity.Type} (Response code: {batchResponse.StatusCode}): {batchResponse.ErrorMessage}");
                    }
                }
            }
        }

        public async Task<RegisteredEntity> RetrieveAsync(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime,
            CancellationToken cancellationToken)
        {
            var query = _queryFactory(CosmosCombinationOperator.And)
                .AddTypeCondition(entityType)
                .AddSourceSystemIdCondition(sourceSystemName, sourceSystemId)
                .AddPointInTimeCondition(pointInTime);
            
            var results = await RunQuery(query, cancellationToken);

            return results.SingleOrDefault();
        }

        public async Task<SearchResult> SearchAsync(SearchRequest request, string entityType, DateTime pointInTime, CancellationToken cancellationToken)
        {
            // Build the query
            var query = new CosmosQuery(request.CombinationOperator.Equals("or") ? CosmosCombinationOperator.Or : CosmosCombinationOperator.And)
                .AddTypeCondition(entityType)
                .AddPointInTimeCondition(pointInTime);
            foreach (var group in request.Groups)
            {
                var groupQuery = new CosmosQuery(group.CombinationOperator.Equals("or") ? CosmosCombinationOperator.Or : CosmosCombinationOperator.And);

                foreach (var filter in group.Filter)
                {
                    groupQuery.AddCondition(filter.Field, filter.Operator, filter.Value);
                }

                query.AddGroup(groupQuery);
            }

            // Add the skip and take
            query.TakeResultsBetween(request.Skip, request.Take);

            // Get the results
            var results = await RunQuery(query, cancellationToken);
            var count = await RunCountQuery(query, cancellationToken);

            var mappedResults = results
                .Select(x => _mapper.Map(x))
                .ToArray();
            
            return new SearchResult
            {
                Results = mappedResults,
                TotalNumberOfRecords = count,
            };
        }
        
        private async Task<CosmosRegisteredEntity[]> RunQuery(CosmosQuery query, CancellationToken cancellationToken)
        {
            var queryDefinition = new QueryDefinition(query.ToString());
            return await RunQuery<CosmosRegisteredEntity>(queryDefinition, cancellationToken);
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
            var iterator = _connection.Container.GetItemQueryIterator<T>(query);
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