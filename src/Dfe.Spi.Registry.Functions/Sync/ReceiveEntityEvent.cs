using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.Sync
{
    public class ReceiveEntityEvent
    {
        private readonly ISyncManager _syncManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public ReceiveEntityEvent(
            ISyncManager syncManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _syncManager = syncManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }

        [FunctionName("ReceiveEntityEvent")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "{entityType}/sync/{sourceSystemName}")]
            HttpRequest req,
            string entityType,
            string sourceSystemName,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"Start processing sync of {entityType} from {sourceSystemName}...");
            
            var singularEntityType = EntityNameTranslator.Singularise(entityType);
            var eventJson = await req.ReadAsStringAsync();
            switch (singularEntityType)
            {
                case EntityNameTranslator.LearningProviderSingular:
                    await _syncManager.ReceiveSyncEntityAsync(
                        JsonConvert.DeserializeObject<SyncEntityEvent<LearningProvider>>(eventJson),
                        sourceSystemName,
                        cancellationToken);
                    break;
                case EntityNameTranslator.ManagementGroupSingular:
                    await _syncManager.ReceiveSyncEntityAsync(
                        JsonConvert.DeserializeObject<SyncEntityEvent<ManagementGroup>>(eventJson),
                        sourceSystemName, 
                        cancellationToken);
                    break;
                default:
                    _logger.Info($"Received entityType {entityType}, which could not be converted to a valid singular value. Returning 400");
                    return new BadRequestResult(); // TODO: include details 
            }
            
            return new AcceptedResult();
        }
    }
}