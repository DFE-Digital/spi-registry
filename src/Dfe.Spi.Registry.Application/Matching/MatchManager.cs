using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Matching;

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
        private readonly ILoggerWrapper _logger;

        public MatchManager(
            IEntityRepository entityRepository,
            IMatchingProfileRepository profileRepository,
            IMatchProfileProcessor matchProfileProcessor,
            ILoggerWrapper logger)
        {
            _entityRepository = entityRepository;
            _profileRepository = profileRepository;
            _matchProfileProcessor = matchProfileProcessor;
            _logger = logger;
        }
        
        public async Task UpdateLinksAsync(EntityForMatching pointer, CancellationToken cancellationToken)
        {
            var source = await _entityRepository.GetEntityAsync(pointer.Type, pointer.SourceSystemName,
                pointer.SourceSystemId, cancellationToken);
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
            var profilesForEntityType = profiles.Where(p =>
                p.SourceType.Equals(entityType, StringComparison.InvariantCultureIgnoreCase) ||
                p.CandidateType.Equals(entityType, StringComparison.InvariantCultureIgnoreCase));
            return profilesForEntityType.ToArray();
        }
    }
}