using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Matching
{
    public interface IMatchingProfileRepository
    {
        Task<MatchingProfile[]> GetMatchingProfilesAsync(CancellationToken cancellationToken);
    }
}