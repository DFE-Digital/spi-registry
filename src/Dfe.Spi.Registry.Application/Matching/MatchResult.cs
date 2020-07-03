using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Application.Matching
{
    public class MatchResult
    {
        public MatchResultItem[] Synonyms { get; set; }
        // TODO: Handle links
    }

    public class MatchResultItem
    {
        public RegisteredEntity RegisteredEntity { get; set; }
        public string MatchReason { get; set; }
    }
}