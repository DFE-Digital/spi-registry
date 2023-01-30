using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Configuration;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    /// <summary>
    /// Should be used as singleton - https://docs.microsoft.com/en-us/azure/azure-functions/manage-connections
    /// </summary>
    public class CosmosDbConnection
    {
        private const int MaxActionAttempts = 3;

        private DateTime _retryAfter;

        internal CosmosDbConnection(Container container)
        {
            _retryAfter = DateTime.MinValue;
            Container = container;
        }

        public CosmosDbConnection(DataConfiguration configuration)
            : this(GetContainerFromConfig(configuration))
        {
        }

        public Container Container { get; }

        public async Task<T[]> RunQueryAsync<T>(QueryDefinition query, ILoggerWrapper logger, CancellationToken cancellationToken)
        {
            return await ExecuteContainerActionAsync(async () =>
            {
                logger.Debug($"Running CosmosDB query: {query.QueryText}");
                var iterator = Container.GetItemQueryIterator<T>(query);
                var results = new List<T>();

                while (iterator.HasMoreResults)
                {
                    var batch = await iterator.ReadNextAsync(cancellationToken);
                    results.AddRange(batch);
                }

                return results.ToArray();
            }, logger, cancellationToken);
        }

        public async Task MakeTransactionalUpdateAsync(
            string partitionKey,
            CosmosRegisteredEntity[] entitiesToUpsert,
            string[] idsToDelete,
            ILoggerWrapper logger,
            CancellationToken cancellationToken)
        {
            await ExecuteContainerActionAsync(async () =>
            {
                logger.Debug($"Running batch update operation...");

                var batch = Container.CreateTransactionalBatch(new PartitionKey(partitionKey));
                foreach (var entity in entitiesToUpsert)
                {
                    TransactionalBatchItemRequestOptions options = null;
                    if (!string.IsNullOrEmpty(entity.ETag))
                    {
                        options = new TransactionalBatchItemRequestOptions
                        {
                            IfMatchEtag = entity.ETag,
                        };
                    }

                    entity.ETag = null;
                    batch.UpsertItem(entity, options);
                }

                using var batchResponse = await batch.ExecuteAsync(cancellationToken);
                if (!batchResponse.IsSuccessStatusCode)
                {

                    if (batchResponse.TryGetNonEnumeratedCount(out var count))
                    {
                        for (var i = 0; i < count; i++)
                        {
                            var itemResponse = batchResponse.GetOperationResultAtIndex<CosmosRegisteredEntity>(i);
                            logger.Debug($"Batch operation result #{i} | StatusCode: {itemResponse.StatusCode}, Resource Id: {itemResponse.Resource?.Id}");
                        }
                    }

                    if (batchResponse.StatusCode == HttpStatusCode.PreconditionFailed)
                    {
                        throw new Exception($"Failed to store batch of entities in {partitionKey} as one or more of the updated entities has been " +
                                            $"modified since the last time it was read. This is likely due to a duplicate sync event being received.");
                    }

                    throw new Exception($"Failed to store batch of entities in {partitionKey} " +
                                        $"(Response code: {batchResponse.StatusCode}): {batchResponse.ErrorMessage}");
                }

                // separated update and delete operation to make sure updated entities are reflected in the to be deleted items! Run only if the Update operation is successful!
                if (batchResponse.IsSuccessStatusCode)
                {
                    var itemsToDelete = false;
                    logger.Debug($"Running batch delete operation...");

                    var deleteBatch = Container.CreateTransactionalBatch(new PartitionKey(partitionKey));

                    foreach (var id in idsToDelete)
                    {
                        if (!await ItemExists(id, new PartitionKey(partitionKey), logger, cancellationToken)) continue;
                        deleteBatch.DeleteItem(id);
                        itemsToDelete = true;
                    }

                    if (itemsToDelete)
                    {
                        using var deleteBatchResponse = await deleteBatch.ExecuteAsync(cancellationToken);
                        if (!deleteBatchResponse.IsSuccessStatusCode)
                        {

                            if (deleteBatchResponse.TryGetNonEnumeratedCount(out var count))
                            {
                                for (var i = 0; i < count; i++)
                                {
                                    var itemResponse =
                                        deleteBatchResponse.GetOperationResultAtIndex<CosmosRegisteredEntity>(i);
                                    logger.Debug(
                                        $"Delete Batch operation result #{i} | StatusCode: {itemResponse.StatusCode}, Resource Id: {itemResponse.Resource?.Id}");
                                }
                            }
                            if (deleteBatchResponse.StatusCode != HttpStatusCode.NotFound)
                                throw new Exception($"Failed to delete batch of entities in {partitionKey} " +
                                                $"(Response code: {deleteBatchResponse.StatusCode}): {deleteBatchResponse.ErrorMessage}");
                        }
                    }
                }


            }, logger, cancellationToken);
        }

        private static Container GetContainerFromConfig(DataConfiguration configuration)
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
            return client.GetDatabase(configuration.DatabaseName).GetContainer(configuration.ContainerName);
        }

        private async Task ExecuteContainerActionAsync(Func<Task> asyncAction, ILoggerWrapper logger, CancellationToken cancellationToken)
        {
            await ExecuteContainerActionAsync<object>(async () =>
            {
                await asyncAction();
                return null;
            }, logger, cancellationToken);
        }
        private async Task<T> ExecuteContainerActionAsync<T>(Func<Task<T>> asyncAction, ILoggerWrapper logger, CancellationToken cancellationToken)
        {
            var attempt = 0;
            while (true)
            {
                var waitFor = _retryAfter - DateTime.Now;
                if (waitFor.TotalMilliseconds > 0)
                {
                    logger.Debug($"Joining queue; waiting for {waitFor.TotalSeconds:0}s");
                    await Task.Delay(waitFor, cancellationToken);
                }

                try
                {
                    return await asyncAction();
                }
                catch (CosmosException ex)
                {
                    if (attempt >= MaxActionAttempts - 1 || (int)ex.StatusCode != 429)
                    {
                        throw;
                    }

                    lock (this)
                    {
                        _retryAfter = DateTime.Now.Add(ex.RetryAfter ?? TimeSpan.FromSeconds(1));
                        logger.Debug($"Received 429 on attempt {attempt}. Will retry after {_retryAfter}");
                    }
                }
                attempt++;
            }
        }

        private async Task<bool> ItemExists(string id, PartitionKey partitionKey, ILoggerWrapper logger, CancellationToken cancellationToken)
        {
            try
            {
                logger.Debug($"ItemExists() > checking if item {id} exists...");

                var result =
                    await Container.ReadItemAsync<CosmosRegisteredEntity>(id, partitionKey,
                        cancellationToken: cancellationToken);
                logger.Debug($"ItemExists() > result status code: {result.StatusCode}");
                return result.StatusCode == HttpStatusCode.OK;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.Debug($"Item {id} not found!, exception: {ex}");
            }
            catch (CosmosException ex)
            {
                logger.Debug($"Filed to check if item {id} exists, exception: {ex}");
            }
            return false;
        }

    }

}