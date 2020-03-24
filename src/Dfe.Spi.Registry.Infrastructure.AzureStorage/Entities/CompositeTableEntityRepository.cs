using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Entities;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities
{
    public class CompositeTableEntityRepository : IEntityRepository
    {
        private readonly ITableEntityRepository _entityRepository;
        private readonly ITableLinkPointerRepository _linkPointerRepository;

        internal CompositeTableEntityRepository(
            ITableEntityRepository entityRepository,
            ITableLinkPointerRepository linkPointerRepository)
        {
            _entityRepository = entityRepository;
            _linkPointerRepository = linkPointerRepository;
        }
        public CompositeTableEntityRepository(EntitiesConfiguration configuration, ILoggerWrapper logger)
            : this(new TableEntityRepository(configuration), new TableLinkPointerRepository(configuration, logger))
        {
        }
        
        public async Task<Entity> GetEntityAsync(string type, string sourceSystemName, string sourceSystemId, CancellationToken cancellationToken)
        {
            var entityTask = _entityRepository.GetEntityAsync(type, sourceSystemName, sourceSystemId, cancellationToken);
            var linksTask = _linkPointerRepository.GetEntityLinksAsync(type, sourceSystemName, sourceSystemId, cancellationToken);

            await Task.WhenAll(entityTask, linksTask);

            var entity = entityTask.Result;
            if (entity != null)
            {
                entity.Links = linksTask.Result;
            }

            return entity;
        }

        public async Task<Entity[]> GetEntitiesOfTypeAsync(string type, CancellationToken cancellationToken)
        {
            var entitiesTask = _entityRepository.GetEntitiesOfTypeAsync(type, cancellationToken);
            var linksTask = _linkPointerRepository.GetAllLinks(cancellationToken);

            await Task.WhenAll(entitiesTask, linksTask);

            var entities = entitiesTask.Result;
            var links = linksTask.Result;

            foreach (var entity in entities)
            {
                entity.Links = links
                    .Where(l =>
                        l.EntityType == entity.Type &&
                        l.SourceSystemName == entity.SourceSystemName &&
                        l.SourceSystemId == entity.SourceSystemId)
                    .ToArray();
            }

            return entities;
        }

        public async Task StoreAsync(Entity entity, CancellationToken cancellationToken)
        {
            await _entityRepository.StoreAsync(entity, cancellationToken);
            await _linkPointerRepository.StoreEntityLinkPointersAsync(entity, cancellationToken);
        }
    }
}