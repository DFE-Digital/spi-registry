using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace Dfe.Spi.Registry.Functions.Entities
{
    public class GetLearningProviderSynonyms
    {
        private const string FunctionName = nameof(GetLearningProviderSynonyms);
        
        private readonly IEntityManager _entityManager;
        private readonly ILoggerWrapper _loggerWrapper;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public GetLearningProviderSynonyms(
            IEntityManager entityManager,
            ILoggerWrapper loggerWrapper,
            IHttpSpiExecutionContextManager executionContextManager)
        {
            _entityManager = entityManager;
            _loggerWrapper = loggerWrapper;
            _executionContextManager = executionContextManager;
        }

        [FunctionName(FunctionName)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "learning-providers/{system}/{id}/synonyms")]
            HttpRequest req,
            string system,
            string id,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _loggerWrapper.Info($"Starting to get synoymns for learning-provider.{system}.{id}");

            var synonyms =
                await _entityManager.GetSynonymousEntitiesAsync("learning-provider", system, id, cancellationToken);
            if (synonyms == null)
            {
                _loggerWrapper.Info(
                    $"Could not find entity/synonyms for learning-provider.{system}.{id}. Returning not found");
                return new NotFoundResult();
            }

            var result = new GetSynonymsResponse
            {
                Synonyms = synonyms,
            };
            return new OkObjectResult(result);
        }
    }

    public class GetSynonymsResponse
    {
        public EntityPointer[] Synonyms { get; set; }
    }
}