using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Queuing;
using Dfe.Spi.Registry.Domain.Search;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.Entities
{
    public class WhenSearchingForEntities
    {
        private Mock<IEntityRepository> _entityRepositoryMock;
        private Mock<ILinkRepository> _linkRepositoryMock;
        private Mock<IMatchingQueue> _matchingQueueMock;
        private Mock<ISearchIndex> _searchIndexMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private EntityManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityRepositoryMock = new Mock<IEntityRepository>();

            _linkRepositoryMock = new Mock<ILinkRepository>();

            _matchingQueueMock = new Mock<IMatchingQueue>();

            _searchIndexMock = new Mock<ISearchIndex>();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new SearchDocument[0],
                });

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new EntityManager(
                _entityRepositoryMock.Object,
                _linkRepositoryMock.Object,
                _matchingQueueMock.Object,
                _searchIndexMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldSearchIndexWithGivenCriteria(string entityType)
        {
            var searchRequest = BuildValidSearchRequest();
            
            await _manager.SearchAsync(searchRequest, entityType, _cancellationToken);

            _searchIndexMock.Verify(idx => idx.SearchAsync(searchRequest, entityType, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnThePagingInfoFromTheResponse(string entityType,
            int skipped, int taken, int totalNumberOfRecords)
        {
            var searchRequest = BuildValidSearchRequest();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new SearchDocument[0],
                    Skipped = skipped,
                    Taken = taken,
                    TotalNumberOfRecords = totalNumberOfRecords,
                });

            var actual = await _manager.SearchAsync(searchRequest, entityType, _cancellationToken);

            Assert.AreEqual(skipped, actual.Skipped);
            Assert.AreEqual(taken, actual.Taken);
            Assert.AreEqual(totalNumberOfRecords, actual.TotalNumberOfRecords);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnASynonymousEntitiesPerSearchResult(string entityType, string sourceSystem, string sourceId1, string sourceId2)
        {
            var searchRequest = BuildValidSearchRequest();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new[]
                    {
                        new SearchDocument {ReferencePointer = $"entity:{entityType}:{sourceSystem}:{sourceId1}"},
                        new SearchDocument {ReferencePointer = $"entity:{entityType}:{sourceSystem}:{sourceId2}"},
                    },
                });

            var actual = await _manager.SearchAsync(searchRequest, entityType, _cancellationToken);

            Assert.IsNotNull(actual.Results);
            Assert.AreEqual(2, actual.Results.Length);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnEntityPointerUsingReferencePointerDetailsIfPointerToEntity(string entityType, string sourceSystem, string sourceId)
        {
            var searchRequest = BuildValidSearchRequest();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new[]
                    {
                        new SearchDocument {ReferencePointer = $"entity:{entityType}:{sourceSystem}:{sourceId}"},
                    },
                });

            var actual = await _manager.SearchAsync(searchRequest, entityType, _cancellationToken);

            Assert.IsNotNull(actual.Results[0].Entities);
            Assert.AreEqual(1, actual.Results[0].Entities.Length);
            Assert.AreEqual(sourceSystem, actual.Results[0].Entities[0].SourceSystemName);
            Assert.AreEqual(sourceId, actual.Results[0].Entities[0].SourceSystemId);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnPointerToAllEntitiesReferencedByLinkIfPointerIsLink(string entityType, string linkType, string linkId, 
            EntityLink linkedEntity1, EntityLink linkedEntity2)
        {
            var searchRequest = BuildValidSearchRequest();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new[]
                    {
                        new SearchDocument {ReferencePointer = $"link:{linkType}:{linkId}"},
                    },
                });
            _linkRepositoryMock.Setup(r =>
                    r.GetLinkAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Link
                {
                    LinkedEntities = new[] {linkedEntity1, linkedEntity2},
                });

            var actual = await _manager.SearchAsync(searchRequest, entityType, _cancellationToken);

            Assert.IsNotNull(actual.Results[0].Entities);
            Assert.AreEqual(2, actual.Results[0].Entities.Length);
            Assert.AreEqual(linkedEntity1.EntitySourceSystemName, actual.Results[0].Entities[0].SourceSystemName);
            Assert.AreEqual(linkedEntity1.EntitySourceSystemId, actual.Results[0].Entities[0].SourceSystemId);
            Assert.AreEqual(linkedEntity2.EntitySourceSystemName, actual.Results[0].Entities[1].SourceSystemName);
            Assert.AreEqual(linkedEntity2.EntitySourceSystemId, actual.Results[0].Entities[1].SourceSystemId);
            _linkRepositoryMock.Verify(r => r.GetLinkAsync(linkType, linkId, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public void ThenItShouldThrowExceptionIsSearchReferencePointerIsNotLinkOfEntity(string entityType)
        {
            var searchRequest = BuildValidSearchRequest();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new[]
                    {
                        new SearchDocument {ReferencePointer = $"other:thing"},
                    },
                });

            Assert.ThrowsAsync<Exception>(async () =>
                await _manager.SearchAsync(searchRequest, entityType, _cancellationToken));
        }

        [Test, AutoData]
        public void ThenItShouldThrowInvalidRequestExceptionIfSearchRequestIsInvalid(string entityType)
        {
            var actual = Assert.ThrowsAsync<InvalidRequestException>(async () =>
                await _manager.SearchAsync(new SearchRequest(), entityType, _cancellationToken));
            Assert.IsNotNull(actual.Reasons);
        }


        private SearchRequest BuildValidSearchRequest()
        {
            return new SearchRequest
            {
                Groups = new []
                {
                    new SearchGroup
                    {
                        Filter = new []
                        {
                            new DataFilter
                            {
                                Field = "Name",
                                Operator = DataOperator.Equals,
                                Value = "something",
                            }, 
                        },
                        CombinationOperator = "and",
                    }, 
                },
                CombinationOperator = "and",
                Skip = 0,
                Take = 10,
            };
        }
    }
}