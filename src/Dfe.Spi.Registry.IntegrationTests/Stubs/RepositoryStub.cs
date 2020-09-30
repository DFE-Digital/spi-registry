using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;

namespace Dfe.Spi.Registry.IntegrationTests.Stubs
{
    public class RepositoryStub : IRepository
    {
        private static readonly PropertyInfo[] EntityProperties = typeof(Entity).GetProperties();

        public RepositoryStub()
        {
            Store = new List<RegisteredEntity>();
        }
        
        public List<RegisteredEntity> Store { get; set; }

        public Task StoreAsync(RegisteredEntity registeredEntity, CancellationToken cancellationToken)
        {
            Remove(registeredEntity.Id);
            Store.Add(Clone(registeredEntity));
            return Task.CompletedTask;
        }

        public Task StoreAsync(RegisteredEntity[] registeredEntitiesToUpsert, RegisteredEntity[] registeredEntitiesToDelete,
            CancellationToken cancellationToken)
        {
            foreach (var entityToDelete in registeredEntitiesToDelete)
            {
                Remove(entityToDelete.Id);
            }

            foreach (var entityToUpsert in registeredEntitiesToUpsert)
            {
                Remove(entityToUpsert.Id);
                Store.Add(Clone(entityToUpsert));
            }

            return Task.CompletedTask;
        }

        public Task<RegisteredEntity> RetrieveAsync(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime,
            CancellationToken cancellationToken)
        {
            var entities = Store.Where(registeredEntity => registeredEntity.Entities.Any(
                entity => entity.EntityType.Equals(entityType, StringComparison.InvariantCultureIgnoreCase) &&
                          entity.SourceSystemName.Equals(sourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                          entity.SourceSystemId.Equals(sourceSystemId, StringComparison.InvariantCultureIgnoreCase)));
            var validAtPointInTime = GetRegisteredEntityRelevantAtPointInTime(entities, pointInTime);
            return Task.FromResult(Clone(validAtPointInTime));
        }

        public async Task<RegisteredEntity[]> RetrieveBatchAsync(EntityPointer[] entityPointers, DateTime pointInTime, CancellationToken cancellationToken)
        {
            return new RegisteredEntity[0]; //TODO: Query store
        }

        public Task<SearchResult> SearchAsync(SearchRequest request, string entityType, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var matcherFunc = SearchRequestToMatcherFunc(request);

            var matches = Store
                .Where(e => e.Type.Equals(entityType, StringComparison.InvariantCultureIgnoreCase))
                .Where(matcherFunc);
            var validAtPointInTime = GetRegisteredEntitiesRelevantAtPointInTime(matches, pointInTime).ToArray();

            var result = new SearchResult
            {
                Results = validAtPointInTime
                    .Skip(request.Skip)
                    .Take(request.Take)
                    .Select(Clone)
                    .ToArray(),
                TotalNumberOfRecords = validAtPointInTime.Length,
            };

            return Task.FromResult(result);
        }

        public string[] GetSearchableFieldNames()
        {
            throw new NotImplementedException();
        }

        private RegisteredEntity GetRegisteredEntityRelevantAtPointInTime(IEnumerable<RegisteredEntity> entities, DateTime pointInTime)
        {
            var orderedResultsValidAtPointInTime = GetRegisteredEntitiesRelevantAtPointInTime(entities, pointInTime)
                .OrderByDescending(entity => entity.ValidFrom);
            return orderedResultsValidAtPointInTime.FirstOrDefault();
        }

        private IEnumerable<RegisteredEntity> GetRegisteredEntitiesRelevantAtPointInTime(IEnumerable<RegisteredEntity> entities, DateTime pointInTime)
        {
            return entities.Where(entity => entity.ValidFrom <= pointInTime &&
                                            (!entity.ValidTo.HasValue || entity.ValidTo >= pointInTime));
        }

        private void Remove(string id)
        {
            var item = Store.SingleOrDefault(x => x.Id == id);
            if (item != null)
            {
                Store.Remove(item);
            }
        }

        private RegisteredEntity Clone(RegisteredEntity source)
        {
            if (source == null)
            {
                return null;
            }
            
            return new RegisteredEntity
            {
                Id = source.Id,
                Type = source.Type,
                ValidFrom = source.ValidFrom,
                ValidTo = source.ValidTo,
                Entities = Clone(source.Entities),
                Links = Clone(source.Links),
            };
        }

        private LinkedEntity[] Clone(IEnumerable<LinkedEntity> source)
        {
            return source.Select(Clone).ToArray();
        }

        private LinkedEntity Clone(LinkedEntity source)
        {
            return new LinkedEntity
            {
                EntityType = source.EntityType,
                SourceSystemName = source.SourceSystemName,
                SourceSystemId = source.SourceSystemId,

                Name = source.Name,
                Type = source.Type,
                SubType = source.SubType,
                Status = source.Status,
                OpenDate = source.OpenDate,
                CloseDate = source.CloseDate,
                Urn = source.Urn,
                Ukprn = source.Ukprn,
                Uprn = source.Uprn,
                CompaniesHouseNumber = source.CompaniesHouseNumber,
                CharitiesCommissionNumber = source.CharitiesCommissionNumber,
                AcademyTrustCode = source.AcademyTrustCode,
                DfeNumber = source.DfeNumber,
                LocalAuthorityCode = source.LocalAuthorityCode,
                ManagementGroupType = source.ManagementGroupType,
                ManagementGroupId = source.ManagementGroupId,
                ManagementGroupCode = source.ManagementGroupCode,
                ManagementGroupUkprn = source.ManagementGroupUkprn,
                ManagementGroupCompaniesHouseNumber = source.ManagementGroupCompaniesHouseNumber,

                LinkType = source.LinkType,
                LinkedBy = source.LinkedBy,
                LinkedReason = source.LinkedReason,
                LinkedAt = source.LinkedAt,
            };
        }

        private Link[] Clone(IEnumerable<Link> source)
        {
            return source.Select(Clone).ToArray();
        }

        private Link Clone(Link source)
        {
            return new Link
            {
                EntityType = source.EntityType,
                SourceSystemName = source.SourceSystemName,
                SourceSystemId = source.SourceSystemId,

                LinkType = source.LinkType,
                LinkedBy = source.LinkedBy,
                LinkedReason = source.LinkedReason,
                LinkedAt = source.LinkedAt,
            };
        }

        private Func<RegisteredEntity, bool> SearchRequestToMatcherFunc(SearchRequest request)
        {
            var filterFuncs = request.Groups.Select(SearchRequestGroupToMatcherFunc).ToArray();
            return ArrayOfFiltersToMatcherFunc(filterFuncs, request.CombinationOperator);
        }

        private Func<RegisteredEntity, bool> SearchRequestGroupToMatcherFunc(SearchRequestGroup group)
        {
            var filterFuncs = group.Filter.Select(SearchRequestFilterToMatcherFunc).ToArray();
            return ArrayOfFiltersToMatcherFunc(filterFuncs, group.CombinationOperator);
        }

        private Func<RegisteredEntity, bool> SearchRequestFilterToMatcherFunc(SearchRequestFilter filter)
        {
            return registeredEntity =>
            {
                foreach (var entity in registeredEntity.Entities)
                {
                    var property = EntityProperties.SingleOrDefault(p => p.Name.Equals(filter.Field, StringComparison.InvariantCultureIgnoreCase));
                    if (property == null)
                    {
                        throw new Exception($"Cannot find Entity property with name {filter.Field}");
                    }

                    var value = property.GetValue(entity)?.ToString();
                    var isMatch = false;
                    switch (filter.Operator)
                    {
                        case DataOperator.Equals:
                            isMatch = filter.Value.Equals(value, StringComparison.InvariantCultureIgnoreCase);
                            break;
                        case DataOperator.IsNull:
                            isMatch = value == null;
                            break;
                        default:
                            throw new Exception($"Operator {filter.Operator} is not supported (Used matching {filter.Field})");
                    }

                    if (isMatch)
                    {
                        return true;
                    }
                }

                return false;
            };
        }

        private Func<RegisteredEntity, bool> ArrayOfFiltersToMatcherFunc(Func<RegisteredEntity, bool>[] matchers, string combinationOperator)
        {
            return registeredEntity =>
            {
                var matchCount = 0;
                foreach (var matcher in matchers)
                {
                    var result = matcher(registeredEntity);
                    if (result)
                    {
                        matchCount++;
                    }
                }

                return combinationOperator.Equals("or", StringComparison.InvariantCultureIgnoreCase)
                    ? matchCount > 0
                    : matchCount == matchers.Length;
            };
        }
    }
}