using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Links
{
    public interface ILinkRepository
    {
        Task<Link> GetLinkAsync(string type, string id, CancellationToken cancellationToken);

        Task StoreAsync(Link link, CancellationToken cancellationToken);
    }
}