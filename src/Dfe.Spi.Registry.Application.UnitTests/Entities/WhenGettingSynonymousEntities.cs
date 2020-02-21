using System.Linq;
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
    public class WhenGettingSynonymousEntities
    {
        private Mock<IEntityRepository> _entityRepositoryMock;
        private Mock<ILinkRepository> _linkRepositoryMock;
        private Mock<IMatchingQueue> _matchingQueueMock;
        private Mock<ILoggerWrapper> _loggerWrapperMock;
        private EntityManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityRepositoryMock = new Mock<IEntityRepository>();

            _linkRepositoryMock = new Mock<ILinkRepository>();
            _linkRepositoryMock
                .Setup(r => r.GetLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Link
                {
                    Id = "link1",
                    Type = LinkTypes.Synonym,
                    LinkedEntities = new EntityLink[0]
                });
            
            _matchingQueueMock = new Mock<IMatchingQueue>();

            _loggerWrapperMock = new Mock<ILoggerWrapper>();

            _manager = new EntityManager(
                _entityRepositoryMock.Object,
                _linkRepositoryMock.Object,
                _matchingQueueMock.Object,
                _loggerWrapperMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldGetEntityFromRepository(string entityType, string sourceSystemName,
            string sourceSystemId)
        {
            var actual =
                await _manager.GetSynonymousEntitiesAsync(entityType, sourceSystemName, sourceSystemId,
                    _cancellationToken);

            _entityRepositoryMock.Verify(
                r => r.GetEntityAsync(entityType, sourceSystemName, sourceSystemId, _cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldReturnNullIfCannotFindEntity()
        {
            _entityRepositoryMock.Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entity) null);

            var actual = await _manager.GetSynonymousEntitiesAsync("", "", "", _cancellationToken);

            Assert.IsNull(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnNullIfEntityFoundButDoesNotHaveLinks(Entity entity)
        {
            entity.Links = new LinkPointer[0];
            _entityRepositoryMock.Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            var actual = await _manager.GetSynonymousEntitiesAsync(entity.Type, entity.SourceSystemName,
                entity.SourceSystemId, _cancellationToken);

            Assert.IsNull(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnNullIfEntityFoundWithLinksButNoneAreSynonyms(Entity entity)
        {
            entity.Links = new[]
            {
                new LinkPointer {LinkType = "not-synonym"},
            };
            _entityRepositoryMock.Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            var actual = await _manager.GetSynonymousEntitiesAsync(entity.Type, entity.SourceSystemName,
                entity.SourceSystemId, _cancellationToken);

            Assert.IsNull(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldLookupLinkedEntitiesForSynonym(Entity entity, LinkPointer linkPointer)
        {
            linkPointer.LinkType = LinkTypes.Synonym;
            entity.Links = new[] {linkPointer};
            _entityRepositoryMock.Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            await _manager.GetSynonymousEntitiesAsync(entity.Type, entity.SourceSystemName, entity.SourceSystemId,
                _cancellationToken);

            _linkRepositoryMock.Verify(
                r => r.GetLinkAsync(linkPointer.LinkType, linkPointer.LinkId, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnPointersToLinkedEntities(Entity entity, LinkPointer linkPointer, Link link)
        {
            linkPointer.LinkType = LinkTypes.Synonym;
            entity.Links = new[] {linkPointer};
            _entityRepositoryMock
                .Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);
            _linkRepositoryMock
                .Setup(r => r.GetLinkAsync(linkPointer.LinkType, linkPointer.LinkId, _cancellationToken))
                .ReturnsAsync(link);

            var actual = await _manager.GetSynonymousEntitiesAsync(entity.Type, entity.SourceSystemName,
                entity.SourceSystemId,
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.AreEqual(link.LinkedEntities.Length, actual.Length);
            for (var i = 0; i < link.LinkedEntities.Length; i++)
            {
                var expectedName = link.LinkedEntities[i].EntitySourceSystemName;
                var actualName = actual[i].SourceSystemName;
                var expectedId = link.LinkedEntities[i].EntitySourceSystemId;
                var actualId = actual[i].SourceSystemId;

                Assert.AreEqual(expectedName, actualName,
                    $"Expected point {i} to have EntitySourceSystemName {expectedName} but had {actualName}");
                Assert.AreEqual(expectedId, actualId,
                    $"Expected point {i} to have SourceSystemId {expectedId} but had {actualId}");
            }
        }

        [Test, AutoData]
        public async Task ThenItShouldExcludeLinkToRequestEntityFromResult(Entity entity, LinkPointer linkPointer,
            Link link)
        {
            linkPointer.LinkType = LinkTypes.Synonym;
            entity.Links = new[] {linkPointer};
            _entityRepositoryMock
                .Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            link.LinkedEntities = link.LinkedEntities.Concat(new[]
            {
                new EntityLink
                {
                    EntitySourceSystemName = entity.SourceSystemName,
                    EntitySourceSystemId = entity.SourceSystemId,
                },
            }).ToArray();
            _linkRepositoryMock
                .Setup(r => r.GetLinkAsync(linkPointer.LinkType, linkPointer.LinkId, _cancellationToken))
                .ReturnsAsync(link);

            var actual = await _manager.GetSynonymousEntitiesAsync(entity.Type, entity.SourceSystemName,
                entity.SourceSystemId,
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.AreEqual(link.LinkedEntities.Length - 1, actual.Length);
            Assert.IsFalse(actual.Any(x =>
                x.SourceSystemName == entity.SourceSystemName &&
                x.SourceSystemId == entity.SourceSystemId));
        }
    }
}