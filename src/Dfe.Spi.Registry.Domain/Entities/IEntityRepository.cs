using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.Registry.Domain.Entities
{
    public interface IEntityRepository
    {
        Task<Entity> GetEntityAsync(string type, string sourceSystemName, string sourceSystemId,
            CancellationToken cancellationToken);

        Task StoreAsync(Entity entity, CancellationToken cancellationToken);
    }
}