using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain.Matching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.Matching
{
    public class UpdateEntityLinks
    {
        private const string FunctionName = nameof(UpdateEntityLinks);

        private readonly IMatchManager _matchManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public UpdateEntityLinks(
            IMatchManager matchManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _matchManager = matchManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName("UpdateEntityLinks")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "matching/sync")]
            HttpRequest req,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info("Start update of entity links...");
            
            EntityForMatching pointer;
            using (var reader = new StreamReader(req.Body))
            {
                var json = await reader.ReadToEndAsync();
                _logger.Debug($"Received body {json}");
            
                pointer = JsonConvert.DeserializeObject<EntityForMatching>(json);
                _logger.Info($"Received pointer for updating links: {pointer}");
            }
            
            await _matchManager.UpdateLinksAsync(pointer, cancellationToken);
            _logger.Info($"Successfully updated links of {pointer}");
            
            return new AcceptedResult();
        }
    }
}