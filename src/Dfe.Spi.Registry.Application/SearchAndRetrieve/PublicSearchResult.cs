using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Application.SearchAndRetrieve
{
    public class PublicSearchResult
    {
        public PublicSearchResultItem[] Results { get; set; }
        public int Skipped { get; set; }
        public int Taken { get; set; }
        public long TotalNumberOfRecords { get; set; }
    }

    public class PublicSearchResultItem
    {
        public EntityPointer[] Entities { get; set; }
        public Entity IndexedData { get; set; }
    }
}