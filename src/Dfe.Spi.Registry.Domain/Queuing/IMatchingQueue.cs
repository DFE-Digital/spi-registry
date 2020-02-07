using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Queuing
{
    public interface IMatchingQueue
    {
        Task EnqueueAsync(EntityForMatching entity, CancellationToken cancellationToken);
    }
}