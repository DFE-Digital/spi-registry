using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Application.Matching
{
    public interface IEntityLinker
    {
        Task LinkEntitiesAsync(Entity source, Entity candidate, MatchingProfile profile,
            string matchingRuleset, CancellationToken cancellationToken);
    }

    public class EntityLinker : IEntityLinker
    {
        private readonly IEntityRepository _entityRepository;
        private readonly ILinkRepository _linkRepository;
        private readonly ISearchIndex _searchIndex;
        private readonly ILoggerWrapper _logger;

        public EntityLinker(
            IEntityRepository entityRepository,
            ILinkRepository linkRepository,
            ISearchIndex searchIndex,
            ILoggerWrapper logger)
        {
            _entityRepository = entityRepository;
            _linkRepository = linkRepository;
            _searchIndex = searchIndex;
            _logger = logger;
        }

        public async Task LinkEntitiesAsync(Entity source, Entity candidate, MatchingProfile profile,
            string matchingRuleset, CancellationToken cancellationToken)
        {
            var link = await GetLink(source, candidate, profile, matchingRuleset, cancellationToken);

            _logger.Debug($"Updating link {link.Id} for {source} and {candidate} using profile {profile.Name}");
            await _linkRepository.StoreAsync(link, cancellationToken);

            _logger.Debug(
                $"Updating source entity {source} with link {link.Id} for {candidate} using profile {profile.Name}");
            await AppendLinkPointerAsync(source, link, cancellationToken);
            _logger.Debug(
                $"Updating candidate entity {candidate} with link {link.Id} for {source} using profile {profile.Name}");
            await AppendLinkPointerAsync(candidate, link, cancellationToken);

            if (link.Type == LinkTypes.Synonym)
            {
                await DeleteExistingEntitiesFromSearchIndex(link, cancellationToken);
                await CreateLinkInSearchIndex(link, cancellationToken);
            }
        }

        private async Task<Link> GetLink(Entity source, Entity candidate, MatchingProfile profile,
            string matchingRuleset, CancellationToken cancellationToken)
        {
            var linkReason =
                $"Matched using profile {profile.Name} against ruleset {matchingRuleset}";
            var link = await GetExistingLinkOfProfileType(source, profile, cancellationToken);

            if (link == null)
            {
                _logger.Info(
                    $"{source} and {candidate} match resulting in new link of type {profile.LinkType} using profile {profile.Name}");
                link = new Link
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = profile.LinkType,
                    LinkedEntities = new[]
                    {
                        GetEntityLinkFromEntity(source, linkReason),
                        GetEntityLinkFromEntity(candidate, linkReason),
                    }
                };
            }
            else if (DoesLinkAlreadyContainCandidate(link, candidate))
            {
                _logger.Info(
                    $"{source} and {candidate} are already linked with type {profile.LinkType} so no new links using profile {profile.Name}");
            }
            else
            {
                _logger.Info(
                    $"Updating link {link.Id} to add {candidate}, for link to {source} using profile {profile.Name}");
                link.LinkedEntities = link.LinkedEntities.Concat(new[]
                {
                    GetEntityLinkFromEntity(candidate, linkReason),
                }).ToArray();
            }

            return link;
        }

        private async Task<Link> GetExistingLinkOfProfileType(Entity source, MatchingProfile profile,
            CancellationToken cancellationToken)
        {
            var existingPointer = source.Links?.SingleOrDefault(lp => lp.LinkType == profile.LinkType);
            if (existingPointer == null)
            {
                return null;
            }

            _logger.Info(
                $"Source already has link of type {profile.LinkType}, will see if link needs updating using profile {profile.Name}");

            return await _linkRepository.GetLinkAsync(existingPointer.LinkType, existingPointer.LinkId,
                cancellationToken);
        }

        private bool DoesLinkAlreadyContainCandidate(Link link, Entity candidate)
        {
            return link.LinkedEntities.Any(l => l.EntityType == candidate.Type &&
                                                l.EntitySourceSystemName == candidate.SourceSystemName &&
                                                l.EntitySourceSystemId == candidate.SourceSystemId);
        }

        private EntityLink GetEntityLinkFromEntity(Entity entity, string reason)
        {
            return new EntityLink
            {
                EntityType = entity.Type,
                EntitySourceSystemName = entity.SourceSystemName,
                EntitySourceSystemId = entity.SourceSystemId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Matcher",
                CreatedReason = reason,
            };
        }

        private async Task AppendLinkPointerAsync(Entity entity, Link link, CancellationToken cancellationToken)
        {
            if (entity.Links != null && entity.Links.Any(lp => lp.LinkId == link.Id))
            {
                return;
            }

            var linkPointer = new LinkPointer
            {
                LinkId = link.Id,
                LinkType = link.Type,
            };

            if (entity.Links != null)
            {
                entity.Links = entity.Links.Concat(new[] {linkPointer}).ToArray();
            }
            else
            {
                entity.Links = new[] {linkPointer};
            }

            await _entityRepository.StoreAsync(entity, cancellationToken);
        }

        private async Task DeleteExistingEntitiesFromSearchIndex(Link link, CancellationToken cancellationToken)
        {
            var entityReferencePointers = link.LinkedEntities
                .Select(le => $"entity:{le.EntityType}:{le.EntitySourceSystemName}:{le.EntitySourceSystemId}")
                .Aggregate((x, y) => $"{x},{y}");
            var entityType = link.LinkedEntities.First().EntityType;

            var searchRequest = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchGroup
                    {
                        Filter = new[]
                        {
                            new DataFilter
                            {
                                Field = "ReferencePointer",
                                Operator = DataOperator.In,
                                Value = entityReferencePointers,
                            },
                        },
                        CombinationOperator = "and",
                    },
                },
                CombinationOperator = "and",
                Take = 100,
            };

            var searchResult = await _searchIndex.SearchAsync(searchRequest, entityType, cancellationToken);

            await _searchIndex.DeleteBatchAsync(searchResult.Results, cancellationToken);
        }

        private async Task CreateLinkInSearchIndex(Link link, CancellationToken cancellationToken)
        {
            var referencePointer = $"link:{LinkTypes.Synonym.ToLower()}:{link.Id}";
            var documentId = Guid.NewGuid().ToString();

            // Check if record for synonym already exists
            var existingDocumentSearchResult = await _searchIndex.SearchUsingSingleCriteriaAsync(
                "ReferencePointer", DataOperator.Equals, referencePointer,
                link.LinkedEntities[0].EntityType,
                cancellationToken);
            var existingDocument = existingDocumentSearchResult.Results?.FirstOrDefault();
            if (existingDocument != null)
            {
                documentId = existingDocument.Id;
            }
            
            // Create / Update link in search index
            var linkedEntities = await Task.WhenAll(link.LinkedEntities.Select(le =>
                _entityRepository.GetEntityAsync(le.EntityType, le.EntitySourceSystemName, le.EntitySourceSystemId,
                    cancellationToken)));

            var searchDocument = new SearchDocument
            {
                Id = documentId,
                EntityType = linkedEntities[0].Type,
                ReferencePointer = referencePointer,
                SortableEntityName = linkedEntities[0].Data.GetValue(DataAttributeNames.Name)?.ToLower(),
                Name = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.Name),
                Type = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.Type),
                SubType = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.SubType),
                Status = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.Status),
                OpenDate = GetUniqueNonNullDateTimeDataAttributeValues(linkedEntities, DataAttributeNames.OpenDate),
                CloseDate = GetUniqueNonNullDateTimeDataAttributeValues(linkedEntities, DataAttributeNames.CloseDate),
                Urn = GetUniqueNonNullLongDataAttributeValues(linkedEntities, DataAttributeNames.Urn),
                Ukprn = GetUniqueNonNullLongDataAttributeValues(linkedEntities, DataAttributeNames.Ukprn),
                Uprn = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.Uprn),
                CompaniesHouseNumber = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.CompaniesHouseNumber),
                CharitiesCommissionNumber = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.CharitiesCommissionNumber),
                AcademyTrustCode = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.AcademyTrustCode),
                DfeNumber = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.DfeNumber),
                LocalAuthorityCode = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.LocalAuthorityCode),
                ManagementGroupType = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.ManagementGroupType),
                ManagementGroupId = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.ManagementGroupId),
                ManagementGroupUkprn = GetUniqueNonNullLongDataAttributeValues(linkedEntities, DataAttributeNames.ManagementGroupUkprn),
                ManagementGroupCompaniesHouseNumber = GetUniqueNonNullStringDataAttributeValues(linkedEntities, DataAttributeNames.ManagementGroupCompaniesHouseNumber),
            };

            await _searchIndex.AddOrUpdateAsync(searchDocument, cancellationToken);
        }

        private string[] GetUniqueNonNullStringDataAttributeValues(Entity[] entities, string attributeName)
        {
            return entities
                .Select(e => e.Data.GetValue(attributeName))
                .Where(v => v != null)
                .Distinct()
                .ToArray();
        }

        private DateTime[] GetUniqueNonNullDateTimeDataAttributeValues(Entity[] entities, string attributeName)
        {
            return entities
                .Select(e => e.Data.GetValueAsDateTime(attributeName))
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .Distinct()
                .ToArray();
        }

        private long[] GetUniqueNonNullLongDataAttributeValues(Entity[] entities, string attributeName)
        {
            return entities
                .Select(e => e.Data.GetValueAsLong(attributeName))
                .Where(v => v.HasValue)
                .Select(v => v.Value)
                .Distinct()
                .ToArray();
        }
    }
}