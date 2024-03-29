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
            string queueItem,
            CancellationToken cancellationToken)
        {
            var tempInternalRequestId = Guid.NewGuid();
            _executionContextManager.SetInternalRequestId(tempInternalRequestId);
            
            _logger.Info($"Queue item content: {queueItem}");
        
            var syncQueueItem = JsonConvert.DeserializeObject<SyncQueueItem>(queueItem);
            _logger.Debug($"Deserialized content to {JsonConvert.SerializeObject(syncQueueItem)}");
        
            if (syncQueueItem.InternalRequestId.HasValue)
            {
                _executionContextManager.SetInternalRequestId(syncQueueItem.InternalRequestId.Value);
                _logger.Info($"Changed internal request id from {tempInternalRequestId} to {syncQueueItem.InternalRequestId.Value} to correlate processing with receipt");
            }
            await _syncManager.ProcessSyncQueueItemAsync(syncQueueItem, cancellationToken);
        }
    }
}