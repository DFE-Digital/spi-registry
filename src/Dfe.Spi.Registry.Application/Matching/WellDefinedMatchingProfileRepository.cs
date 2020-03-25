using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Matching;

namespace Dfe.Spi.Registry.Application.Matching
{
    public class WellDefinedMatchingProfileRepository : IMatchingProfileRepository
    {
        public Task<MatchingProfile[]> GetMatchingProfilesAsync(CancellationToken cancellationToken)
        {
            var profiles = new[]
            {
                GetLearningProviderSynonymProfile(),
                GetLearningProviderManagementGroupProfile(),
            };
            return Task.FromResult(profiles);
        }


        private MatchingProfile GetLearningProviderSynonymProfile()
        {
            var urnRuleset = new MatchingRuleset
            {
                Name = "Match by URN",
                Criteria = new []
                {
                    new MatchingCriteria
                    {
                        SourceAttribute = DataAttributeNames.Urn,
                        CandidateAttribute = DataAttributeNames.Urn,
                    }, 
                },
            };
            var ukprnRuleset = new MatchingRuleset
            {
                Name = "Match by UKPRN",
                Criteria = new []
                {
                    new MatchingCriteria
                    {
                        SourceAttribute = DataAttributeNames.Ukprn,
                        CandidateAttribute = DataAttributeNames.Ukprn,
                    }, 
                },
            };
            
            return new MatchingProfile
            {
                Name = "Learning Provider synonyms",
                SourceType = TypeNames.LearningProvider,
                CandidateType = TypeNames.LearningProvider,
                LinkType = LinkTypes.Synonym,
                Rules = new []
                {
                    urnRuleset,
                    ukprnRuleset,
                },
            };
        }
        private MatchingProfile GetLearningProviderManagementGroupProfile()
        {
            var managementGroupRuleset = new MatchingRuleset
            {
                Name = "Match by management group code",
                Criteria = new []
                {
                    new MatchingCriteria
                    {
                        SourceAttribute = DataAttributeNames.ManagementGroupId,
                        CandidateAttribute = DataAttributeNames.ManagementGroupId,
                    }, 
                },
            };
            
            return new MatchingProfile
            {
                Name = "Learning Provider Management Group",
                SourceType = TypeNames.LearningProvider,
                CandidateType = TypeNames.ManagementGroup,
                LinkType = LinkTypes.ManagementGroup,
                Rules = new []
                {
                    managementGroupRuleset,
                },
            };
        }
    }
}