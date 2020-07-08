using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Matching;

namespace Dfe.Spi.Registry.Infrastructure.StaticData.Matching
{
    public class StaticMatchingProfileRepository : IMatchingProfileRepository
    {
        public Task<MatchingProfile[]> GetMatchingProfilesForEntityTypeAsync(string entityType, CancellationToken cancellationToken)
        {
            var profiles = new[]
            {
                GetLearningProviderSynonyms(),
                GetLearningProviderManagementGroup(),
            };

            return Task.FromResult(profiles);
        }

        private MatchingProfile GetLearningProviderSynonyms()
        {
            return new MatchingProfile
            {
                Id = "03884c33-4b1a-4c69-a275-c99161a90a31",
                Name = "Learning provider synonyms",
                SourceType = EntityNameTranslator.LearningProviderSingular,
                CandidateType = EntityNameTranslator.LearningProviderSingular,
                LinkType = "synonym",
                Rules = new[]
                {
                    new MatchingProfileRuleset
                    {
                        Id = "00e162eb-0efe-4138-82fd-24cae71bbef8",
                        Name = "Matching URN",
                        Conditions = new[]
                        {
                            new MatchingProfileCondition
                            {
                                SourceAttribute = "Urn",
                                CandidateAttribute = "Urn",
                            },
                        }
                    },
                    new MatchingProfileRuleset
                    {
                        Id = "ee3d6a80-7a2e-49f8-a45b-8b02b7acacd4",
                        Name = "Matching UKPRN",
                        Conditions = new[]
                        {
                            new MatchingProfileCondition
                            {
                                SourceAttribute = "Ukprn",
                                CandidateAttribute = "Ukprn",
                            },
                        }
                    },
                }
            };
        }
        
        private MatchingProfile GetLearningProviderManagementGroup()
        {
            return new MatchingProfile
            {
                Id = "4395706b-a583-47dc-92ce-56648c8608f4",
                Name = "Learning provider management group",
                SourceType = EntityNameTranslator.LearningProviderSingular,
                CandidateType = EntityNameTranslator.ManagementGroupSingular,
                LinkType = "managementgroup",
                Rules = new[]
                {
                    new MatchingProfileRuleset
                    {
                        Id = "583f2cdc-1416-4167-9bae-b9ed30058d0a",
                        Name = "Matching ManagementGroupCode",
                        Conditions = new[]
                        {
                            new MatchingProfileCondition
                            {
                                SourceAttribute = "ManagementGroupCode",
                                CandidateAttribute = "ManagementGroupCode",
                            },
                        }
                    },
                }
            };
        }
    }
}