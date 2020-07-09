using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.SearchAndRetrieve;
using Dfe.Spi.Registry.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.SearchAndRetrieve
{
    public class GetEntitySynonyms
    {
        private readonly ISearchAndRetrieveManager _searchAndRetrieveManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public GetEntitySynonyms(
            ISearchAndRetrieveManager searchAndRetrieveManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _searchAndRetrieveManager = searchAndRetrieveManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName("GetEntitySynonyms")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "{entityType}/{sourceSystemName}/{sourceSystemId}/synonyms")]
            HttpRequest req,
            string entityType,
            string sourceSystemName,
            string sourceSystemId,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"Start processing retrieval of synonyms of {entityType}:{sourceSystemName}:{sourceSystemId}...");

            var pointInTimeKey = req.Query.Keys.FirstOrDefault(k => k.Equals("PointInTime", StringComparison.InvariantCultureIgnoreCase));
            var pointInTimeString = string.IsNullOrEmpty(pointInTimeKey) ? null : req.Query[pointInTimeKey].First();
            var pointInTime = string.IsNullOrEmpty(pointInTimeString) ? DateTime.UtcNow : pointInTimeString.ToDateTime();
            
            var singularEntityType = EntityNameTranslator.Singularise(entityType);
            var registeredEntity = await _searchAndRetrieveManager.RetrieveAsync(singularEntityType, sourceSystemName, sourceSystemId, pointInTime, cancellationToken);
            if (registeredEntity == null)
            {
                return new NotFoundResult();
            }

            var result = new GetSynonymsResult
            {
                Synonyms = registeredEntity.Entities
                    .Where(e => !(e.SourceSystemName.Equals(sourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                                  e.SourceSystemId.Equals(sourceSystemId, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(e =>
                        new EntityPointer
                        {
                            SourceSystemName = e.SourceSystemName,
                            SourceSystemId = e.SourceSystemId,
                        })
                    .ToArray(),
            };

            return new FormattedJsonResult(result);
        }
    }

    public class GetSynonymsResult
    {
        public EntityPointer[] Synonyms { get; set; }
    }
}