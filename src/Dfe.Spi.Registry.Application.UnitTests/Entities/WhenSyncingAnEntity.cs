using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Queuing;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.Entities
{
    public class WhenSyncingAnEntity
    {
        private Mock<IEntityRepository> _entityRepositoryMock;
        private Mock<ILinkRepository> _linkRepositoryMock;
        private Mock<IMatchingQueue> _matchingQueueMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private EntityManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityRepositoryMock = new Mock<IEntityRepository>();

            _linkRepositoryMock = new Mock<ILinkRepository>();
            
            _matchingQueueMock = new Mock<IMatchingQueue>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new EntityManager(
                _entityRepositoryMock.Object,
                _linkRepositoryMock.Object,
                _matchingQueueMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreEntityInRepository(Entity entity)
        {
            await _manager.SyncEntityAsync(entity, _cancellationToken);

            _entityRepositoryMock.Verify(r => r.StoreAsync(
                    entity, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldQueueEntityForMatching(Entity entity)
        {
            await _manager.SyncEntityAsync(entity, _cancellationToken);

            _matchingQueueMock.Verify(q => q.EnqueueAsync(
                    It.Is<EntityForMatching>(e=>
                        e.Type == entity.Type &&
                        e.SourceSystemName == entity.SourceSystemName &&
                        e.SourceSystemId == entity.SourceSystemId), _cancellationToken),
                Times.Once);
        }
    }
}