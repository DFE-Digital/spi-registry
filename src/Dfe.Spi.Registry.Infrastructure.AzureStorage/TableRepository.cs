using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.OData.UriParser;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage
{
    public abstract class TableRepository<TEntity, TModel> 
        where TEntity : TableEntity, new()
        where TModel : class
    {
        protected TableRepository(string connectionString, string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            Table = tableClient.GetTableReference(tableName);
        }

        protected CloudTable Table { get; private set; }

        protected async Task<TModel> GetSingleEntityAsync(string partitionKey, string rowKey,
            CancellationToken cancellationToken)
        {
            await Table.CreateIfNotExistsAsync(cancellationToken);
            
            var operation = TableOperation.Retrieve<TEntity>(partitionKey, rowKey);
            var operationResult = await Table.ExecuteAsync(operation, cancellationToken);
            if (operationResult.Result == null)
            {
                return null;
            }

            return ConvertEntityToModel((TEntity) operationResult.Result);
        }
        protected async Task<TModel[]> GetEntitiesInPartition(string partitionKey, CancellationToken cancellationToken)
        {
            await Table.CreateIfNotExistsAsync(cancellationToken);

            var query = new TableQuery<TEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            var queryResult = Table.ExecuteQuery(query);
            var models = queryResult.Select(ConvertEntityToModel).ToArray();
            return models;
        }

        protected async Task InsertOrReplaceAsync(TModel model, CancellationToken cancellationToken)
        {
            await Table.CreateIfNotExistsAsync(cancellationToken);
            
            var entity = ConvertModelToEntity(model);
            if (string.IsNullOrEmpty(entity.ETag))
            {
                entity.ETag = "*";
            }

            var operation = TableOperation.InsertOrReplace(entity);
            await Table.ExecuteAsync(operation, cancellationToken);
        }

        protected async Task InsertOrReplaceBatchAsync(TModel[] models, CancellationToken cancellationToken)
        {
            await Table.CreateIfNotExistsAsync(cancellationToken);
            
            var batch = new TableBatchOperation();

            foreach (var model in models)
            {
                var entity = ConvertModelToEntity(model);
                if (string.IsNullOrEmpty(entity.ETag))
                {
                    entity.ETag = "*";
                }
                batch.InsertOrReplace(entity);
            }
            
            await Table.ExecuteBatchAsync(batch, cancellationToken);
        }

        protected abstract TModel ConvertEntityToModel(TEntity entity);
        protected abstract TEntity ConvertModelToEntity(TModel model);
    }
}