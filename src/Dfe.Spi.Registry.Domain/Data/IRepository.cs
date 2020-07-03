using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Data
{
    public interface IRepository
    {
        Task StoreAsync(RegisteredEntity registeredEntity, CancellationToken cancellationToken);
        Task StoreAsync(RegisteredEntity[] registeredEntitiesToUpsert, RegisteredEntity[] registeredEntitiesToDelete, CancellationToken cancellationToken);

        Task<RegisteredEntity> RetrieveAsync(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime,
            CancellationToken cancellationToken);

        Task<RegisteredEntity[]> SearchAsync(SearchRequest request, string entityType, DateTime pointInTime, CancellationToken cancellationToken);
    }
}