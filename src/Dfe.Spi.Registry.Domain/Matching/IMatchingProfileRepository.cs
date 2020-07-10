using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Matching
{
    public interface IMatchingProfileRepository
    {
        Task<MatchingProfile[]> GetMatchingProfilesForEntityTypeAsync(string entityType, CancellationToken cancellationToken);
    }
}