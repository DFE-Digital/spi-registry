using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions
{
    public class GetLearningProviderSynonymsStub
    {
        private const string FunctionName = nameof(GetLearningProviderSynonymsStub);

        private readonly RegistryConfiguration _configuration;
        private readonly ILoggerWrapper _logger;

        public GetLearningProviderSynonymsStub(RegistryConfiguration configuration, ILoggerWrapper logger)
        {
            _configuration = configuration;
            _logger = logger;
        }
        
        [FunctionName(FunctionName)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "learning-providers/{system}/{id}/synonyms")]
            HttpRequest req,
            string system,
            string id,
            CancellationToken cancellationToken)
        {
            _logger.SetContext(req.Headers);
            _logger.Info($"{FunctionName} triggered at {DateTime.Now} with system {system} and id {id}");

            if (!system.Equals("GIAS"))
            {
                _logger.Debug($"Only support GIAS as system, but received {system}");
                return new NotFoundResult();
            }

            long urn;
            if (!long.TryParse(id, out urn))
            {
                return new BadRequestObjectResult(new HttpErrorBody
                {
                    ErrorIdentifier = "REG-URN-NOTNUMERIC",
                    Message = "id must be a urn (numeric)",
                    StatusCode = HttpStatusCode.BadRequest,
                });
            }

            var entity = await GetEstablishmentAsync(urn, cancellationToken);
            if (entity == null)
            {
                _logger.Debug($"Could not find an establishment with urn {urn}");
                return new NotFoundResult();
            }

            var result = new StubSynonymResult();
            if (entity.Ukprn.HasValue && entity.Ukprn > 0)
            {
                result.Synonyms = new[]
                {
                    new EntityPointer
                    {
                        SourceSystem = "UKRLP",
                        SourceSystemId = entity.Ukprn.ToString(),
                    },
                };
            }
            return new OkObjectResult(result);
        }


        private async Task<CloudTable> GetTableAsync(CancellationToken cancellationToken)
        {
            var storageAccount = CloudStorageAccount.Parse(_configuration.GiasAdapter.CacheConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("establishments");

            await table.CreateIfNotExistsAsync(cancellationToken);

            return table;
        }

        private async Task<EstablishmentEntity> GetEstablishmentAsync(long urn, CancellationToken cancellationToken)
        {
            var table = await GetTableAsync(cancellationToken);
            
            var operation = TableOperation.Retrieve<EstablishmentEntity>(urn.ToString(), "current");
            var operationResult = await table.ExecuteAsync(operation, cancellationToken);
            var entity = (EstablishmentEntity) operationResult.Result;
            return entity;
        }
        
        
        private class EstablishmentEntity : TableEntity
        {
            public long Urn { get; set; }
            public string Name { get; set; }
            public long? Ukprn { get; set; }
        }
    }

    public class StubSynonymResult
    {
        public EntityPointer[] Synonyms { get; set; } = new EntityPointer[0];
    }

    public class EntityPointer
    {
        public string SourceSystem { get; set; }
        public string SourceSystemId { get; set; }
    }
}