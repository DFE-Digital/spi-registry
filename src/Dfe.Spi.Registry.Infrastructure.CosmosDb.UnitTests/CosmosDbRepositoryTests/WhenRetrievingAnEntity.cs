using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain;
using Microsoft.Azure.Cosmos;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosDbRepositoryTests
{
    public class WhenRetrievingAnEntity
    {
        private Mock<Container> _containerMock;
        private Mock<IMapper> _mapperMock;
        private Mock<CosmosQuery> _queryMock;
        private Mock<Func<CosmosCombinationOperator, CosmosQuery>> _queryFactoryMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private CosmosDbRepository _repository;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _containerMock = new Mock<Container>();
            _containerMock.Setup(c => c.GetItemQueryIterator<CosmosRegisteredEntity>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                .Returns(new Mock<FeedIterator<CosmosRegisteredEntity>>().Object);

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m => m.Map(It.IsAny<RegisteredEntity>()))
                .Returns((RegisteredEntity re) => new CosmosRegisteredEntity {Id = re.Id});

            _queryMock = new Mock<CosmosQuery>();
            _queryMock.Setup(q => q.AddCondition(It.IsAny<string>(), It.IsAny<DataOperator>(), It.IsAny<string>()))
                .Returns(_queryMock.Object);
            _queryMock.Setup(q => q.AddTypeCondition(It.IsAny<string>()))
                .Returns(_queryMock.Object);
            _queryMock.Setup(q => q.AddSourceSystemIdCondition(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_queryMock.Object);
            _queryMock.Setup(q => q.AddPointInTimeCondition(It.IsAny<DateTime>()))
                .Returns(_queryMock.Object);
            _queryMock.Setup(q => q.ToString())
                .Returns("query-text");

            _queryFactoryMock = new Mock<Func<CosmosCombinationOperator, CosmosQuery>>();
            _queryFactoryMock.Setup(f => f.Invoke(It.IsAny<CosmosCombinationOperator>()))
                .Returns(_queryMock.Object);

            _loggerMock = new Mock<ILoggerWrapper>();

            _repository = new CosmosDbRepository(
                new CosmosDbConnection(_containerMock.Object), 
                _mapperMock.Object,
                _queryFactoryMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldBuildNewAndQuery(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime)
        {
            await _repository.RetrieveAsync(entityType, sourceSystemName, sourceSystemId, pointInTime, _cancellationToken);

            _queryFactoryMock.Verify(f => f.Invoke(CosmosCombinationOperator.And), Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldAddTypeCondition(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime)
        {
            await _repository.RetrieveAsync(entityType, sourceSystemName, sourceSystemId, pointInTime, _cancellationToken);

            _queryMock.Verify(q => q.AddTypeCondition(entityType), Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldAddSourceSystemIdCondition(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime)
        {
            await _repository.RetrieveAsync(entityType, sourceSystemName, sourceSystemId, pointInTime, _cancellationToken);

            _queryMock.Verify(q => q.AddSourceSystemIdCondition(sourceSystemName, sourceSystemId), Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldAddPointInTimeCondition(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime)
        {
            await _repository.RetrieveAsync(entityType, sourceSystemName, sourceSystemId, pointInTime, _cancellationToken);

            _queryMock.Verify(q => q.AddPointInTimeCondition(pointInTime), Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRunTheQueryAgainstCosmosDb(string entityType, string sourceSystemName, string sourceSystemId, DateTime pointInTime)
        {
            var queryText = $"{entityType}:{sourceSystemName}:{sourceSystemId}:{pointInTime}";
            _queryMock.Setup(q => q.ToString())
                .Returns(queryText);

            await _repository.RetrieveAsync(entityType, sourceSystemName, sourceSystemId, pointInTime, _cancellationToken);

            _containerMock.Verify(c => c.GetItemQueryIterator<CosmosRegisteredEntity>(
                    It.Is<QueryDefinition>(qd => qd.QueryText == queryText), null, null),
                Times.Once);
        }
        
        [Test, AutoData]
        public async Task ThenItShouldReturnResultOfQuery(
            string entityType, 
            string sourceSystemName, 
            string sourceSystemId, 
            DateTime pointInTime, 
            CosmosRegisteredEntity expected)
        {
            var feedIteratorMock = new Mock<FeedIterator<CosmosRegisteredEntity>>();
            feedIteratorMock.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            feedIteratorMock.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StaticFeedResponse<CosmosRegisteredEntity>(expected));
            
            _containerMock.Setup(c => c.GetItemQueryIterator<CosmosRegisteredEntity>(
                    It.IsAny<QueryDefinition>(), It.IsAny<string>(), It.IsAny<QueryRequestOptions>()))
                .Returns(feedIteratorMock.Object);
            
            var actual = await _repository.RetrieveAsync(entityType, sourceSystemName, sourceSystemId, pointInTime, _cancellationToken);

            Assert.AreSame(expected, actual);
        }
    }
}