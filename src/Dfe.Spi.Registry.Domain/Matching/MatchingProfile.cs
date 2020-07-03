namespace Dfe.Spi.Registry.Domain.Matching
{
    public class MatchingProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SourceType { get; set; }
        public string CandidateType { get; set; }
        public string LinkType { get; set; }
        public MatchingProfileRuleset[] Rules { get; set; }
    }
}