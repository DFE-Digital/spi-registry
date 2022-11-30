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
        private readonly RepositoryStub _repositoryStub;
        private readonly ConcurrentQueue<SyncQueueItem> _queue;

        public SyncQueueStub(ServiceFactory<ProcessEntityEvent> functionFactory, RepositoryStub repositoryStub)
        {
            _functionFactory = functionFactory;
            _repositoryStub = repositoryStub;
            _queue = new ConcurrentQueue<SyncQueueItem>();
        }

        public Task<string> EnqueueEntityForSyncAsync(SyncQueueItem queueItem, CancellationToken cancellationToken)
        {
            _queue.Enqueue(queueItem);
            return Task.FromResult(Guid.NewGuid().ToString());
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
                    await function.RunAsync(queueItemJson, cancellationToken);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error processing queue item for {queueItem.Entity} as {queueItem.PointInTime}: {ex.Message}", ex);
                }
            }
        }
    }
}