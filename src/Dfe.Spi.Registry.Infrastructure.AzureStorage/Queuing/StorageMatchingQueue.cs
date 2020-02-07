using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Queuing;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Queuing
{
    public class StorageMatchingQueue : StorageQueue, IMatchingQueue
    {
        public StorageMatchingQueue(QueueConfiguration configuration)
            : base(configuration.StorageQueueConnectionString, QueueNames.Matching)
        {
        }
        
        public async Task EnqueueAsync(EntityForMatching entity, CancellationToken cancellationToken)
        {
            await EnqueueItemAsync(entity, cancellationToken);
        }
    }
}