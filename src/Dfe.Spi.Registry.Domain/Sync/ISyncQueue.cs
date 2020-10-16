using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Sync
{
    public interface ISyncQueue
    {
        Task<string> EnqueueEntityForSyncAsync(SyncQueueItem queueItem, CancellationToken cancellationToken);
    }
}