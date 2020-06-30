using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Sync;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.Sync.SyncManagerTests
{
    public class WhenProcessingSyncQueueItem
    {
        private Mock<ISyncQueue> _syncQueueMock;
        private Mock<IRepository> _repositoryMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private SyncManager _syncManager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _syncQueueMock = new Mock<ISyncQueue>();

            _repositoryMock = new Mock<IRepository>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _syncManager = new SyncManager(
                _syncQueueMock.Object,
                _repositoryMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreUnmatchedEntity(SyncQueueItem queueItem)
        {
            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity>(e => 
                        e.Type == queueItem.Entity.EntityType &&
                        e.ValidFrom == queueItem.PointInTime &&
                        !e.ValidTo.HasValue &&
                        e.Entities != null &&
                        e.Entities.Length==1 &&
                        e.Entities[0] == queueItem.Entity &&
                        e.Links != null &&
                        e.Links.Length == 0),
                    _cancellationToken),
                Times.Once);
        }
    }
}