using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Search;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace Dfe.Spi.Registry.Infrastructure.AzureCognitiveSearch
{
    public class AcsSearchIndex : ISearchIndex
    {
        private static readonly SearchFieldDefinition[] FieldDefinitions;

        private readonly SearchConfiguration _configuration;
        private readonly ILoggerWrapper _logger;


        static AcsSearchIndex()
        {
            var properties = typeof(AcsSearchDocument).GetProperties();
            var definitions = new List<SearchFieldDefinition>();

            foreach (var property in properties)
            {
                var dataType = property.PropertyType.IsArray
                    ? property.PropertyType.GetElementType()
                    : property.PropertyType;
                definitions.Add(new SearchFieldDefinition
                {
                    Name = property.Name,
                    DataType = dataType,
                    IsSearchable = property.GetCustomAttribute(typeof(IsSearchableAttribute)) != null,
                    IsFilterable = property.GetCustomAttribute(typeof(IsFilterableAttribute)) != null,
                    IsArray = property.PropertyType.IsArray,
                });
            }

            FieldDefinitions = definitions.ToArray();
        }

        public AcsSearchIndex(
            SearchConfiguration configuration,
            ILoggerWrapper logger)
        {
            _configuration = configuration;
            _logger = logger;
        }


        public async Task<SearchIndexResult> SearchAsync(SearchRequest request, string entityType,
            CancellationToken cancellationToken)
        {
            using (var client = GetIndexClient())
            {
                var search = BuildSearch(request, entityType);
                _logger.Info(
                    $"Search ACS with query {search.Query} and filter {search.Filter} for items {request.Skip} to {request.Skip + request.Take}...");

                var results = await client.Documents.SearchAsync<AcsSearchDocument>(
                    search.Query,
                    new SearchParameters
                    {
                        QueryType = QueryType.Full,
                        Filter = search.Filter,
                        Skip = request.Skip,
                        Top = request.Take,
                        OrderBy = new[] {"Id"},
                        IncludeTotalResultCount = true,
                    },
                    cancellationToken: cancellationToken);

                var documents = results.Results.Select(acs => acs.Document).ToArray();

                return new SearchIndexResult()
                {
                    Results = documents,
                    Skipped = request.Skip,
                    Taken = request.Take,
                    TotalNumberOfRecords = results.Count ?? 0,
                };
            }
        }

        public async Task AddOrUpdateAsync(SearchDocument document, CancellationToken cancellationToken)
        {
            await AddOrUpdateBatchAsync(new[] {document}, cancellationToken);
        }

        public async Task AddOrUpdateBatchAsync(SearchDocument[] documents, CancellationToken cancellationToken)
        {
            using (var client = GetIndexClient())
            {
                var batch = IndexBatch.Upload(documents.Select(ConvertModelToSearchDocument));

                await client.Documents.IndexAsync(batch, cancellationToken: cancellationToken);
            }
        }

        public async Task DeleteAsync(SearchDocument document, CancellationToken cancellationToken)
        {
            await DeleteBatchAsync(new[] {document}, cancellationToken);
        }

        public async Task DeleteBatchAsync(SearchDocument[] documents, CancellationToken cancellationToken)
        {
            using (var client = GetIndexClient())
            {
                var batch = IndexBatch.Delete(documents.Select(ConvertModelToSearchDocument));

                await client.Documents.IndexAsync(batch, cancellationToken: cancellationToken);
            }
        }


        private SearchIndexClient GetIndexClient()
        {
            return new SearchIndexClient(_configuration.AzureCognitiveSearchServiceName, _configuration.IndexName,
                new SearchCredentials(_configuration.AzureCognitiveSearchKey));
        }

        private AcsSearch BuildSearch(SearchRequest request, string entityType)
        {
            var userSearch = new AcsSearch(request.CombinationOperator);

            foreach (var searchGroup in request.Groups)
            {
                var group = new AcsSearch(searchGroup.CombinationOperator);

                foreach (var requestFilter in searchGroup.Filter)
                {
                    var definition = FieldDefinitions.Single(fd =>
                        fd.Name.Equals(requestFilter.Field, StringComparison.InvariantCultureIgnoreCase));

                    if (definition.IsSearchable || definition.IsFilterable)
                    {
                        group.AppendFilter(definition, requestFilter.Operator, requestFilter.Value);
                    }
                    else
                    {
                        throw new Exception($"{requestFilter.Field} is neither searchable nor filterable");
                    }
                }

                userSearch.AddGroup(group);
            }


            var search = new AcsSearch("and");
            search.AddGroup(userSearch);
            search.AppendFilter(FieldDefinitions.Single(fd => fd.Name == "EntityType"), DataOperator.Equals,
                entityType);

            if (string.IsNullOrEmpty(search.Query))
            {
                search.Query = "*";
            }

            return search;
        }

        private AcsSearchDocument ConvertModelToSearchDocument(SearchDocument model)
        {
            return new AcsSearchDocument
            {
                Id = model.Id,
                SortableEntityName = model.SortableEntityName,
                EntityType = model.EntityType,
                ReferencePointer = model.ReferencePointer,
                Name = model.Name,
                Type = model.Type,
                SubType = model.SubType,
                OpenDate = model.OpenDate,
                CloseDate = model.CloseDate,
                Urn = model.Urn,
                Ukprn = model.Ukprn,
                Uprn = model.Uprn,
                CompaniesHouseNumber = model.CompaniesHouseNumber,
                CharitiesCommissionNumber = model.CharitiesCommissionNumber,
                AcademyTrustCode = model.AcademyTrustCode,
                DfeNumber = model.DfeNumber,
                LocalAuthorityCode = model.LocalAuthorityCode,
                ManagementGroupType = model.ManagementGroupType,
                ManagementGroupId = model.ManagementGroupId,
            };
        }
    }
}