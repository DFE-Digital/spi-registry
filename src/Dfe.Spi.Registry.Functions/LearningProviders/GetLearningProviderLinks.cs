using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Dfe.Spi.Registry.Functions.LearningProviders
{
    public class GetLearningProviderLinks
    {
        private const string FunctionName = nameof(GetLearningProviderLinks);
        
        private readonly IEntityManager _entityManager;
        private readonly ILoggerWrapper _loggerWrapper;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public GetLearningProviderLinks(
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
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "learning-providers/{system}/{id}/links")]
            HttpRequest req,
            string system,
            string id,
            CancellationToken cancellationToken)
        {
            _executionContextManager.SetContext(req.Headers);
            _loggerWrapper.Info($"Starting to get links for {TypeNames.LearningProvider}.{system}.{id}");

            var links =
                await _entityManager.GetEntityLinksAsync(TypeNames.LearningProvider, system, id, cancellationToken);
            if (links == null)
            {
                _loggerWrapper.Info(
                    $"Could not find entity/links for {TypeNames.LearningProvider}.{system}.{id}. Returning not found");
                return new NotFoundResult();
            }

            var result = new GetLinksResponse
            {
                Links = links,
            };
            return new OkObjectResult(result);
        }
    }

    public class GetLinksResponse
    {
        public LinkedEntityPointer[] Links { get; set; }
    }
}