using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Search
{
    public interface ISearchIndex
    {
        Task<SearchIndexResult> SearchAsync(SearchRequest request, string entityType, CancellationToken cancellationToken);
    }
}