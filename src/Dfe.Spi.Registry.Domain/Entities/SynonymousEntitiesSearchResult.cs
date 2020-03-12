using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Domain.Entities
{
    public class SynonymousEntitiesSearchResult
    {
        public SynonymousEntities[] Results { get; set; }
        
        public int Skipped { get; set; }
        public int Taken { get; set; }
        public long TotalNumberOfRecords { get; set; }
    }
}