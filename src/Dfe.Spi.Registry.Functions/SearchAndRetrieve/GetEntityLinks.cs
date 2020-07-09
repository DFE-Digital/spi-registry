using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.SearchAndRetrieve;
using Dfe.Spi.Registry.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Dfe.Spi.Registry.Functions.SearchAndRetrieve
{
    public class GetEntityLinks
    {
        private readonly ISearchAndRetrieveManager _searchAndRetrieveManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public GetEntityLinks(
            ISearchAndRetrieveManager searchAndRetrieveManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _searchAndRetrieveManager = searchAndRetrieveManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName("GetEntityLinks")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "{entityType}/{sourceSystemName}/{sourceSystemId}/links")]
            HttpRequest req,
            string entityType,
            string sourceSystemName,
            string sourceSystemId,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"Start processing retrieval of links of {entityType}:{sourceSystemName}:{sourceSystemId}...");

            var pointInTimeKey = req.Query.Keys.FirstOrDefault(k => k.Equals("PointInTime", StringComparison.InvariantCultureIgnoreCase));
            var pointInTimeString = string.IsNullOrEmpty(pointInTimeKey) ? null : req.Query[pointInTimeKey].First();
            var pointInTime = string.IsNullOrEmpty(pointInTimeString) ? DateTime.UtcNow : pointInTimeString.ToDateTime();
            
            var singularEntityType = EntityNameTranslator.Singularise(entityType);
            var registeredEntity = await _searchAndRetrieveManager.RetrieveAsync(singularEntityType, sourceSystemName, sourceSystemId, pointInTime, cancellationToken);
            if (registeredEntity == null)
            {
                return new NotFoundResult();
            }

            var result = new GetLinksResult
            {
                Links = registeredEntity.Links,
            };

            return new FormattedJsonResult(result);
        }
    }

    public class GetLinksResult
    {
        public Link[] Links { get; set; }
    }
}