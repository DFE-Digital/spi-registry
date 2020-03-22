using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Application
{
    public static class SearchIndexExtensions
    {
        public static async Task<SearchIndexResult> SearchUsingSingleCriteriaAsync(this ISearchIndex searchIndex,
            string field, DataOperator @operator, string value, string entityType, CancellationToken cancellationToken)
        {
            return await searchIndex.SearchAsync(
                new SearchRequest
                {
                    Groups = new[]
                    {
                        new SearchGroup
                        {
                            Filter = new[]
                            {
                                new DataFilter
                                {
                                    Field = field,
                                    Operator = @operator,
                                    Value = value,
                                },
                            },
                            CombinationOperator = "and",
                        },
                    },
                    CombinationOperator = "and",
                    Take = 100,
                },
                entityType,
                cancellationToken);
        }
    }
}