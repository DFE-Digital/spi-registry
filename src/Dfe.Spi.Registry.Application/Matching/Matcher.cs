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
            var synonyms = new List<MatchResultSynonym>();
            var links = new List<MatchResultLink>();

            var profiles = await _matchingProfileRepository.GetMatchingProfilesForEntityTypeAsync(sourceEntity.EntityType, cancellationToken);
            foreach (var profile in profiles)
            {
                var ensuredProfile = EnsureProfileSourceMatchesSourceEntityType(sourceEntity.EntityType, profile);
                foreach (var ruleset in ensuredProfile.Rules)
                {
                    var matches = await FindMatches(sourceEntity, pointInTime, ruleset, profile, cancellationToken);

                    foreach (var match in matches)
                    {
                        if (profile.LinkType.Equals("synonym") && !synonyms.Any(x => x.RegisteredEntity.Id.Equals(match.RegisteredEntity.Id)))
                        {
                            synonyms.Add(match);
                        }
                        else if (!profile.LinkType.Equals("synonym"))
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
            }

            return new MatchResult
            {
                Synonyms = synonyms.ToArray(),
                Links = links.ToArray(),
            };
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

        private async Task<MatchResultSynonym[]> FindMatches(
            Entity sourceEntity,
            DateTime pointInTime,
            MatchingProfileRuleset ruleset,
            MatchingProfile profile,
            CancellationToken cancellationToken)
        {
            var searchRequest = new SearchRequest
            {
                Groups = new[]
                {
                    new SearchRequestGroup
                    {
                        Filter = ruleset.Conditions
                            .Select(condition => ConvertRulesetConditionToSearchFilter(condition, sourceEntity))
                            .ToArray(),
                        CombinationOperator = "and",
                    },
                },
                CombinationOperator = "and",
                Skip = 0,
                Take = 1000,
            };
            var candidates = await _repository.SearchAsync(searchRequest, profile.CandidateType, pointInTime, cancellationToken);

            return candidates
                .Where(candidate =>
                    !candidate.Entities.Any(entity =>
                        entity.EntityType.Equals(sourceEntity.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                        entity.SourceSystemName.Equals(sourceEntity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                        entity.SourceSystemId.Equals(sourceEntity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase)))
                .Select(candidate =>
                    new MatchResultSynonym
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
                return new SearchRequestFilter
                {
                    Field = condition.CandidateAttribute,
                    Operator = DataOperator.IsNull,
                };
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
                case "managementgroupukprn":
                    return sourceEntity.ManagementGroupUkprn?.ToString();
                case "managementgroupcompanieshousenumber":
                    return sourceEntity.ManagementGroupCompaniesHouseNumber;
            }

            return null;
        }
    }
}