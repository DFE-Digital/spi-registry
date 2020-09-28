using System;
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

        public SearchAndRetrieveManager(
            IRepository repository,
            ILoggerWrapper logger)
        {
            _repository = repository;
            _logger = logger;
        }
        
        public async Task<PublicSearchResult> SearchAsync(SearchRequest request, string entityType, CancellationToken cancellationToken)
        {
            if (request.Take > 100)
            {
                throw new InvalidRequestException("Search request is invalid",
                    new[]
                    {
                        $"request has invalid Take ({request.Take}). Must between 1 and 100 inclusive"
                    });
            }
            
            if (request.Skip < 0)
            {
                request.Skip = 0;
            }
            if (request.Take < 1)
            {
                request.Take = 1;
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
                                Name = registeredEntity.Entities[0].Name,
                                Type = registeredEntity.Entities[0].Type,
                                SubType = registeredEntity.Entities[0].SubType,
                                Status = registeredEntity.Entities[0].Status,
                                OpenDate = registeredEntity.Entities[0].OpenDate,
                                CloseDate = registeredEntity.Entities[0].CloseDate,
                                Urn = registeredEntity.Entities[0].Urn,
                                Ukprn = registeredEntity.Entities[0].Ukprn,
                                Uprn = registeredEntity.Entities[0].Uprn,
                                CompaniesHouseNumber = registeredEntity.Entities[0].CompaniesHouseNumber,
                                CharitiesCommissionNumber = registeredEntity.Entities[0].CharitiesCommissionNumber,
                                AcademyTrustCode = registeredEntity.Entities[0].AcademyTrustCode,
                                DfeNumber = registeredEntity.Entities[0].DfeNumber,
                                LocalAuthorityCode = registeredEntity.Entities[0].LocalAuthorityCode,
                                ManagementGroupType = registeredEntity.Entities[0].ManagementGroupType,
                                ManagementGroupId = registeredEntity.Entities[0].ManagementGroupId,
                                ManagementGroupCode = registeredEntity.Entities[0].ManagementGroupCode,
                                ManagementGroupUkprn = registeredEntity.Entities[0].ManagementGroupUkprn,
                                ManagementGroupCompaniesHouseNumber = registeredEntity.Entities[0].ManagementGroupCompaniesHouseNumber,
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
    }
}