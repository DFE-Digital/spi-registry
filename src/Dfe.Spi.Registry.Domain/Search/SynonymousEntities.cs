using System.Collections.Generic;
using Dfe.Spi.Registry.Domain.Entities;

namespace Dfe.Spi.Registry.Domain.Search
{
    public class SynonymousEntities
    {
        public EntityPointer[] Entities { get; set; }
        public Dictionary<string, string> IndexedData { get; set; }
    }
}