using System.Collections.Generic;

namespace Dfe.Spi.Registry.Domain.Entities
{
    public class Entity : EntityPointer
    {
        public string Type { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public LinkPointer[] Links { get; set; }

        public override string ToString()
        {
            return $"{Type}.{SourceSystemName}.{SourceSystemId}";
        }
    }
}