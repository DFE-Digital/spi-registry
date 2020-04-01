using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.Search
{
    public class SearchEntities
    {
        private const string FunctionName = nameof(SearchEntities);

        private readonly IEntityManager _entityManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public SearchEntities(
            IEntityManager entityManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _entityManager = entityManager;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }
        
        [FunctionName(FunctionName)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "search/{entityType}")]
            HttpRequest req,
            string entityType,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _logger.Info($"{FunctionName} called to search for entities of type {entityType}");
            
            SearchRequest request;
            using (var reader = new StreamReader(req.Body))
            {
                var json = await reader.ReadToEndAsync();
                _logger.Debug($"Received body {json}");
            
                request = JsonConvert.DeserializeObject<SearchRequest>(json);
                _logger.Info($"Searching {entityType} entities using {request}");
            }

            try
            {
                var results = await _entityManager.SearchAsync(request, GetSingularEntityName(entityType), cancellationToken);

                return new OkObjectResult(results);
            }
            catch (InvalidRequestException ex)
            {
                return new BadRequestObjectResult(new
                {
                    ex.Reasons,
                });
            }
        }

        private static string GetSingularEntityName(string pluralEntityName)
        {
            if (string.IsNullOrEmpty(pluralEntityName))
            {
                return string.Empty;
            }

            switch (pluralEntityName.ToLower())
            {
                case "learning-providers":
                    return "learning-provider";
                case "management-groups":
                    return "management-group";
                default:
                    return pluralEntityName;
            }
        }
    }
}