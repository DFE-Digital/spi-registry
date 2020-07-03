using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public SyncManager(
            ISyncQueue syncQueue,
            IRepository repository,
            IMatcher matcher,
            ILoggerWrapper logger)
        {
            _syncQueue = syncQueue;
            _repository = repository;
            _matcher = matcher;
            _logger = logger;
        }

        public async Task ReceiveSyncEntityAsync<T>(SyncEntityEvent<T> @event, string sourceSystemName, CancellationToken cancellationToken)
            where T : EntityBase
        {
            var entity = MapEventToEntity(@event, sourceSystemName);
            var queueItem = new SyncQueueItem
            {
                Entity = entity,
                PointInTime = @event.PointInTime,
            };

            await _syncQueue.EnqueueEntityForSyncAsync(queueItem, cancellationToken);
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
                    SourceSystemName = sourceSystemName,
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
                ManagementGroupUkprn = entity.ManagementGroupUkprn,
                ManagementGroupCompaniesHouseNumber = entity.ManagementGroupCompaniesHouseNumber,
            };
        }

        private RegisteredEntity GetRegisteredEntityForPointInTime(Entity entity, DateTime pointInTime, MatchResult matchResult)
        {
            _logger.Debug($"Preparing updated version of {entity} at {pointInTime}");
            var entities = new List<LinkedEntity>(new[] {MapEntityToLinkedEntity(entity)});
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
                    if (!entities.Any(x => x.SourceSystemName == synonymousEntity.SourceSystemName && x.SourceSystemId == synonymousEntity.SourceSystemId))
                    {
                        synonymousEntity.LinkedAt = synonymousEntity.LinkedAt ?? DateTime.UtcNow;
                        synonymousEntity.LinkedBy = synonymousEntity.LinkedBy ?? "Matcher";
                        synonymousEntity.LinkedReason = synonymousEntity.LinkedReason ?? synonym.MatchReason;
                        synonymousEntity.LinkType = "synonym";
                        entities.Add(synonymousEntity);
                    }
                }
            }

            return new RegisteredEntity
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Type = entity.EntityType,
                ValidFrom = pointInTime,
                Entities = entities.ToArray(),
                Links = new Link[0],
            };
        }

        private async Task ProcessEntityChangesAsync(RegisteredEntity existingEntity, RegisteredEntity updatedEntity, MatchResult matchResult, CancellationToken cancellationToken)
        {
            var updates = new List<RegisteredEntity>();
            var deletes = new List<RegisteredEntity>();

            if (existingEntity == null)
            {
                _logger.Info($"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) has not been seen before {updatedEntity.ValidFrom}. Adding entry to be created");
                updates.Add(updatedEntity);
            }
            else if (!AreSame(existingEntity, updatedEntity))
            {
                _logger.Info($"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) on {updatedEntity.ValidFrom} has changed since {existingEntity.ValidFrom}. Adding entry to be updated");
                
                updates.Add(updatedEntity);

                if (existingEntity.ValidFrom == updatedEntity.ValidFrom)
                {
                    deletes.Add(existingEntity);
                }
                else
                {
                    existingEntity.ValidTo = updatedEntity.ValidFrom;
                    updates.Add(existingEntity);
                }
            }
            
            if (updates.Count > 0)
            {
                if (matchResult.Synonyms.Length > 0)
                {
                    foreach (var synonym in matchResult.Synonyms)
                    {
                        if (synonym.RegisteredEntity.ValidFrom == updatedEntity.ValidFrom)
                        {
                            _logger.Info($"Adding {synonym.RegisteredEntity.Id} to be deleted");
                            deletes.Add(synonym.RegisteredEntity);
                        }
                        else
                        {
                            _logger.Info($"Setting ValidTo of {synonym.RegisteredEntity.Id} to {updatedEntity.ValidFrom}");
                            synonym.RegisteredEntity.ValidTo = updatedEntity.ValidFrom;
                            updates.Add(synonym.RegisteredEntity);
                        }
                    }
                }
                
                _logger.Debug($"Storing {updates.Count} updated and {deletes.Count} deletes in repository");
                await _repository.StoreAsync(updates.ToArray(), deletes.ToArray(), cancellationToken);
            }
        }

        private bool AreSame(RegisteredEntity registeredEntity1, RegisteredEntity registeredEntity2)
        {
            if (registeredEntity1.Entities.Length != registeredEntity2.Entities.Length)
            {
                _logger.Debug($"Entity {registeredEntity1.Id} and {registeredEntity2.Id} have a different number of entities");
                return false;
            }

            foreach (var entity1 in registeredEntity1.Entities)
            {
                var entity2 = registeredEntity2.Entities.SingleOrDefault(e2 =>
                    e2.SourceSystemName == entity1.SourceSystemName && 
                    e2.SourceSystemId == entity1.SourceSystemId);
                
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

            return true;
        }
        private bool AreSame(Entity entity1, Entity entity2)
        {
            return entity1.Name == entity2.Name &&
                   entity1.Type == entity2.Type &&
                   entity1.SubType == entity2.SubType &&
                   entity1.Status == entity2.Status &&
                   entity1.OpenDate == entity2.OpenDate &&
                   entity1.CloseDate == entity2.CloseDate &&
                   entity1.Urn == entity2.Urn &&
                   entity1.Ukprn == entity2.Ukprn &&
                   entity1.Uprn == entity2.Uprn &&
                   entity1.CompaniesHouseNumber == entity2.CompaniesHouseNumber &&
                   entity1.CharitiesCommissionNumber == entity2.CharitiesCommissionNumber &&
                   entity1.AcademyTrustCode == entity2.AcademyTrustCode &&
                   entity1.DfeNumber == entity2.DfeNumber &&
                   entity1.LocalAuthorityCode == entity2.LocalAuthorityCode &&
                   entity1.ManagementGroupType == entity2.ManagementGroupType &&
                   entity1.ManagementGroupId == entity2.ManagementGroupId &&
                   entity1.ManagementGroupUkprn == entity2.ManagementGroupUkprn &&
                   entity1.ManagementGroupCompaniesHouseNumber == entity2.ManagementGroupCompaniesHouseNumber;
        }
    }
}