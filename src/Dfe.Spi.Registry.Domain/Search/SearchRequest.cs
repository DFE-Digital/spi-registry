namespace Dfe.Spi.Registry.Domain.Search
{
    public class SearchRequest
    {
        public SearchGroup[] Groups { get; set; }
        public string CombinationOperator { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }
}