using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Queuing;

namespace Dfe.Spi.Registry.Application.Entities
{
    public interface IEntityManager
    {
        Task<EntityPointer[]> GetSynonymousEntitiesAsync(string entityType, string sourceSystemName,
            string sourceSystemId, CancellationToken cancellationToken);
        
        Task<LinkedEntityPointer[]> GetEntityLinksAsync(string entityType, string sourceSystemName,
            string sourceSystemId, CancellationToken cancellationToken);

        Task SyncEntityAsync(Entity entity, CancellationToken cancellationToken);
    }

    public class EntityManager : IEntityManager
    {
        private readonly IEntityRepository _entityRepository;
        private readonly ILinkRepository _linkRepository;
        private readonly IMatchingQueue _matchingQueue;
        private readonly ILoggerWrapper _logger;

        public EntityManager(
            IEntityRepository entityRepository,
            ILinkRepository linkRepository,
            IMatchingQueue matchingQueue,
            ILoggerWrapper logger)
        {
            _entityRepository = entityRepository;
            _linkRepository = linkRepository;
            _matchingQueue = matchingQueue;
            _logger = logger;
        }

        public async Task<EntityPointer[]> GetSynonymousEntitiesAsync(
            string entityType, 
            string sourceSystemName,
            string sourceSystemId,
            CancellationToken cancellationToken)
        {
            var sourceEntity =
                await _entityRepository.GetEntityAsync(entityType, sourceSystemName, sourceSystemId, cancellationToken);
            var synonymLinkPointer = sourceEntity?.Links?.SingleOrDefault(l => l.LinkType == LinkTypes.Synonym);
            if (synonymLinkPointer == null)
            {
                _logger.Info(
                    $"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} does not point to any synonyms");
                return null;
            }

            _logger.Info(
                $"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} points to synonym {synonymLinkPointer.LinkId}");

            var link = await _linkRepository.GetLinkAsync(synonymLinkPointer.LinkType, synonymLinkPointer.LinkId,
                cancellationToken);
            var entityPointers = link.LinkedEntities
                .Where(le =>
                    !(le.EntitySourceSystemName == sourceSystemName && le.EntitySourceSystemId == sourceSystemId))
                .Select(le => new EntityPointer
                {
                    SourceSystemName = le.EntitySourceSystemName,
                    SourceSystemId = le.EntitySourceSystemId,
                })
                .ToArray();
            _logger.Info(
                $"Found {entityPointers} entities in the synonym {synonymLinkPointer} (Looked up for {entityType}:{sourceSystemName}:{sourceSystemId})");

            return entityPointers;
        }

        public async Task<LinkedEntityPointer[]> GetEntityLinksAsync(
            string entityType, 
            string sourceSystemName,
            string sourceSystemId,
            CancellationToken cancellationToken)
        {
            var sourceEntity =
                await _entityRepository.GetEntityAsync(entityType, sourceSystemName, sourceSystemId, cancellationToken);
            var linkPointers = sourceEntity?.Links?.Where(l => l.LinkType != LinkTypes.Synonym)?.ToArray();
            if (linkPointers == null)
            {
                _logger.Info(
                    $"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} does not point to any links");
                return null;
            }

            _logger.Info(
                $"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} points to {linkPointers.Length} links");

            var entityPointers = new List<LinkedEntityPointer>();
            foreach (var linkPointer in linkPointers)
            {
                var link = await _linkRepository.GetLinkAsync(linkPointer.LinkType, linkPointer.LinkId,
                    cancellationToken);
                var linkEntityPointers = link.LinkedEntities
                    .Where(le =>
                        !(le.EntitySourceSystemName == sourceSystemName && le.EntitySourceSystemId == sourceSystemId))
                    .Select(le => new LinkedEntityPointer
                    {
                        SourceSystemName = le.EntitySourceSystemName,
                        SourceSystemId = le.EntitySourceSystemId,
                        LinkType = link.Type,
                    })
                    .ToArray();
                _logger.Info(
                    $"Found {linkEntityPointers} entities in the link {linkPointer} (Looked up for {entityType}:{sourceSystemName}:{sourceSystemId})");
                
                entityPointers.AddRange(linkEntityPointers);
            }

            return entityPointers.Count == 0 ? null : entityPointers.ToArray();
        }

        public async Task SyncEntityAsync(Entity entity, CancellationToken cancellationToken)
        {
            await _entityRepository.StoreAsync(entity, cancellationToken);

            await _matchingQueue.EnqueueAsync(new EntityForMatching
            {
                Type = entity.Type,
                SourceSystemName = entity.SourceSystemName,
                SourceSystemId = entity.SourceSystemId,
            }, cancellationToken);
        }
    }
}