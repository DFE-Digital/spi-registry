using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Data
{
    public interface IRepository
    {
        Task StoreAsync(RegisteredEntity registeredEntity, CancellationToken cancellationToken);
    }
}