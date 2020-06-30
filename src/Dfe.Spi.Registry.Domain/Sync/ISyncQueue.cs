using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Sync
{
    public interface ISyncQueue
    {
        Task EnqueueEntityForSyncAsync(SyncQueueItem queueItem, CancellationToken cancellationToken);
    }
}