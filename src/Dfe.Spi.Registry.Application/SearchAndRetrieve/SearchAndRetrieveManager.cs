using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;

namespace Dfe.Spi.Registry.Application.SearchAndRetrieve
{
    public interface ISearchAndRetrieveManager
    {
        Task<PublicSearchResult> SearchAsync(SearchRequest request, string entityType, CancellationToken cancellationToken);
        Task<RegisteredEntity> RetrieveAsync(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime, CancellationToken cancellationToken);
        Task<RegisteredEntity[]> RetrieveBatchAsync(EntityPointer[] entityPointers, DateTime pointInTime, CancellationToken cancellationToken);
    }
    
    public class SearchAndRetrieveManager : ISearchAndRetrieveManager
    {
        private readonly IRepository _repository;
        private readonly ILoggerWrapper _logger;
        private readonly ISearchRequestValidator _searchRequestValidator;

        internal SearchAndRetrieveManager(
            IRepository repository,
            ILoggerWrapper logger,
            ISearchRequestValidator searchRequestValidator)
        {
            _repository = repository;
            _logger = logger;
            _searchRequestValidator = searchRequestValidator;
        }
        public SearchAndRetrieveManager(IRepository repository, ILoggerWrapper logger)
            : this(repository, logger, new SearchRequestValidator(repository, logger))
        {
        }
        
        public async Task<PublicSearchResult> SearchAsync(SearchRequest request, string entityType, CancellationToken cancellationToken)
        {
            var validationResult = _searchRequestValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                throw new InvalidRequestException("Search request is invalid", validationResult.ValidationErrors);
            }
            
            if (!request.PointInTime.HasValue)
            {
                request.PointInTime = DateTime.UtcNow;
            }
            
            var searchResult = await _repository.SearchAsync(request, entityType, request.PointInTime.Value, cancellationToken);
            
            return new PublicSearchResult
            {
                Results = searchResult.Results
                    .Select(registeredEntity =>
                        new PublicSearchResultItem
                        {
                            Entities = registeredEntity.Entities
                                .Select(entity =>
                                    new EntityPointer
                                    {
                                        SourceSystemName = entity.SourceSystemName,
                                        SourceSystemId = entity.SourceSystemId,
                                    })
                                .ToArray(),
                            IndexedData = new Entity
                            {
                                Name = GetFirstNotNullValue(registeredEntity.Entities, x => x.Name),
                                Type = GetFirstNotNullValue(registeredEntity.Entities, x => x.Type),
                                SubType = GetFirstNotNullValue(registeredEntity.Entities, x => x.SubType),
                                Status = GetFirstNotNullValue(registeredEntity.Entities, x => x.Status),
                                OpenDate = GetFirstNotNullValue(registeredEntity.Entities, x => x.OpenDate),
                                CloseDate = GetFirstNotNullValue(registeredEntity.Entities, x => x.CloseDate),
                                Urn = GetFirstNotNullValue(registeredEntity.Entities, x => x.Urn),
                                Ukprn = GetFirstNotNullValue(registeredEntity.Entities, x => x.Ukprn),
                                Uprn = GetFirstNotNullValue(registeredEntity.Entities, x => x.Uprn),
                                CompaniesHouseNumber = GetFirstNotNullValue(registeredEntity.Entities, x => x.CompaniesHouseNumber),
                                CharitiesCommissionNumber = GetFirstNotNullValue(registeredEntity.Entities, x => x.CharitiesCommissionNumber),
                                AcademyTrustCode = GetFirstNotNullValue(registeredEntity.Entities, x => x.AcademyTrustCode),
                                DfeNumber = GetFirstNotNullValue(registeredEntity.Entities, x => x.DfeNumber),
                                LocalAuthorityCode = GetFirstNotNullValue(registeredEntity.Entities, x => x.LocalAuthorityCode),
                                ManagementGroupType = GetFirstNotNullValue(registeredEntity.Entities, x => x.ManagementGroupType),
                                ManagementGroupId = GetFirstNotNullValue(registeredEntity.Entities, x => x.ManagementGroupId),
                                ManagementGroupCode = GetFirstNotNullValue(registeredEntity.Entities, x => x.ManagementGroupCode),
                                ManagementGroupUkprn = GetFirstNotNullValue(registeredEntity.Entities, x => x.ManagementGroupUkprn),
                                ManagementGroupCompaniesHouseNumber = GetFirstNotNullValue(registeredEntity.Entities, x => x.ManagementGroupCompaniesHouseNumber),
                            },
                        })
                    .ToArray(),
                Skipped = request.Skip,
                Taken = searchResult.Results.Length,
                TotalNumberOfRecords = searchResult.TotalNumberOfRecords,
            };
        }

        public async Task<RegisteredEntity> RetrieveAsync(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var registeredEntity = await _repository.RetrieveAsync(entityType, sourceSystemName, sourceSystemId, pointInTime, cancellationToken);
            return registeredEntity;
        }

        public async Task<RegisteredEntity[]> RetrieveBatchAsync(EntityPointer[] entityPointers, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var registeredEntities = await _repository.RetrieveBatchAsync(entityPointers, pointInTime, cancellationToken);
            return registeredEntities;
        }


        private TValue GetFirstNotNullValue<TValue>(IEnumerable<LinkedEntity> entities, Func<LinkedEntity, TValue> expression)
        {
            var values = entities.Select(expression).ToArray();
            return values.FirstOrDefault(x => x != null);
        }
    }
}