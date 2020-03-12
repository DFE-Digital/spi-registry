using Dfe.Spi.Common.Models;

namespace Dfe.Spi.Registry.Domain.Search
{
    public class SearchGroup
    {
        public DataFilter[] Filter { get; set; }
        public string CombinationOperator { get; set; }
    }
}