using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Search
{
    public interface ISearchIndex
    {
        Task<SearchIndexResult> SearchAsync(SearchRequest request, string entityType, CancellationToken cancellationToken);
        Task AddOrUpdateAsync(SearchDocument document, CancellationToken cancellationToken);
        Task AddOrUpdateBatchAsync(SearchDocument[] documents, CancellationToken cancellationToken);
        Task DeleteAsync(SearchDocument document, CancellationToken cancellationToken);
        Task DeleteBatchAsync(SearchDocument[] documents, CancellationToken cancellationToken);
    }
}