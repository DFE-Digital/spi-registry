using System;
using System.Threading.Tasks;
using System.Threading;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Queuing;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.Matching
{
    public class ProcessMatchingQueue
    {
        private const string FunctionName = nameof(ProcessMatchingQueue);

        private readonly IMatchManager _matchManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public ProcessMatchingQueue(
            IMatchManager matchManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _matchManager = matchManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [StorageAccount("SPI_Queue:StorageQueueConnectionString")]
        [FunctionName("ProcessMatchingQueue")]
        public async Task RunAsync([QueueTrigger(QueueNames.Matching)]
            string queueItemContent, 
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetInternalRequestId(Guid.NewGuid());

            _logger.Info($"{FunctionName} trigger with: {queueItemContent}");

            var entityForMatching = JsonConvert.DeserializeObject<EntityForMatching>(queueItemContent);
            _logger.Debug($"Deserialized to {entityForMatching}");

            await _matchManager.UpdateLinksAsync(entityForMatching, cancellationToken);
        }
    }
}