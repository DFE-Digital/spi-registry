using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Sync;
using Dfe.Spi.Registry.Functions.Sync;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.IntegrationTests.Stubs
{
    public class SyncQueueStub : ISyncQueue
    {
        private readonly ServiceFactory<ProcessEntityEvent> _functionFactory;
        private readonly ConcurrentQueue<SyncQueueItem> _queue;

        public SyncQueueStub(ServiceFactory<ProcessEntityEvent> functionFactory)
        {
            _functionFactory = functionFactory;
            _queue = new ConcurrentQueue<SyncQueueItem>();
        }

        public Task EnqueueEntityForSyncAsync(SyncQueueItem queueItem, CancellationToken cancellationToken)
        {
            _queue.Enqueue(queueItem);
            return Task.CompletedTask;
        }

        public async Task DrainQueueAsync(CancellationToken cancellationToken)
        {
            var function = _functionFactory.Create();
            while (!_queue.IsEmpty)
            {
                SyncQueueItem queueItem;
                if (!_queue.TryDequeue(out queueItem))
                {
                    continue;
                }

                try
                {
                    var queueItemJson = JsonConvert.SerializeObject(queueItem);
                    var message = new CloudQueueMessage(queueItemJson);
                    await function.RunAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error processing queue item for {queueItem.Entity} as {queueItem.PointInTime}: {ex.Message}", ex);
                }
            }
        }
    }
}