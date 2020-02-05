using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage
{
    public abstract class TableRepository<TEntity, TModel> 
        where TEntity : TableEntity
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

        protected abstract TModel ConvertEntityToModel(TEntity entity);
    }
}