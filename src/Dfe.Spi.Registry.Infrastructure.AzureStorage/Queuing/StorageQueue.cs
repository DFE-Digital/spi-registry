using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Queuing
{
    public abstract class StorageQueue
    {
        public StorageQueue(string connectionString, string queueName)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            Queue = queueClient.GetQueueReference(queueName);
        }

        protected CloudQueue Queue { get; private set; }

        protected virtual async Task EnqueueItemAsync(object item, CancellationToken cancellationToken)
        {
            await Queue.CreateIfNotExistsAsync(cancellationToken);
                
            var message = new CloudQueueMessage(SerializeItem(item));
            await Queue.AddMessageAsync(message, cancellationToken);
        }

        protected virtual string SerializeItem(object item)
        {
            return JsonConvert.SerializeObject(item);
        }
    }
}