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

            source = await _entityRepository.GetEntityAsync(pointer.Type, pointer.SourceSystemName,
                pointer.SourceSystemId, cancellationToken);
            if (source.Links == null || !source.Links.Any(l => l.LinkType == LinkTypes.Synonym))
            {
                var document = MapEntityToSearchDocument(source);
                await _searchIndex.AddOrUpdateAsync(document, cancellationToken);
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

        private SearchDocument MapEntityToSearchDocument(Entity entity)
        {
            return new SearchDocument
            {
                Id = Guid.NewGuid().ToString(),
                EntityType = entity.Type,
                ReferencePointer = $"entity:{entity.Type}:{entity.SourceSystemName}:{entity.SourceSystemId}",
                SortableEntityName = entity.Data.GetValue(DataAttributeNames.Name)?.ToLower(),
                Name = ValueToArray(entity.Data.GetValue(DataAttributeNames.Name)),
                Type = ValueToArray(entity.Data.GetValue(DataAttributeNames.Type)),
                SubType = ValueToArray(entity.Data.GetValue(DataAttributeNames.SubType)),
                OpenDate = ValueToArray(entity.Data.GetValueAsDateTime(DataAttributeNames.OpenDate)),
                CloseDate = ValueToArray(entity.Data.GetValueAsDateTime(DataAttributeNames.CloseDate)),
                Urn = ValueToArray(entity.Data.GetValueAsLong(DataAttributeNames.Urn)),
                Ukprn = ValueToArray(entity.Data.GetValueAsLong(DataAttributeNames.Ukprn)),
                Uprn = ValueToArray(entity.Data.GetValue(DataAttributeNames.Uprn)),
                CompaniesHouseNumber = ValueToArray(entity.Data.GetValue(DataAttributeNames.CompaniesHouseNumber)),
                CharitiesCommissionNumber =
                    ValueToArray(entity.Data.GetValue(DataAttributeNames.CharitiesCommissionNumber)),
                AcademyTrustCode = ValueToArray(entity.Data.GetValue(DataAttributeNames.AcademyTrustCode)),
                DfeNumber = ValueToArray(entity.Data.GetValue(DataAttributeNames.DfeNumber)),
                LocalAuthorityCode = ValueToArray(entity.Data.GetValue(DataAttributeNames.LocalAuthorityCode)),
                ManagementGroupType = ValueToArray(entity.Data.GetValue(DataAttributeNames.ManagementGroupType)),
                ManagementGroupId = ValueToArray(entity.Data.GetValue(DataAttributeNames.ManagementGroupId)),
            };
        }

        private string[] ValueToArray(string value)
        {
            return string.IsNullOrEmpty(value)
                ? new string[0]
                : new[] {value};
        }

        private DateTime[] ValueToArray(DateTime? value)
        {
            return !value.HasValue
                ? new DateTime[0]
                : new[] {value.Value};
        }

        private long[] ValueToArray(long? value)
        {
            return !value.HasValue
                ? new long[0]
                : new[] {value.Value};
        }
    }
}