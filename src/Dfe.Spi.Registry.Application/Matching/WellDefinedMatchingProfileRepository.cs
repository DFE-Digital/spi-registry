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
                        SourceAttribute = "urn",
                        CandidateAttribute = "urn",
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
                        SourceAttribute = "ukprn",
                        CandidateAttribute = "ukprn",
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
    }
}