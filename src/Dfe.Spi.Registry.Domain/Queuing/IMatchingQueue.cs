using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Matching;

namespace Dfe.Spi.Registry.Domain.Queuing
{
    public interface IMatchingQueue
    {
        Task EnqueueAsync(EntityForMatching entity, CancellationToken cancellationToken);
    }
}