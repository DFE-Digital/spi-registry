namespace Dfe.Spi.Registry.Domain.Matching
{
    public class MatchingProfile
    {
        public string Name { get; set; }
        public string SourceType { get; set; }
        public string CandidateType { get; set; }
        public string LinkType { get; set; }
        public MatchingRuleset[] Rules { get; set; }
    }

    public class MatchingRuleset
    {
        public string Name { get; set; }
        public MatchingCriteria[] Criteria { get; set; }
    }

    public class MatchingCriteria
    {
        public string SourceAttribute { get; set; }
        public string CandidateAttribute { get; set; }
    }
}