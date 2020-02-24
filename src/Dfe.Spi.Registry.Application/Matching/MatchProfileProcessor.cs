using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Matching;

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
        private readonly ILoggerWrapper _logger;

        public MatchProfileProcessor(
            IEntityRepository entityRepository,
            ILinkRepository linkRepository,
            ILoggerWrapper logger)
        {
            _entityRepository = entityRepository;
            _linkRepository = linkRepository;
            _logger = logger;
        }

        public async Task UpdateLinksAsync(Entity source, MatchingProfile profile, CancellationToken cancellationToken)
        {
            profile = EnsureProfileSourceMatchesSourceEntityType(source.Type, profile);

            var candidates = await _entityRepository.GetEntitiesOfTypeAsync(source.Type, cancellationToken);

            foreach (var candidate in candidates)
            {
                var matchResult = AssessMatch(source, candidate, profile);
                if (matchResult.IsMatch)
                {
                    var linkReason =
                        $"Matched using profile {profile.Name} against ruleset {matchResult.MatchingRuleset}";
                    var link = new Link
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = profile.LinkType,
                        LinkedEntities = new[]
                        {
                            GetEntityLinkFromEntity(source, linkReason),
                            GetEntityLinkFromEntity(candidate, linkReason),
                        }
                    };
                    await _linkRepository.StoreAsync(link, cancellationToken);

                    await AppendLinkPointerAsync(source, link, cancellationToken);
                    await AppendLinkPointerAsync(candidate, link, cancellationToken);
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

        private static MatchResult AssessMatch(Entity source, Entity candidate, MatchingProfile profile)
        {
            if (source.Type == candidate.Type &&
                source.SourceSystemName == candidate.SourceSystemName &&
                source.SourceSystemId == candidate.SourceSystemId)
            {
                // Can't match against self
                return new MatchResult
                {
                    IsMatch = false,
                };
            }
            
            foreach (var ruleset in profile.Rules)
            {
                var isMatch = true;

                foreach (var criteria in ruleset.Criteria)
                {
                    if (source.Data == null || !source.Data.ContainsKey(criteria.SourceAttribute) ||
                        candidate.Data == null || !candidate.Data.ContainsKey(criteria.SourceAttribute))
                    {
                        isMatch = false;
                        break;
                    }

                    if (source.Data[criteria.SourceAttribute] != candidate.Data[criteria.CandidateAttribute])
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    return new MatchResult
                    {
                        IsMatch = true,
                        MatchingRuleset = ruleset.Name,
                    };
                }
            }

            return new MatchResult
            {
                IsMatch = false,
            };
        }

        private static EntityLink GetEntityLinkFromEntity(Entity entity, string reason)
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

        private class MatchResult
        {
            public bool IsMatch { get; set; }
            public string MatchingRuleset { get; set; }
        }
    }
}