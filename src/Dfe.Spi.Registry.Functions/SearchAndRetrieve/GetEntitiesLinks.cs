using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public class GetEntitiesLinks
    {
        private readonly ISearchAndRetrieveManager _searchAndRetrieveManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public GetEntitiesLinks(
            ISearchAndRetrieveManager searchAndRetrieveManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _searchAndRetrieveManager = searchAndRetrieveManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName("GetEntitiesLinks")]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "entities/links")]
            HttpRequest req,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"Start processing retrieval of batch entity links...");
            
            var requestJson = await req.ReadAsStringAsync();
            var request = JsonConvert.DeserializeObject<EntityLinksRequest>(requestJson);

            foreach (var entityPointer in request.Entities)
            {
                entityPointer.EntityType = EntityNameTranslator.Singularise(entityPointer.EntityType);
            }

            var entities = await _searchAndRetrieveManager.RetrieveBatchAsync(request.Entities, request.PointInTime ?? DateTime.Now, cancellationToken);

            var results = request.Entities.Select(requestedPointer =>
            {
                var registeredEntity = entities.SingleOrDefault(re =>
                    re.Entities.Any(e =>
                        e.EntityType.Equals(requestedPointer.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                        e.SourceSystemName.Equals(requestedPointer.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                        e.SourceSystemId.Equals(requestedPointer.SourceSystemId, StringComparison.InvariantCultureIgnoreCase)));
                return new EntityLinksResult
                {
                    Entity = requestedPointer,
                    Links = registeredEntity?.Links,
                };
            }).ToArray();

            return new FormattedJsonResult(new EntityLinksResponse
            {
                Entities = results,
            });
        }
    }
}