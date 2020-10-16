using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain;
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
                var toUpsert = partition.Where(u => !u.ToDelete).Select(u => u.Entity).ToArray();
                var toDelete = partition.Where(u => u.ToDelete).Select(u => u.Entity.Id).ToArray();

                await _connection.MakeTransactionalUpdateAsync(
                    partition[0].Entity.PartitionableId,
                    toUpsert,
                    toDelete,
                    _logger,
                    cancellationToken);
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
            
            // It is possible that the above query will return multiple results if the pointInTime is a day that a change occured
            //    Example: pointInTime=2020-10-04, E1=2020-01-01 to 2020-10-04, E2=2020-10-04 to null
            // So we need to check for this
            if (results.Length == 2)
            {
                var orderedResults = results.OrderBy(x => x.ValidFrom).ToArray();
                var earliestRecord = orderedResults.First();
                var latestRecord = orderedResults.Last();

                // Check to see if this was a date of change
                if (earliestRecord.ValidTo == latestRecord.ValidFrom)
                {
                    // If so, just use the latest record
                    return latestRecord;
                }
                
                // This is not a data of change
                throw new Exception($"Query for {entityType}:{sourceSystemName}:{sourceSystemId} at {pointInTime} returned 2 results. " +
                                    $"Those 2 records do not represent a change date " +
                                    $"(earliest record valid {earliestRecord.ValidFrom} to {earliestRecord.ValidTo}, " +
                                    $"latest record valid {latestRecord.ValidFrom} to {latestRecord.ValidTo})." +
                                    "Something appears to be wrong with the stored data");
            }
            
            if (results.Length > 2)
            {
                throw new Exception($"Query for {entityType}:{sourceSystemName}:{sourceSystemId} at {pointInTime} returned {results.Length} results. " +
                                    "There should normally only be a single result, occasionally 2 on the date of a change. " +
                                    "Something appears to be wrong with the stored data");
            }

            return results.SingleOrDefault();
        }

        public async Task<RegisteredEntity[]> RetrieveBatchAsync(EntityPointer[] entityPointers, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var pointersQuery = _queryFactory(CosmosCombinationOperator.Or);
            foreach (var entityPointer in entityPointers)
            {
                var entityQuery = _queryFactory(CosmosCombinationOperator.And)
                    .AddTypeCondition(entityPointer.EntityType)
                    .AddSourceSystemIdCondition(entityPointer.SourceSystemName, entityPointer.SourceSystemId);
                pointersQuery.AddGroup(entityQuery);
            }

            var batchQuery = _queryFactory(CosmosCombinationOperator.And)
                .AddGroup(pointersQuery)
                .AddPointInTimeCondition(pointInTime);
            
            var results = await RunQuery(batchQuery, cancellationToken);

            return results.ToArray();
        }

        public async Task<SearchResult> SearchAsync(SearchRequest request, string entityType, DateTime pointInTime, CancellationToken cancellationToken)
        {
            // Build the query for use input
            var userQuery = new CosmosQuery(request.CombinationOperator.Equals("or") ? CosmosCombinationOperator.Or : CosmosCombinationOperator.And);
            foreach (var group in request.Groups)
            {
                var groupQuery = new CosmosQuery(group.CombinationOperator.Equals("or") ? CosmosCombinationOperator.Or : CosmosCombinationOperator.And);

                foreach (var filter in group.Filter)
                {
                    groupQuery.AddCondition(filter.Field, filter.Operator, filter.Value);
                }

                userQuery.AddGroup(groupQuery);
            }

            // Add system properties
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddGroup(userQuery)
                .AddTypeCondition(entityType)
                .AddPointInTimeCondition(pointInTime)
                .TakeResultsBetween(request.Skip, request.Take);

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

        public IDictionary<string, Type> GetSearchableFieldNames()
        {
            return CosmosQuery.GetSearchablePropertyNames();
        }

        private async Task<CosmosRegisteredEntity[]> RunQuery(CosmosQuery query, CancellationToken cancellationToken)
        {
            var queryDefinition = new QueryDefinition(query.ToString());
            return await _connection.RunQueryAsync<CosmosRegisteredEntity>(queryDefinition, _logger, cancellationToken);
        }

        private async Task<long> RunCountQuery(CosmosQuery query, CancellationToken cancellationToken)
        {
            var queryDefinition = new QueryDefinition(query.ToString(true));
            var results = await _connection.RunQueryAsync<JObject>(queryDefinition, _logger, cancellationToken);

            return (long) ((JValue) results[0]["$1"]).Value;
        }
    }
}