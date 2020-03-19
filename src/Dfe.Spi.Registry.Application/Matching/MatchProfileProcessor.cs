using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Application.Matching
{
    public interface IMatchProfileProcessor
    {
        Task UpdateLinksAsync(Entity source, MatchingProfile profile, CancellationToken cancellationToken);
    }

    public class MatchProfileProcessor : IMatchProfileProcessor
    {
        private readonly IEntityRepository _entityRepository;
        private readonly ILinkRepository _linkRepository;
        private readonly ISearchIndex _searchIndex;
        private readonly IEntityLinker _entityLinker;
        private readonly ILoggerWrapper _logger;

        public MatchProfileProcessor(
            IEntityRepository entityRepository,
            ILinkRepository linkRepository,
            ISearchIndex searchIndex,
            IEntityLinker entityLinker,
            ILoggerWrapper logger)
        {
            _entityRepository = entityRepository;
            _linkRepository = linkRepository;
            _searchIndex = searchIndex;
            _entityLinker = entityLinker;
            _logger = logger;
        }

        public async Task UpdateLinksAsync(Entity source, MatchingProfile profile, CancellationToken cancellationToken)
        {
            _logger.Info($"Starting to process {source} using profile {profile.Name}");
            profile = EnsureProfileSourceMatchesSourceEntityType(source.Type, profile);

            var matches = await FindMatches(source, profile, cancellationToken);
            foreach (var match in matches)
            {
                if (!AreTheSameEntity(source, match.Candidate))
                {
                    await _entityLinker.LinkEntitiesAsync(source, match.Candidate, profile, match.MatchingRuleset, cancellationToken);
                }
            }
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
                    new MatchingRuleset
                    {
                        Name = ruleset.Name,
                        Criteria = ruleset.Criteria.Select(c =>
                            new MatchingCriteria
                            {
                                SourceAttribute = c.CandidateAttribute,
                                CandidateAttribute = c.SourceAttribute,
                            }).ToArray(),
                    }).ToArray(),
            };
        }

        private async Task<CandidateMatch[]> FindMatches(Entity source, MatchingProfile profile,
            CancellationToken cancellationToken)
        {
            var matches = new List<CandidateMatch>();

            foreach (var ruleset in profile.Rules)
            {
                var filter = ruleset.Criteria.Select(criteria =>
                    new DataFilter
                    {
                        Field = criteria.CandidateAttribute,
                        Operator = DataOperator.Equals,
                        Value = source.Data.ContainsKey(criteria.SourceAttribute)
                            ? source.Data[criteria.SourceAttribute]
                            : null,
                    }).ToArray();
                if (filter.Any(kvp => kvp.Value == null))
                {
                    // Cannot match if source does not have the value
                    continue;
                }

                var candidateSearch = new SearchRequest
                {
                    Groups = new[]
                    {
                        new SearchGroup
                        {
                            Filter = filter,
                            CombinationOperator = "and",
                        },
                    },
                    CombinationOperator = "and",
                    Take = 100,
                };
                var candidateSearchResults = await _searchIndex.SearchAsync(candidateSearch, profile.CandidateType, cancellationToken);
                foreach (var searchDocument in candidateSearchResults.Results)
                {
                    var referenceParts = searchDocument.ReferencePointer.Split(':');
                    if (referenceParts[0] == "entity")
                    {
                        await AddEntityToMatchesList(matches, referenceParts[1], referenceParts[2], referenceParts[3],
                            ruleset.Name, cancellationToken);
                    }
                    else if (referenceParts[0] == "link")
                    {
                        var link = await _linkRepository.GetLinkAsync(referenceParts[1], referenceParts[2],
                            cancellationToken);
                        foreach (var linkedEntity in link.LinkedEntities)
                        {
                            await AddEntityToMatchesList(matches, linkedEntity.EntityType, linkedEntity.EntitySourceSystemName, linkedEntity.EntitySourceSystemId,
                                ruleset.Name, cancellationToken);
                        }
                    }
                }
            }

            return matches.ToArray();
        }

        private async Task AddEntityToMatchesList(List<CandidateMatch> matches, string entityType, string entitySourceSystemName, string entitySourceSystemId, 
            string rulesetName, CancellationToken cancellationToken)
        {
            if (matches.Any(m =>
                m.Candidate.Type == entityType &&
                m.Candidate.SourceSystemName == entitySourceSystemName &&
                m.Candidate.SourceSystemId == entitySourceSystemId))
            {
                return;
            }
            
            var entity = await _entityRepository.GetEntityAsync(entityType, entitySourceSystemName,
                entitySourceSystemId, cancellationToken);
            matches.Add(new CandidateMatch
            {
                Candidate = entity,
                MatchingRuleset = rulesetName,
            });
        }

        private static bool AreTheSameEntity(Entity source, Entity candidate)
        {
            return source.Type == candidate.Type &&
                   source.SourceSystemName == candidate.SourceSystemName &&
                   source.SourceSystemId == candidate.SourceSystemId;
        }

        private class CandidateMatch
        {
            public Entity Candidate { get; set; }
            public string MatchingRuleset { get; set; }
        }
    }
}