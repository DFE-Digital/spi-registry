using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Sync;

namespace Dfe.Spi.Registry.Application.Sync
{
    public interface ISyncManager
    {
        Task ReceiveSyncEntityAsync<T>(SyncEntityEvent<T> @event, string sourceSystemName, CancellationToken cancellationToken) where T: Models.Entities.EntityBase;

        Task ProcessSyncQueueItemAsync(SyncQueueItem queueItem, CancellationToken cancellationToken);
    }
    
    public class SyncManager : ISyncManager
    {
        private readonly ISyncQueue _syncQueue;
        private readonly IRepository _repository;
        private readonly ILoggerWrapper _logger;

        public SyncManager(
            ISyncQueue syncQueue,
            IRepository repository,
            ILoggerWrapper logger)
        {
            _syncQueue = syncQueue;
            _repository = repository;
            _logger = logger;
        }
        
        public async Task ReceiveSyncEntityAsync<T>(SyncEntityEvent<T> @event, string sourceSystemName, CancellationToken cancellationToken) where T : EntityBase
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
            
            _logger.Debug($"Preparing updated version of {queueItem.Entity} at {queueItem.PointInTime}");
            var registeredEntity = new RegisteredEntity
            {
                Id = Guid.NewGuid().ToString().ToLower(),
                Type = queueItem.Entity.EntityType,
                ValidFrom = queueItem.PointInTime,
                Entities = new[] {queueItem.Entity},
                Links = new Link[0],
            };

            await ProcessEntityChangesAsync(existingEntity, registeredEntity, cancellationToken);
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
                    SourceSystemName =  sourceSystemName,
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

        private async Task ProcessEntityChangesAsync(RegisteredEntity existingEntity, RegisteredEntity updatedEntity, CancellationToken cancellationToken)
        {
            if (existingEntity == null)
            {
                _logger.Info($"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) has not been seen before {updatedEntity.ValidFrom}. Creating new entry");
                await _repository.StoreAsync(updatedEntity, cancellationToken);
                return;
            }
            
            // For now, assume only one entity. Will need revisiting when matching put in
            if (AreSame(existingEntity.Entities[0], updatedEntity.Entities[0]))
            {
                _logger.Info($"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) on {updatedEntity.ValidFrom} has not changed since {existingEntity.ValidFrom}. No further action to take");
                return;
            }
            
            _logger.Info($"Entity {updatedEntity.Id} ({updatedEntity.Entities[0]}) on {updatedEntity.ValidFrom} has changed since {existingEntity.ValidFrom}. Updating");

            // TODO: Update in batch?
            existingEntity.ValidTo = updatedEntity.ValidFrom;
            await _repository.StoreAsync(updatedEntity, cancellationToken);
            await _repository.StoreAsync(existingEntity, cancellationToken);
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