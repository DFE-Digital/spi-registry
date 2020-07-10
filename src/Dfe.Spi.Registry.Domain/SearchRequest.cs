using System;
using Dfe.Spi.Common.Models;

namespace Dfe.Spi.Registry.Domain
{
    public class SearchRequest
    {
        public DateTime? PointInTime { get; set; }
        public SearchRequestGroup[] Groups { get; set; }
        public string CombinationOperator { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
    }

    public class SearchRequestGroup
    {
        public SearchRequestFilter[] Filter { get; set; }
        public string CombinationOperator { get; set; }
    }

    public class SearchRequestFilter
    {
        public string Field { get; set; }
        public DataOperator Operator { get; set; }
        public string Value { get; set; }
    }
}