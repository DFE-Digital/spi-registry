using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Sync;

namespace Dfe.Spi.Registry.Application.Sync
{
    public interface ISyncManager
    {
        Task ReceiveSyncEntityAsync<T>(SyncEntityEvent<T> @event, string sourceSystemName, CancellationToken cancellationToken) where T: Models.Entities.EntityBase;
    }
    
    public class SyncManager : ISyncManager
    {
        private readonly ISyncQueue _syncQueue;
        private readonly ILoggerWrapper _logger;

        public SyncManager(
            ISyncQueue syncQueue,
            ILoggerWrapper logger)
        {
            _syncQueue = syncQueue;
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
    }
}