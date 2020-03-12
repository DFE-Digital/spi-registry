namespace Dfe.Spi.Registry.Domain.Search
{
    public class SearchIndexResult
    {
        public SearchDocument[] Results { get; set; }
        
        public int Skipped { get; set; }
        public int Taken { get; set; }
        public long TotalNumberOfRecords { get; set; }
    }
}