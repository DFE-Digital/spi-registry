namespace Dfe.Spi.Registry.Domain.Matching
{
    public class MatchingProfileRuleset
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public MatchingProfileCondition[] Conditions { get; set; }
    }
}