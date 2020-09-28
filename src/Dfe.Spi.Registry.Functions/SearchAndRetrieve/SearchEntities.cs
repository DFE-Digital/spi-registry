using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Application;
using Dfe.Spi.Registry.Application.SearchAndRetrieve;
using Dfe.Spi.Registry.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.SearchAndRetrieve
{
    public class SearchEntities : FunctionsBase<SearchRequest>
    {
        private readonly ISearchAndRetrieveManager _searchAndRetrieveManager;
        private readonly ILoggerWrapper _logger;
        private readonly IHttpSpiExecutionContextManager _executionContextManager;

        public SearchEntities(
            ISearchAndRetrieveManager searchAndRetrieveManager,
            ILoggerWrapper logger,
            IHttpSpiExecutionContextManager executionContextManager)
            : base(executionContextManager, logger)
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
            var runContext = new SearchEntitiesRunContext {EntityType = entityType};
            
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
            
            var singularEntityType = EntityNameTranslator.Singularise(searchRunContext.EntityType);

            try
            {
                var result = await _searchAndRetrieveManager.SearchAsync(request, singularEntityType, cancellationToken);

                return new FormattedJsonResult(result);
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
    }

    public class SearchEntitiesRunContext : FunctionRunContext
    {
        public string EntityType { get; set; }
    }
}