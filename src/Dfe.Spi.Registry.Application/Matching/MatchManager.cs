using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Application.Matching
{
    public interface IMatchManager
    {
        Task UpdateLinksAsync(EntityForMatching pointer, CancellationToken cancellationToken);
    }

    public class MatchManager : IMatchManager
    {
        private readonly IEntityRepository _entityRepository;
        private readonly IMatchingProfileRepository _profileRepository;
        private readonly IMatchProfileProcessor _matchProfileProcessor;
        private readonly ISearchIndex _searchIndex;
        private readonly ILoggerWrapper _logger;

        public MatchManager(
            IEntityRepository entityRepository,
            IMatchingProfileRepository profileRepository,
            IMatchProfileProcessor matchProfileProcessor,
            ISearchIndex searchIndex,
            ILoggerWrapper logger)
        {
            _entityRepository = entityRepository;
            _profileRepository = profileRepository;
            _matchProfileProcessor = matchProfileProcessor;
            _searchIndex = searchIndex;
            _logger = logger;
        }

        public async Task UpdateLinksAsync(EntityForMatching pointer, CancellationToken cancellationToken)
        {
            var source = await _entityRepository.GetEntityAsync(pointer.Type, pointer.SourceSystemName,
                pointer.SourceSystemId, cancellationToken);
            _logger.Debug($"Found source item for {pointer}");

            var profiles = await GetProfilesForEntityTypeAsync(pointer.Type, cancellationToken);

            foreach (var profile in profiles)
            {
                await _matchProfileProcessor.UpdateLinksAsync(source, profile, cancellationToken);
            }
        }

        private async Task<MatchingProfile[]> GetProfilesForEntityTypeAsync(string entityType,
            CancellationToken cancellationToken)
        {
            var profiles = await _profileRepository.GetMatchingProfilesAsync(cancellationToken);
            _logger.Info($"Found {profiles.Length} matching profiles");

            var profilesForEntityType = profiles.Where(p =>
                    p.SourceType.Equals(entityType, StringComparison.InvariantCultureIgnoreCase) ||
                    p.CandidateType.Equals(entityType, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();
            _logger.Info($"Found {profilesForEntityType.Length} matching profiles for type {entityType}");

            return profilesForEntityType;
        }
    }
}