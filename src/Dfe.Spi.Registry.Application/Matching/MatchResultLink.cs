using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Application.Matching
{
    public class MatchResultLink
    {
        public RegisteredEntity RegisteredEntity { get; set; }
        public Entity Entity { get; set; }
        public string LinkType { get; set; }
        public string MatchReason { get; set; }
    }
}