using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;

namespace Dfe.Spi.Registry.Application.Entities
{
    public interface IEntityManager
    {
        Task<EntityPointer[]> GetSynonymousEntitiesAsync(string entityType, string sourceSystemName,
            string sourceSystemId, CancellationToken cancellationToken);
    }

    public class EntityManager : IEntityManager
    {
        private readonly IEntityRepository _entityRepository;
        private readonly ILinkRepository _linkRepository;
        private readonly ILoggerWrapper _loggerWrapper;

        public EntityManager(
            IEntityRepository entityRepository,
            ILinkRepository linkRepository,
            ILoggerWrapper loggerWrapper)
        {
            _entityRepository = entityRepository;
            _linkRepository = linkRepository;
            _loggerWrapper = loggerWrapper;
        }

        public async Task<EntityPointer[]> GetSynonymousEntitiesAsync(string entityType, string sourceSystemName,
            string sourceSystemId,
            CancellationToken cancellationToken)
        {
            var sourceEntity =
                await _entityRepository.GetEntityAsync(entityType, sourceSystemName, sourceSystemId, cancellationToken);
            var synonymLinkPointer = sourceEntity?.Links?.SingleOrDefault(l => l.LinkType == "Synonym");
            if (synonymLinkPointer == null)
            {
                _loggerWrapper.Info($"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} does not point to any synonyms");
                return null;
            }
            _loggerWrapper.Info($"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} points to synonym {synonymLinkPointer.LinkId}");

            var link = await _linkRepository.GetLinkAsync(synonymLinkPointer.LinkType, synonymLinkPointer.LinkId,
                cancellationToken);
            var entityPointers = link.LinkedEntities.Select(le => new EntityPointer
            {
                SourceSystemName = le.EntitySourceSystemName,
                SourceSystemId = le.EntitySourceSystemId,
            }).ToArray();
            _loggerWrapper.Info($"Found {entityPointers} entities in the synonym {synonymLinkPointer} (Looked up for {entityType}:{sourceSystemName}:{sourceSystemId})");

            return entityPointers;
        }
    }
}