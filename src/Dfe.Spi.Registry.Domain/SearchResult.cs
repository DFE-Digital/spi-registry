namespace Dfe.Spi.Registry.Domain
{
    public class SearchResult
    {
        public RegisteredEntity[] Results { get; set; }
        public long TotalNumberOfRecords { get; set; }
    }
}