using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.SearchAndRetrieve;
using Dfe.Spi.Registry.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.SearchAndRetrieve
{
    public class SearchEntities
    {
        private readonly ISearchAndRetrieveManager _searchAndRetrieveManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public SearchEntities(
            ISearchAndRetrieveManager searchAndRetrieveManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _searchAndRetrieveManager = searchAndRetrieveManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName("SearchEntities")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "search/{entityType}")]
            HttpRequest req,
            string entityType,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"Start processing search of {entityType}...");
            
            var requestJson = await req.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<SearchRequest>(requestJson);
            var singularEntityType = EntityNameTranslator.Singularise(entityType);

            var result = await _searchAndRetrieveManager.SearchAsync(request, singularEntityType, cancellationToken);

            return new FormattedJsonResult(result);
        }
    }
}