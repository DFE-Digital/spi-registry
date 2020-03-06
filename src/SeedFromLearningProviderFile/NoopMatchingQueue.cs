using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Queuing;

namespace SeedFromLearningProviderFile
{
    public class NoopMatchingQueue : IMatchingQueue
    {
        public Task EnqueueAsync(EntityForMatching entity, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}