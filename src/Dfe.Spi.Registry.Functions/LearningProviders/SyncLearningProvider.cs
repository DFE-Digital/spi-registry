using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Models;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Application.LearningProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.LearningProviders
{
    public class SyncLearningProvider
    {
        private const string FunctionName = nameof(SyncLearningProvider);

        private readonly ILearningProviderManager _learningProviderManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public SyncLearningProvider(
            ILearningProviderManager learningProviderManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _learningProviderManager = learningProviderManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName(FunctionName)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "learning-providers/sync/{source}")]
            HttpRequest req,
            string source,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"Start processing sync of learning provider from {source}...");
            
            LearningProvider learningProvider;
            using (var reader = new StreamReader(req.Body))
            {
                var json = await reader.ReadToEndAsync();
                _logger.Debug($"Received body {json}");

                learningProvider = JsonConvert.DeserializeObject<LearningProvider>(json);
                _logger.Info($"Received learning provider for sync: {JsonConvert.SerializeObject(learningProvider)}");
            }

            await _learningProviderManager.SyncLearningProviderAsync(source, learningProvider, cancellationToken);
            _logger.Info("Successfully sync'd learning provider");
            
            return new AcceptedResult();
        }
    }
}