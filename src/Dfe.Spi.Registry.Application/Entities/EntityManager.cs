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
using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Application.Entities
{
    public interface IEntityManager
    {
        Task<EntityPointer[]> GetSynonymousEntitiesAsync(string entityType, string sourceSystemName,
            string sourceSystemId, CancellationToken cancellationToken);

        Task<LinkedEntityPointer[]> GetEntityLinksAsync(string entityType, string sourceSystemName,
            string sourceSystemId, CancellationToken cancellationToken);

        Task SyncEntityAsync(Entity entity, CancellationToken cancellationToken);

        Task<SynonymousEntitiesSearchResult> SearchAsync(SearchRequest criteria, string entityType,
            CancellationToken cancellationToken);
    }

    public class EntityManager : IEntityManager
    {
        private readonly IEntityRepository _entityRepository;
        private readonly ILinkRepository _linkRepository;
        private readonly IMatchingQueue _matchingQueue;
        private readonly ISearchIndex _searchIndex;
        private readonly ILoggerWrapper _logger;

        public EntityManager(
            IEntityRepository entityRepository,
            ILinkRepository linkRepository,
            IMatchingQueue matchingQueue,
            ISearchIndex searchIndex,
            ILoggerWrapper logger)
        {
            _entityRepository = entityRepository;
            _linkRepository = linkRepository;
            _matchingQueue = matchingQueue;
            _searchIndex = searchIndex;
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
                _logger.Debug(
                    $"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} does not point to any synonyms");
                return null;
            }

            _logger.Debug(
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
                _logger.Debug(
                    $"Source entity {entityType}:{sourceSystemName}:{sourceSystemId} does not point to any links");
                return null;
            }

            _logger.Debug(
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
            var entityToStore = entity;
            var existing = await _entityRepository.GetEntityAsync(entity.Type,
                entity.SourceSystemName, entity.SourceSystemId, cancellationToken);

            if (existing != null)
            {
                existing.Data = entity.Data;
                entityToStore = existing;
            }
            
            await _entityRepository.StoreAsync(entityToStore, cancellationToken);
            
            if (entityToStore.Links == null || !entityToStore.Links.Any(l => l.LinkType == LinkTypes.Synonym))
            {
                var document = entityToStore.ToSearchDocument();
                await _searchIndex.AddOrUpdateAsync(document, cancellationToken);
            }

            await _matchingQueue.EnqueueAsync(new EntityForMatching
            {
                Type = entityToStore.Type,
                SourceSystemName = entityToStore.SourceSystemName,
                SourceSystemId = entityToStore.SourceSystemId,
            }, cancellationToken);
        }

        public async Task<SynonymousEntitiesSearchResult> SearchAsync(SearchRequest criteria, string entityType,
            CancellationToken cancellationToken)
        {
            var searchResults = await _searchIndex.SearchAsync(criteria, entityType, cancellationToken);

            var results = new List<SynonymousEntities>();
            foreach (var searchDocument in searchResults.Results)
            {
                var referenceParts = searchDocument.ReferencePointer.Split(':');
                if (referenceParts[0] == "entity")
                {
                    results.Add(new SynonymousEntities
                    {
                        Entities = new[]
                        {
                            new EntityPointer
                            {
                                SourceSystemName = referenceParts[2],
                                SourceSystemId = referenceParts[3],
                            }, 
                        }
                    });
                }
                else if (referenceParts[0] == "link")
                {
                    var link = await _linkRepository.GetLinkAsync(referenceParts[1], referenceParts[2],
                        cancellationToken);
                    results.Add(new SynonymousEntities
                    {
                        Entities = link.LinkedEntities.Select(linkedEntity =>
                            new EntityPointer
                            {
                                SourceSystemName = linkedEntity.EntitySourceSystemName,
                                SourceSystemId = linkedEntity.EntitySourceSystemId,
                            }).ToArray(),
                    });
                }
                else
                {
                    throw new Exception(
                        $"Search result had unexpected reference pointer '{searchDocument.ReferencePointer}'");
                }
            }
            
            return new SynonymousEntitiesSearchResult
            {
                Results = results.ToArray(),
                Skipped = searchResults.Skipped,
                Taken = searchResults.Taken,
                TotalNumberOfRecords = searchResults.TotalNumberOfRecords
            };
        }
    }
}