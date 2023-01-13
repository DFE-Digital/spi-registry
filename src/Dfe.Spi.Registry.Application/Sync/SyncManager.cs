using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Sync;

namespace Dfe.Spi.Registry.Application.Sync
{
    public interface ISyncManager
    {
        Task ReceiveSyncEntityAsync<T>(SyncEntityEvent<T> @event, string sourceSystemName, CancellationToken cancellationToken)
            where T : Models.Entities.EntityBase;

        Task ProcessSyncQueueItemAsync(SyncQueueItem queueItem, CancellationToken cancellationToken);
    }

    public class SyncManager : ISyncManager
    {
        private readonly ISyncQueue _syncQueue;
        private readonly IRepository _repository;
        private readonly IMatcher _matcher;
        private readonly ILoggerWrapper _logger;
        private readonly ISpiExecutionContextManager _executionContextManager;

        public SyncManager(
            ISyncQueue syncQueue,
            IRepository repository,
            IMatcher matcher,
            ILoggerWrapper logger,
            ISpiExecutionContextManager executionContextManager)
        {
            _syncQueue = syncQueue;
            _repository = repository;
            _matcher = matcher;
            _logger = logger;
            _executionContextManager = executionContextManager;
        }

        public async Task ReceiveSyncEntityAsync<T>(SyncEntityEvent<T> @event, string sourceSystemName, CancellationToken cancellationToken)
            where T : EntityBase
        {
            var entity = MapEventToEntity(@event, sourceSystemName);

            var queueItem = new SyncQueueItem
            {
                Entity = entity,
                PointInTime = @event.PointInTime,
                InternalRequestId = _executionContextManager.SpiExecutionContext.InternalRequestId,
                ExternalRequestId = _executionContextManager.SpiExecutionContext.ExternalRequestId,
            };

            var messageId = await _syncQueue.EnqueueEntityForSyncAsync(queueItem, cancellationToken);
            _logger.Info($"Queued item with message id {messageId}");
        }

        public async Task ProcessSyncQueueItemAsync(SyncQueueItem queueItem, CancellationToken cancellationToken)
        {
            _logger.Debug($"Trying to find existing entity for {queueItem.Entity} at {queueItem.PointInTime}");
            var existingEntity = await _repository.RetrieveAsync(
                queueItem.Entity.EntityType,
                queueItem.Entity.SourceSystemName,
                queueItem.Entity.SourceSystemId,
                queueItem.PointInTime,
                cancellationToken);

            var matchResult = await _matcher.MatchAsync(queueItem.Entity, queueItem.PointInTime, cancellationToken);
            _logger.Info($"Matching found {matchResult.Synonyms?.Length} synonyms and {matchResult.Links?.Length} links");

            var registeredEntity = GetRegisteredEntityForPointInTime(queueItem.Entity, queueItem.PointInTime, matchResult);

            await ProcessEntityChangesAsync(existingEntity, registeredEntity, matchResult, cancellationToken);
            _logger.Info($"Finished processing entity {queueItem.Entity} at {queueItem.PointInTime}");
        }

        private Entity MapEventToEntity<T>(SyncEntityEvent<T> @event, string sourceSystemName) where T : EntityBase
        {
            if (@event.Details is LearningProvider learningProvider)
            {
                var sourceSystemId = sourceSystemName.Equals(SourceSystemNames.UkRegisterOfLearningProviders, StringComparison.InvariantCultureIgnoreCase)
                    ? learningProvider.Ukprn?.ToString()
                    : learningProvider.Urn?.ToString();
                return new Entity
                {
                    EntityType = EntityNameTranslator.LearningProviderSingular,
                    SourceSystemName = sourceSystemName.ToUpper(),
                    SourceSystemId = sourceSystemId,
                    Name = learningProvider.Name,
                    Type = learningProvider.Type,
                    SubType = learningProvider.SubType,
                    Status = learningProvider.Status,
                    OpenDate = learningProvider.OpenDate,
                    CloseDate = learningProvider.CloseDate,
                    Urn = learningProvider.Urn,
                    Ukprn = learningProvider.Ukprn,
                    Uprn = learningProvider.Uprn,
                    CompaniesHouseNumber = learningProvider.CompaniesHouseNumber,
                    CharitiesCommissionNumber = learningProvider.CharitiesCommissionNumber,
                    AcademyTrustCode = learningProvider.AcademyTrustCode,
                    DfeNumber = learningProvider.DfeNumber,
                    LocalAuthorityCode = learningProvider.LocalAuthorityCode,
                    ManagementGroupType = learningProvider.ManagementGroup?.Type,
                    ManagementGroupId = learningProvider.ManagementGroup?.Identifier,
                    ManagementGroupCode = learningProvider.ManagementGroup?.Code,
                    ManagementGroupUkprn = learningProvider.ManagementGroup?.Ukprn,
                    ManagementGroupCompaniesHouseNumber = learningProvider.ManagementGroup?.CompaniesHouseNumber,
                };
            }

            if (@event.Details is ManagementGroup managementGroup)
            {
                return new Entity
                {
                    EntityType = EntityNameTranslator.ManagementGroupSingular,
                    SourceSystemName = sourceSystemName,
                    SourceSystemId = managementGroup.Code,
                    ManagementGroupType = managementGroup.Type,
                    ManagementGroupId = managementGroup.Identifier,
                    ManagementGroupCode = managementGroup.Code,
                    ManagementGroupUkprn = managementGroup.Ukprn,
                    ManagementGroupCompaniesHouseNumber = managementGroup.CompaniesHouseNumber,
                };
            }

            throw new Exception($"Unprocessable event for type {typeof(T)}");
        }

        private LinkedEntity MapEntityToLinkedEntity(Entity entity)
        {
            return new LinkedEntity
            {
                EntityType = entity.EntityType,
                SourceSystemName = entity.SourceSystemName,
                SourceSystemId = entity.SourceSystemId,
                Name = entity.Name,
                Type = entity.Type,
                SubType = entity.SubType,
                Status = entity.Status,
                OpenDate = entity.OpenDate,
                CloseDate = entity.CloseDate,
                Urn = entity.Urn,
                Ukprn = entity.Ukprn,
                Uprn = entity.Uprn,
                CompaniesHouseNumber = entity.CompaniesHouseNumber,
                CharitiesCommissionNumber = entity.CharitiesCommissionNumber,
                AcademyTrustCode = entity.AcademyTrustCode,
                DfeNumber = entity.DfeNumber,
                LocalAuthorityCode = entity.LocalAuthorityCode,
                ManagementGroupType = entity.ManagementGroupType,
                ManagementGroupId = entity.ManagementGroupId,
                ManagementGroupCode = entity.ManagementGroupCode,
                ManagementGroupUkprn = entity.ManagementGroupUkprn,
                ManagementGroupCompaniesHouseNumber = entity.ManagementGroupCompaniesHouseNumber,
            };
        }

        private RegisteredEntity GetRegisteredEntityForPointInTime(Entity entity, DateTime pointInTime, MatchResult matchResult)
        {
            _logger.Debug($"Preparing updated version of {entity} at {pointInTime}");

            // Get entities
            var entities = new List<LinkedEntity>(new[] { MapEntityToLinkedEntity(entity) });
            if (matchResult.Synonyms.Length > 0)
            {
                entities[0].LinkedAt = DateTime.UtcNow;
                entities[0].LinkedBy = "Matcher";
                entities[0].LinkedReason = matchResult.Synonyms[0].MatchReason;
                entities[0].LinkType = "synonym";
            }

            foreach (var synonym in matchResult.Synonyms)
            {
                foreach (var synonymousEntity in synonym.RegisteredEntity.Entities)
                {
                    if (!entities.Any(x =>
                        x.SourceSystemName.Equals(synonymousEntity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                        x.SourceSystemId.Equals(synonymousEntity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var cloned = MapEntityToLinkedEntity(synonymousEntity);
                        cloned.LinkedAt = synonymousEntity.LinkedAt ?? DateTime.UtcNow;
                        cloned.LinkedBy = synonymousEntity.LinkedBy ?? "Matcher";
                        cloned.LinkedReason = synonymousEntity.LinkedReason ?? synonym.MatchReason;
                        cloned.LinkType = "synonym";
                        entities.Add(cloned);
                    }
                }
            }

            // Get links
            var links = new List<Link>();
            foreach (var link in matchResult.Links)
            {
                links.Add(new Link
                {
                    EntityType = link.Entity.EntityType,
                    SourceSystemName = link.Entity.SourceSystemName,
                    SourceSystemId = link.Entity.SourceSystemId,
                    LinkedAt = DateTime.UtcNow,
                    LinkedBy = "Matcher",
                    LinkedReason = link.MatchReason,
                    LinkType = link.LinkType,
                });
            }

            // Put it together
            return new RegisteredEntity
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Type = entity.EntityType,
                ValidFrom = pointInTime,
                Entities = entities.ToArray(),
                Links = links.ToArray(),
            };
        }

        private async Task ProcessEntityChangesAsync(RegisteredEntity existingEntity, RegisteredEntity updatedEntity, MatchResult matchResult,
            CancellationToken cancellationToken)
        {
            var updates = new List<RegisteredEntity>();
            var deletes = new List<RegisteredEntity>();

            // Check if entity has changed
            if (existingEntity == null)
            {
                _logger.Info(
                    $"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) has not been seen before {updatedEntity.ValidFrom}. Adding entry to be created");
                updates.Add(updatedEntity);
            }
            else if (!AreSame(existingEntity, updatedEntity))
            {
                _logger.Info(
                    $"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) on {updatedEntity.ValidFrom} has changed since {existingEntity.ValidFrom} ({existingEntity.Id}). Adding entry to be updated");

                updates.Add(updatedEntity);

                if (existingEntity.ValidFrom == updatedEntity.ValidFrom)
                {
                    _logger.Info($"Updated entity {updatedEntity.Id} has same valid from as existing entity {existingEntity.Id}. Deleting existing entity");
                    deletes.Add(existingEntity);
                }
                else
                {
                    _logger.Info($"Setting existing entity {existingEntity.Id} ValidTo to {updatedEntity.ValidFrom}");
                    existingEntity.ValidTo = updatedEntity.ValidFrom;
                    updates.Add(existingEntity);
                }
            }
            else
            {
                _logger.Info($"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) on {updatedEntity.ValidFrom} has not changed since " +
                             $"{existingEntity.ValidFrom} ({existingEntity.Id}). No update being made");
                return;
            }

            // Check if any synonyms of original entity are now unlinked
            if (existingEntity != null)
            {
                foreach (var existingSynonym in existingEntity.Entities)
                {
                    var isLinked = updates.Any(update =>
                        !update.ValidTo.HasValue &&
                        update.Entities.Any(updateSynonym =>
                            existingSynonym.SourceSystemId.Equals(updateSynonym.SourceSystemId, StringComparison.InvariantCultureIgnoreCase) &&
                            existingSynonym.SourceSystemName.Equals(updateSynonym.SourceSystemName, StringComparison.InvariantCultureIgnoreCase)));
                    if (isLinked)
                    {
                        _logger.Debug($"Existing synonym {existingSynonym} has been re-linked. No further action required");
                        continue;
                    }

                    _logger.Info($"Existing synonym {existingSynonym} is no longer linked. Re-matching for update");

                    var unlinkedSynonymMatchResult = await _matcher.MatchAsync(existingSynonym, updatedEntity.ValidFrom, cancellationToken);
                    _logger.Info($"Matching found {matchResult.Synonyms?.Length} synonyms and {matchResult.Links?.Length} links " +
                                 $"for unlinked synonym {existingSynonym}");

                    var relinkedEntity = GetRegisteredEntityForPointInTime(existingSynonym, updatedEntity.ValidFrom, unlinkedSynonymMatchResult);
                    _logger.Info($"Entity {existingSynonym} is being updated as {relinkedEntity.Id}");
                    updates.Add(relinkedEntity);
                }
            }

            // Make sure all synonyms are correctly updated with valid to
            foreach (var synonym in matchResult.Synonyms)
            {
                if (synonym.RegisteredEntity.ValidFrom == updatedEntity.ValidFrom)
                {
                    _logger.Info($"Adding {synonym.RegisteredEntity.Id} to be deleted");
                    deletes.Add(synonym.RegisteredEntity);
                }
                else if (!updates.Any(x => x.Id == synonym.RegisteredEntity.Id))
                {
                    _logger.Info($"Setting ValidTo of {synonym.RegisteredEntity.Id} to {updatedEntity.ValidFrom}");
                    synonym.RegisteredEntity.ValidTo = updatedEntity.ValidFrom;
                    updates.Add(synonym.RegisteredEntity);
                }
            }

            // Make sure all links are updated with link to this entity
            var linksToUpdate = matchResult.Links
                .Where(link => !link.LinkFromSynonym &&
                               !AreAlreadyLinked(updatedEntity.Entities[0], link.LinkType, link.RegisteredEntity))
                .ToArray();

            _logger.Info($"Links to update {linksToUpdate.Length}");

            foreach (var link in linksToUpdate)
            {
                _logger.Info($"Link to update {link.Entity.SourceSystemId}, {link.Entity.SourceSystemName}, {link.Entity.EntityType}");

                var linkFromUpdate = updatedEntity.Links.FirstOrDefault(updateLink =>
                    string.Equals(updateLink.EntityType,link.Entity.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(updateLink.SourceSystemName,link.Entity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(updateLink.SourceSystemId,link.Entity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase));
                var newLink = linkFromUpdate != null ? new Link
                {
                    EntityType = updatedEntity.Entities[0].EntityType,
                    SourceSystemName = updatedEntity.Entities[0].SourceSystemName,
                    SourceSystemId = updatedEntity.Entities[0].SourceSystemId,
                    LinkedAt = linkFromUpdate.LinkedAt,
                    LinkedBy = linkFromUpdate.LinkedBy,
                    LinkedReason = linkFromUpdate.LinkedReason,
                    LinkType = linkFromUpdate.LinkType,
                } : null;

                var updatedLinkedEntity = CloneWithNewLink(link.RegisteredEntity, newLink, updatedEntity.ValidFrom);
                updates.Add(updatedLinkedEntity);

                if (link.RegisteredEntity.ValidFrom == updatedEntity.ValidFrom)
                {
                    _logger.Info($"Adding {link.RegisteredEntity.Id} to be deleted");
                    deletes.Add(link.RegisteredEntity);
                }
                else
                {
                    _logger.Info($"Setting ValidTo of {link.RegisteredEntity.Id} to {updatedEntity.ValidFrom}");
                    link.RegisteredEntity.ValidTo = updatedEntity.ValidFrom;
                    updates.Add(link.RegisteredEntity);
                }
            }

            // Write changes to store
            var updateIds = updates.Count > 0 ? updates.Select(x => x.Id).Aggregate((x, y) => $"{x}, {y}") : string.Empty;
            var deleteIds = deletes.Count > 0 ? deletes.Select(x => x.Id).Aggregate((x, y) => $"{x}, {y}") : string.Empty;
            _logger.Debug($"Storing {updates.Count} updates and {deletes.Count} deletes in repository." +
                          $"\nUpdate document ids: {updateIds}" +
                          $"\nDelete document ids: {deleteIds}");
            await _repository.StoreAsync(updates.ToArray(), deletes.ToArray(), cancellationToken);
        }

        private RegisteredEntity CloneWithNewLink(RegisteredEntity registeredEntity, Link newLink, DateTime validFrom)
        {
            return new RegisteredEntity
            {
                Id = Guid.NewGuid().ToString(),
                Type = registeredEntity.Type,
                ValidFrom = validFrom,
                Entities = registeredEntity.Entities,
                Links = newLink != null ? registeredEntity.Links.Concat(
                    new[]
                    {
                        newLink
                    }).ToArray() : registeredEntity.Links,
            };
        }

        private bool AreAlreadyLinked(Entity sourceEntity, string linkType, RegisteredEntity entityBeingLinkedTo)
        {
            return entityBeingLinkedTo.Links.Any(link => string.Equals(link.LinkType,linkType, StringComparison.InvariantCultureIgnoreCase) &&
                                                         string.Equals(link.EntityType,sourceEntity.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                                                         string.Equals(link.SourceSystemName,sourceEntity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                                                         string.Equals(link.SourceSystemId,sourceEntity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase));
        }

        private bool AreSame(RegisteredEntity registeredEntity1, RegisteredEntity registeredEntity2)
        {
            // Compare entities
            if (registeredEntity1.Entities.Length != registeredEntity2.Entities.Length)
            {
                _logger.Debug($"Entity {registeredEntity1.Id} and {registeredEntity2.Id} have a different number of entities");
                return false;
            }

            foreach (var entity1 in registeredEntity1.Entities)
            {
                var entity2 = registeredEntity2.Entities.FirstOrDefault(e2 =>
                    string.Equals(e2.SourceSystemName,entity1.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(e2.SourceSystemId,entity1.SourceSystemId, StringComparison.InvariantCultureIgnoreCase));

                if (entity2 == null)
                {
                    _logger.Debug($"Entity {registeredEntity2.Id} is missing {entity1}");
                    return false;
                }

                if (!AreSame(entity1, entity2))
                {
                    _logger.Debug($"Entity {registeredEntity1.Id} and {registeredEntity2.Id} have different versions of {entity1}");
                    return false;
                }
            }

            // Compare links
            if ((registeredEntity1.Links?.Length ?? 0) != (registeredEntity2.Links?.Length ?? 0))
            {
                _logger.Debug($"Entity {registeredEntity1.Id} and {registeredEntity2.Id} have a different number of links");
                return false;
            }

            foreach (var link1 in registeredEntity1.Links ?? new Link[] { })
            {
                var link2 = registeredEntity2.Links.FirstOrDefault(l2 =>
                    string.Equals(l2.EntityType,link1.EntityType, StringComparison.InvariantCultureIgnoreCase) &&
                                  string.Equals(l2.SourceSystemName,link1.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                                                string.Equals(l2.SourceSystemId,link1.SourceSystemId, StringComparison.InvariantCultureIgnoreCase) &&
                                                              string.Equals(l2.LinkType,link1.LinkType, StringComparison.InvariantCultureIgnoreCase));

                if (link2 == null)
                {
                    _logger.Debug($"Entity {registeredEntity2.Id} is missing link {link1}");
                    return false;
                }
            }

            // They are the same
            return true;
        }

        private bool AreSame(Entity entity1, Entity entity2)
        {
            return string.Equals(entity1.Name, entity2.Name, StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(entity1.Type, entity2.Type, StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(entity1.SubType, entity2.SubType, StringComparison.InvariantCultureIgnoreCase) &&
                   string.Equals(entity1.Status, entity2.Status, StringComparison.InvariantCultureIgnoreCase) &&
                   entity1.OpenDate == entity2.OpenDate &&
                   entity1.CloseDate == entity2.CloseDate &&
                   entity1.Urn == entity2.Urn &&
                   entity1.Ukprn == entity2.Ukprn &&
                   string.Equals(entity1.Uprn, entity2.Uprn, StringComparison.InvariantCultureIgnoreCase) &&
                     entity1.CompaniesHouseNumber == entity2.CompaniesHouseNumber &&
                     entity1.CharitiesCommissionNumber == entity2.CharitiesCommissionNumber &&
                     entity1.AcademyTrustCode == entity2.AcademyTrustCode &&
                     entity1.DfeNumber == entity2.DfeNumber &&
                     entity1.LocalAuthorityCode == entity2.LocalAuthorityCode &&
                     entity1.ManagementGroupType == entity2.ManagementGroupType &&
                     entity1.ManagementGroupId == entity2.ManagementGroupId &&
                     entity1.ManagementGroupCode == entity2.ManagementGroupCode &&
                     entity1.ManagementGroupUkprn == entity2.ManagementGroupUkprn &&
                     entity1.ManagementGroupCompaniesHouseNumber == entity2.ManagementGroupCompaniesHouseNumber;
        }

    }
}