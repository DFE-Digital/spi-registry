using System.Collections.Generic;

namespace Dfe.Spi.Registry.Domain.Entities
{
    public class EntityPointer
    {
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
    }
    public class Entity : EntityPointer
    {
        public string Type { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public LinkPointer[] Links { get; set; }
    }
}