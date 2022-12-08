using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Matching;

namespace Dfe.Spi.Registry.Application.Matching
{
    public interface IMatcher
    {
        Task<MatchResult> MatchAsync(Entity sourceEntity, DateTime pointInTime, CancellationToken cancellationToken);
    }

    public class Matcher : IMatcher
    {
        private readonly IRepository _repository;
        private readonly IMatchingProfileRepository _matchingProfileRepository;
        private readonly ILoggerWrapper _logger;

        public Matcher(
            IRepository repository,
            IMatchingProfileRepository matchingProfileRepository,
            ILoggerWrapper logger)
        {
            _repository = repository;
            _matchingProfileRepository = matchingProfileRepository;
            _logger = logger;
        }

        public async Task<MatchResult> MatchAsync(Entity sourceEntity, DateTime pointInTime, CancellationToken cancellationToken)
        {
            var profiles = await _matchingProfileRepository.GetMatchingProfilesForEntityTypeAsync(sourceEntity.EntityType, cancellationToken);
            var synonyms = await GetSynonyms(sourceEntity, pointInTime, profiles, cancellationToken);
            var links = await GetLinks(sourceEntity, pointInTime, profiles, cancellationToken);

            var synonymousEntities = synonyms
                .SelectMany(x => x.RegisteredEntity.Entities)
                .Distinct(new LinkedEntityEqualityComparer())
                .ToArray();
            foreach (var synonymousEntity in synonymousEntities)
            {
                var synonymLinks = await GetLinks(synonymousEntity, pointInTime, profiles, cancellationToken);
                foreach (var synonymLink in synonymLinks)
                {
                    if (!links.Any(l => l.LinkType.Equals(synonymLink.LinkType, StringComparison.InvariantCultureIgnoreCase) &&
                                        l.Entity.EntityType.Equals(synonymLink.Entity.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                                                                   l.Entity.SourceSystemName.Equals(synonymLink.Entity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                                                                   l.Entity.SourceSystemId.Equals(synonymLink.Entity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        synonymLink.LinkFromSynonym = true;
                        synonymLink.LinkFromSynonym = true;
                        links.Add(synonymLink);
                    }
                }
            }

            return new MatchResult
            {
                Synonyms = synonyms.ToArray(),
                Links = links.ToArray(),
            };
        }

        private async Task<List<MatchResultItem>> GetSynonyms(Entity sourceEntity, DateTime pointInTime, MatchingProfile[] profiles,
            CancellationToken cancellationToken)
        {
            var synonyms = new List<MatchResultItem>();

            var synonymProfiles = profiles.Where(p => p.LinkType.Equals("synonym", StringComparison.InvariantCultureIgnoreCase));
            foreach (var profile in synonymProfiles)
            {
                var ensuredProfile = EnsureProfileSourceMatchesSourceEntityType(sourceEntity.EntityType, profile);
                foreach (var ruleset in ensuredProfile.Rules)
                {
                    var matches = await FindMatches(sourceEntity, pointInTime, ruleset, profile, cancellationToken);
                    foreach (var match in matches)
                    {
                        if (!synonyms.Any(x => x.RegisteredEntity.Id.Equals(match.RegisteredEntity.Id)))
                        {
                            synonyms.Add(match);
                        }
                    }
                }
            }

            return synonyms;
        }

        private async Task<List<MatchResultLink>> GetLinks(Entity sourceEntity, DateTime pointInTime, MatchingProfile[] profiles,
            CancellationToken cancellationToken)
        {
            var links = new List<MatchResultLink>();

            var linkProfiles = profiles.Where(p => !p.LinkType.Equals("synonym", StringComparison.InvariantCultureIgnoreCase));
            foreach (var profile in linkProfiles)
            {
                var ensuredProfile = EnsureProfileSourceMatchesSourceEntityType(sourceEntity.EntityType, profile);
                foreach (var ruleset in ensuredProfile.Rules)
                {
                    var matches = await FindMatches(sourceEntity, pointInTime, ruleset, profile, cancellationToken);
                    foreach (var match in matches)
                    {
                        var matchedEntity = GetMatchingEntity(sourceEntity, match.RegisteredEntity, ruleset);
                        links.Add(new MatchResultLink
                        {
                            RegisteredEntity = match.RegisteredEntity,
                            Entity = matchedEntity,
                            LinkType = profile.LinkType,
                            MatchReason = match.MatchReason,
                        });
                    }
                }
            }

            return links;
        }

        private static MatchingProfile EnsureProfileSourceMatchesSourceEntityType(string sourceEntityType,
            MatchingProfile profile)
        {
            if (profile.SourceType.Equals(sourceEntityType, StringComparison.InvariantCultureIgnoreCase))
            {
                return profile;
            }

            return new MatchingProfile
            {
                Name = profile.Name,
                SourceType = profile.CandidateType,
                CandidateType = profile.SourceType,
                LinkType = profile.LinkType,
                Rules = profile.Rules.Select(ruleset =>
                    new MatchingProfileRuleset
                    {
                        Name = ruleset.Name,
                        Conditions = ruleset.Conditions.Select(c =>
                            new MatchingProfileCondition
                            {
                                SourceAttribute = c.CandidateAttribute,
                                CandidateAttribute = c.SourceAttribute,
                            }).ToArray(),
                    }).ToArray(),
            };
        }

        private async Task<MatchResultItem[]> FindMatches(
            Entity sourceEntity,
            DateTime pointInTime,
            MatchingProfileRuleset ruleset,
            MatchingProfile profile,
            CancellationToken cancellationToken)
        {
            var searchFilter = ruleset.Conditions
                .Select(condition => ConvertRulesetConditionToSearchFilter(condition, sourceEntity))
                .Where(filter => filter != null)
                .ToArray();
            if (searchFilter.Length == 0)
            {
                _logger.Info($"No conditions could be fulfilled by source entity {sourceEntity} when finding candidates for profile {profile.Name} and ruleset {ruleset.Name}");
                return new MatchResultItem[0];
            }

            var searchRequest = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = searchFilter,
                        CombinationOperator = "and",
                    },
                },
                CombinationOperator = "and",
                Skip = 0,
                Take = 1000,
            };
            var searchResult = await _repository.SearchAsync(searchRequest, profile.CandidateType, pointInTime, cancellationToken);
            var candidates = searchResult.Results;
            
            bool CandidateHasMoreEntitiesThanSource(RegisteredEntity candidate)
            {
                var numberOfEntitiesThatAreSourceEntity =
                    candidate.Entities.Count(entity =>
                        entity.EntityType.Equals(sourceEntity.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                        entity.SourceSystemName.Equals(sourceEntity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                        entity.SourceSystemId.Equals(sourceEntity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase));

                return candidate.Entities.Length - numberOfEntitiesThatAreSourceEntity > 0;
            }

            return candidates
                .Where(CandidateHasMoreEntitiesThanSource)
                .Select(candidate =>
                    new MatchResultItem
                    {
                        RegisteredEntity = candidate,
                        MatchReason = $"Matched using ruleset {ruleset.Name} in profile {profile.Name}",
                    })
                .ToArray();
        }

        private Entity GetMatchingEntity(Entity sourceEntity, RegisteredEntity matchedRegisteredEntity, MatchingProfileRuleset ruleset)
        {
            return matchedRegisteredEntity.Entities[0];
        }

        private SearchRequestFilter ConvertRulesetConditionToSearchFilter(MatchingProfileCondition condition, Entity sourceEntity)
        {
            var value = GetSourceValue(sourceEntity, condition.SourceAttribute);
            if (value == null)
            {
                return condition.MatchNulls
                    ? new SearchRequestFilter
                    {
                        Field = condition.CandidateAttribute,
                        Operator = DataOperator.IsNull,
                    }
                    : null;
            }

            return new SearchRequestFilter
            {
                Field = condition.CandidateAttribute,
                Operator = DataOperator.Equals,
                Value = value,
            };
        }

        private string GetSourceValue(Entity sourceEntity, string attributeName)
        {
            switch (attributeName.ToLower())
            {
                case "name":
                    return sourceEntity.Name;
                case "type":
                    return sourceEntity.Type;
                case "subtype":
                    return sourceEntity.SubType;
                case "status":
                    return sourceEntity.Status;
                case "opendate":
                    return sourceEntity.OpenDate.HasValue
                        ? sourceEntity.OpenDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")
                        : null;
                case "closedate":
                    return sourceEntity.CloseDate.HasValue
                        ? sourceEntity.CloseDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")
                        : null;
                case "urn":
                    return sourceEntity.Urn?.ToString();
                case "ukprn":
                    return sourceEntity.Ukprn?.ToString();
                case "uprn":
                    return sourceEntity.Uprn;
                case "companieshousenumber":
                    return sourceEntity.CompaniesHouseNumber;
                case "charitiescommissionnumber":
                    return sourceEntity.CharitiesCommissionNumber;
                case "academytrustcode":
                    return sourceEntity.AcademyTrustCode;
                case "dfenumber":
                    return sourceEntity.DfeNumber;
                case "localauthoritycode":
                    return sourceEntity.LocalAuthorityCode;
                case "managementgrouptype":
                    return sourceEntity.ManagementGroupType;
                case "managementgroupid":
                    return sourceEntity.ManagementGroupId;
                case "managementgroupcode":
                    return sourceEntity.ManagementGroupCode;
                case "managementgroupukprn":
                    return sourceEntity.ManagementGroupUkprn?.ToString();
                case "managementgroupcompanieshousenumber":
                    return sourceEntity.ManagementGroupCompaniesHouseNumber;
            }

            return null;
        }

        private class LinkedEntityEqualityComparer : IEqualityComparer<LinkedEntity>
        {
            public bool Equals(LinkedEntity x, LinkedEntity y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                return x != null && y != null &&
                       x.EntityType.Equals(y.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                       x.SourceSystemName.Equals(y.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                       x.SourceSystemId.Equals(y.SourceSystemId, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(LinkedEntity obj)
            {
                return obj.ToString().GetHashCode();
            }
        }
    }
}