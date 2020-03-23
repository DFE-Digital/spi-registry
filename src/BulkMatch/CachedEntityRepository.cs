using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Entities;

namespace BulkMatch
{
    public class CachedEntityRepository : IEntityRepository
    {
        private readonly IEntityRepository _innerRepository;
        private readonly ConcurrentDictionary<string, Entity[]> _cache;

        public CachedEntityRepository(IEntityRepository innerRepository)
        {
            _innerRepository = innerRepository;
            
            _cache = new ConcurrentDictionary<string, Entity[]>();
        }
        
        public async Task<Entity> GetEntityAsync(string type, string sourceSystemName, string sourceSystemId, CancellationToken cancellationToken)
        {
            var entitiesOfType = await LoadAndGetEntitiesOfTypeAsync(type, cancellationToken);
            var match = entitiesOfType.SingleOrDefault(entity =>
                entity.SourceSystemName.Equals(sourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                entity.SourceSystemId.Equals(sourceSystemId, StringComparison.InvariantCultureIgnoreCase));
            return match;
        }

        public async Task<Entity[]> GetEntitiesOfTypeAsync(string type, CancellationToken cancellationToken)
        {
            return await LoadAndGetEntitiesOfTypeAsync(type, cancellationToken);
        }

        public async Task StoreAsync(Entity entity, CancellationToken cancellationToken)
        {
            await _innerRepository.StoreAsync(entity, cancellationToken);
        }


        private async Task<Entity[]> LoadAndGetEntitiesOfTypeAsync(string type, CancellationToken cancellationToken)
        {
            Entity[] entities;
            if (_cache.TryGetValue(type, out entities))
            {
                return entities;
            }
            
            entities = await _innerRepository.GetEntitiesOfTypeAsync(type, cancellationToken);
            _cache.TryAdd(type, entities);
            return entities;
        }
    }
}