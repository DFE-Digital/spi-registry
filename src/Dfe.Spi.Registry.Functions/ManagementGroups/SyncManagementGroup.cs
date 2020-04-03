using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.ManagementGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.ManagementGroups
{
    public class SyncManagementGroup
    {
        private const string FunctionName = nameof(SyncManagementGroup);

        private readonly IManagementGroupManager _managementGroupManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public SyncManagementGroup(
            IManagementGroupManager managementGroupManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _managementGroupManager = managementGroupManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName(FunctionName)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "management-groups/sync/{source}")]
            HttpRequest req,
            string source,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"Start processing sync of management group from {source}...");

            var validSource = ValidationHelpers.GetValidSourceSystemName(source);
            if (string.IsNullOrEmpty(validSource))
            {
                _logger.Warning($"Received request to sync management group for unknown system {source}");
                return new NotFoundResult();
            }
            
            ManagementGroup managementGroup;
            using (var reader = new StreamReader(req.Body))
            {
                var json = await reader.ReadToEndAsync();
                _logger.Debug($"Received body {json}");

                managementGroup = JsonConvert.DeserializeObject<ManagementGroup>(json);
                _logger.Info($"Received management group for sync: {JsonConvert.SerializeObject(managementGroup)}");
            }

            await _managementGroupManager.SyncManagementGroupAsync(validSource, managementGroup, cancellationToken);
            _logger.Info("Successfully sync'd management group");
            
            return new AcceptedResult();
        }
    }
}