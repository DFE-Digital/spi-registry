using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Context.Models;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Sync;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.Sync.SyncManagerTests
{
    public class WhenReceivingSyncEntity
    {
        private Fixture _fixture;
        private Mock<ISyncQueue> _syncQueueMock;
        private Mock<IRepository> _repositoryMock;
        private Mock<IMatcher> _matcherMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private SpiExecutionContext _executionContext;
        private Mock<ISpiExecutionContextManager> _executionContextManagerMock;
        private SyncManager _syncManager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            
            _syncQueueMock = new Mock<ISyncQueue>();
            
            _repositoryMock = new Mock<IRepository>();
            
            _matcherMock = new Mock<IMatcher>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _executionContext = new SpiExecutionContext
            {
                InternalRequestId = _fixture.Create<Guid>(),
                ExternalRequestId = _fixture.Create<string>(),
            };
            _executionContextManagerMock = new Mock<ISpiExecutionContextManager>();
            _executionContextManagerMock.Setup(cm => cm.SpiExecutionContext)
                .Returns(_executionContext);

            _syncManager = new SyncManager(
                _syncQueueMock.Object,
                _repositoryMock.Object,
                _matcherMock.Object,
                _loggerMock.Object,
                _executionContextManagerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [TestCase(SourceSystemNames.GetInformationAboutSchools)]
        [TestCase(SourceSystemNames.UkRegisterOfLearningProviders)]
        public async Task ThenItShouldEnqueueMappedLearningProviderForLearningProviderEvent(string sourceSystemName)
        {
            var @event = _fixture.Create<SyncEntityEvent<LearningProvider>>();
            
            await _syncManager.ReceiveSyncEntityAsync(@event, sourceSystemName, _cancellationToken);

            _syncQueueMock.Verify(q => q.EnqueueEntityForSyncAsync(
                It.Is<SyncQueueItem>(actual => 
                    IsCorrectlyMappedEntity(@event.Details, sourceSystemName, actual.Entity) &&
                    actual.PointInTime == @event.PointInTime),
                _cancellationToken));
        }

        [Test]
        public async Task ThenItShouldUseContextRequestIdsForQueueItem()
        {
            var @event = _fixture.Create<SyncEntityEvent<LearningProvider>>();
            
            await _syncManager.ReceiveSyncEntityAsync(@event, SourceSystemNames.GetInformationAboutSchools, _cancellationToken);
            
            _syncQueueMock.Verify(q => q.EnqueueEntityForSyncAsync(
                It.Is<SyncQueueItem>(actual => 
                    actual.InternalRequestId == _executionContext.InternalRequestId &&
                    actual.ExternalRequestId == _executionContext.ExternalRequestId),
                _cancellationToken));
        }

        [Test]
        public async Task ThenItShouldEnqueueMappedManagementGroupForManagementGroupEvent()
        {
            var @event = _fixture.Create<SyncEntityEvent<ManagementGroup>>();
            
            await _syncManager.ReceiveSyncEntityAsync(@event, SourceSystemNames.GetInformationAboutSchools, _cancellationToken);

            _syncQueueMock.Verify(q => q.EnqueueEntityForSyncAsync(
                It.Is<SyncQueueItem>(actual => 
                    IsCorrectlyMappedEntity(@event.Details, SourceSystemNames.GetInformationAboutSchools, actual.Entity) &&
                    actual.PointInTime == @event.PointInTime),
                _cancellationToken));
        }

        private bool IsCorrectlyMappedEntity(LearningProvider learningProvider, string sourceSystemName, Entity actual)
        {
            var expectedId = sourceSystemName == SourceSystemNames.UkRegisterOfLearningProviders ? actual.Ukprn : actual.Urn;

            return actual.EntityType == EntityNameTranslator.LearningProviderSingular &&
                   actual.SourceSystemName == sourceSystemName &&
                   actual.SourceSystemId == expectedId?.ToString() &&
                   actual.Name == learningProvider.Name &&
                   actual.Type == learningProvider.Type &&
                   actual.SubType == learningProvider.SubType &&
                   actual.Status == learningProvider.Status &&
                   actual.OpenDate == learningProvider.OpenDate &&
                   actual.CloseDate == learningProvider.CloseDate &&
                   actual.Urn == learningProvider.Urn &&
                   actual.Ukprn == learningProvider.Ukprn &&
                   actual.Uprn == learningProvider.Uprn &&
                   actual.CompaniesHouseNumber == learningProvider.CompaniesHouseNumber &&
                   actual.CharitiesCommissionNumber == learningProvider.CharitiesCommissionNumber &&
                   actual.AcademyTrustCode == learningProvider.AcademyTrustCode &&
                   actual.DfeNumber == learningProvider.DfeNumber &&
                   actual.LocalAuthorityCode == learningProvider.LocalAuthorityCode &&
                   actual.ManagementGroupType == learningProvider.ManagementGroup?.Type &&
                   actual.ManagementGroupId == learningProvider.ManagementGroup?.Identifier &&
                   actual.ManagementGroupUkprn == learningProvider.ManagementGroup?.Ukprn &&
                   actual.ManagementGroupCompaniesHouseNumber == learningProvider.ManagementGroup?.CompaniesHouseNumber;
        }

        private bool IsCorrectlyMappedEntity(ManagementGroup managementGroup, string sourceSystemName, Entity actual)
        {
            return actual.EntityType == EntityNameTranslator.ManagementGroupSingular &&
                   actual.SourceSystemName == sourceSystemName &&
                   actual.SourceSystemId == managementGroup.Code &&
                   actual.ManagementGroupType == managementGroup.Type &&
                   actual.ManagementGroupId == managementGroup.Identifier &&
                   actual.ManagementGroupUkprn == managementGroup.Ukprn &&
                   actual.ManagementGroupCompaniesHouseNumber == managementGroup.CompaniesHouseNumber;
        }
    }
}