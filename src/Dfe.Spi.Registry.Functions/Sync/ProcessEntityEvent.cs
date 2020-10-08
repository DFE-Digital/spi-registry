using System.Threading.Tasks;
using System;
using System.Threading;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Sync;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.Sync
{
    public class ProcessEntityEvent
    {
        private readonly ISyncManager _syncManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public ProcessEntityEvent(
            ISyncManager syncManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _syncManager = syncManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [StorageAccount("SPI_Sync:QueueConnectionString")]
        [FunctionName("ProcessEntityEvent")]
        public async Task RunAsync(
            [QueueTrigger(QueueNames.SyncQueue)]
            CloudQueueMessage queueItem,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetInternalRequestId(Guid.NewGuid());
            
            _logger.Info($"Started processing item {queueItem.Id} from {QueueNames.SyncQueue} for attempt {queueItem.DequeueCount} (Put in queue at {queueItem.InsertionTime})");
            _logger.Info($"Queue item content: {queueItem.AsString}");

            var syncQueueItem = JsonConvert.DeserializeObject<SyncQueueItem>(queueItem.AsString);
            _logger.Debug($"Deserialized content to {JsonConvert.SerializeObject(syncQueueItem)}");

            if (syncQueueItem.InternalRequestId.HasValue)
            {
                _executionContextManager.SetInternalRequestId(syncQueueItem.InternalRequestId.Value);
            }

            await _syncManager.ProcessSyncQueueItemAsync(syncQueueItem, cancellationToken);
        }
    }
}