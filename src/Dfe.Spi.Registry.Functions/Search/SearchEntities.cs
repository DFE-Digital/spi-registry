using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
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
    public class SearchEntities : FunctionsBase<SearchRequest>
    {
        private const string FunctionName = nameof(SearchEntities);

        private readonly IEntityManager _entityManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public SearchEntities(
            IEntityManager entityManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
            : base(executionContextManager, logger)
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
            var runContext = new SearchEntitiesRunContext
            {
                EntityType = entityType,
            };

            return await ValidateAndRunAsync(req, runContext, cancellationToken);
        }

        protected override HttpErrorBodyResult GetMalformedErrorResponse(FunctionRunContext runContext)
        {
            return new HttpErrorBodyResult(
                HttpStatusCode.BadRequest,
                Errors.SearchMalformedRequest.Code,
                Errors.SearchMalformedRequest.Message);
        }

        protected override HttpErrorBodyResult GetSchemaValidationResponse(JsonSchemaValidationException validationException, FunctionRunContext runContext)
        {
            return new HttpSchemaValidationErrorBodyResult(Errors.SearchSchemaValidation.Code, validationException);
        }

        protected override async Task<IActionResult> ProcessWellFormedRequestAsync(SearchRequest request, FunctionRunContext runContext,
            CancellationToken cancellationToken)
        {
            var searchRunContext = (SearchEntitiesRunContext) runContext;
            try
            {
                var results = await _entityManager.SearchAsync(request, GetSingularEntityName(searchRunContext.EntityType), cancellationToken);

                return new OkObjectResult(results);
            }
            catch (InvalidRequestException ex)
            {
                return new HttpErrorBodyResult(
                    new HttpDetailedErrorBody
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorIdentifier = Errors.SearchCodeValidation.Code,
                        Message = Errors.SearchCodeValidation.Message,
                        Details = ex.Reasons,
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

    public class SearchEntitiesRunContext : FunctionRunContext
    {
        public string EntityType { get; set; }
    }
}