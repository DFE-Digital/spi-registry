using Dfe.Spi.Registry.Domain.Entities;

namespace Dfe.Spi.Registry.Domain.Matching
{
    public class EntityForMatching : EntityPointer
    {
        public string Type { get; set; }

        public override string ToString()
        {
            return $"{Type}.{SourceSystemName}.{SourceSystemId}";
        }
    }
}