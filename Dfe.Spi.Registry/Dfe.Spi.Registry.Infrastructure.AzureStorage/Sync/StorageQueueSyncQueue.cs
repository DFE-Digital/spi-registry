using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Sync;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Sync
{
    public class StorageQueueSyncQueue : ISyncQueue
    {
        private CloudQueue _queue;

        public StorageQueueSyncQueue(SyncConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration.QueueConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(QueueNames.SyncQueue);
        }
        
        public async Task EnqueueEntityForSyncAsync(SyncQueueItem queueItem, CancellationToken cancellationToken)
        {
            await _queue.CreateIfNotExistsAsync(cancellationToken);
                
            var message = new CloudQueueMessage(JsonConvert.SerializeObject(queueItem));
            await _queue.AddMessageAsync(message, cancellationToken);
        }
    }
}