namespace Dfe.Spi.Registry.Domain.Matching
{
    public class MatchingProfileCondition
    {
        public string SourceAttribute { get; set; }
        public string CandidateAttribute { get; set; }
        public bool MatchNulls { get; set; } = false;
    }
}