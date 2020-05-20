using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Search;
using Dfe.Spi.Registry.Infrastructure.SqlServer;

namespace Dfe.Spi.Registry.Application.Entities
{
    public class SpikeEntityManager : IEntityManager
    {
        private SqlServerSpikeRepository _repository;

        public SpikeEntityManager()
        {
            _repository = new SqlServerSpikeRepository();
        }
        
        public async Task<EntityPointer[]> GetSynonymousEntitiesAsync(string entityType, string sourceSystemName, string sourceSystemId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public async Task<LinkedEntityPointer[]> GetEntityLinksAsync(string entityType, string sourceSystemName, string sourceSystemId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public async Task SyncEntityAsync(Entity entity, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public async Task<SynonymousEntitiesSearchResult> SearchAsync(SearchRequest criteria, string entityType, CancellationToken cancellationToken)
        {
            return await _repository.SearchAsync(criteria, entityType, cancellationToken);
        }
    }
}