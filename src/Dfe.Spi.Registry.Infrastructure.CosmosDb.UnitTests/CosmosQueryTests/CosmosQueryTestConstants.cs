using System;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    internal static class CosmosQueryTestConstants
    {
        internal const string QueryPrefix = "SELECT * FROM re";
        internal const string CountQueryPrefix = "SELECT COUNT(1) FROM re";
        internal const string OrderBy = "ORDER BY re.id";

        internal static string QueryWithoutOrderByOrSkip(CosmosQuery query)
        {
            var queryText = query.ToString();
            
            var orderByIndex = queryText.IndexOf(OrderBy, StringComparison.InvariantCultureIgnoreCase);
            if (orderByIndex > -1)
            {
                queryText = queryText.Substring(0, orderByIndex).Trim();
            }

            return queryText;
        }

        internal static string QueryWhereClause(CosmosQuery query)
        {
            var queryText = QueryWithoutOrderByOrSkip(query);

            var whereIndex = queryText.IndexOf("WHERE", StringComparison.InvariantCultureIgnoreCase);
            return queryText.Substring(whereIndex + 5).Trim();
        }
    }
}