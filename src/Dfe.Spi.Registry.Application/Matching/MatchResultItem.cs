using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Application.Matching
{
    public class MatchResultItem
    {
        public RegisteredEntity RegisteredEntity { get; set; }
        public string MatchReason { get; set; }
    }
}