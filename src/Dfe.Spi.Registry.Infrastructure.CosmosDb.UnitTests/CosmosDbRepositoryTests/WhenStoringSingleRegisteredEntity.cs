using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain;
using Microsoft.Azure.Cosmos;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosDbRepositoryTests
{
    public class WhenStoringSingleRegisteredEntity
    {
        private Mock<Container> _containerMock;
        private Mock<IMapper> _mapperMock;
        private Mock<Func<CosmosCombinationOperator, CosmosQuery>> _queryFactoryMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private CosmosDbRepository _repository;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _containerMock = new Mock<Container>();

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m => m.Map(It.IsAny<RegisteredEntity>()))
                .Returns((RegisteredEntity re) => new CosmosRegisteredEntity {Id = re.Id});
            
            _queryFactoryMock = new Mock<Func<CosmosCombinationOperator, CosmosQuery>>();
            _queryFactoryMock.Setup(f => f.Invoke(It.IsAny<CosmosCombinationOperator>()))
                .Returns((CosmosCombinationOperator @operator) => new CosmosQuery(@operator));

            _loggerMock = new Mock<ILoggerWrapper>();

            _repository = new CosmosDbRepository(
                new CosmosDbConnection(_containerMock.Object),
                _mapperMock.Object,
                _queryFactoryMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldUpsertTheEntryInTheContainer(RegisteredEntity registeredEntity)
        {
            await _repository.StoreAsync(registeredEntity, _cancellationToken);

            _containerMock.Verify(
                c => c.UpsertItemAsync(It.Is<CosmosRegisteredEntity>(e => e.Id == registeredEntity.Id), It.IsAny<PartitionKey>(), null, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapEntityToCosmosEntityForStorage(RegisteredEntity registeredEntity, CosmosRegisteredEntity mappedEntity)
        {
            _mapperMock.Setup(m => m.Map(It.IsAny<RegisteredEntity>()))
                .Returns(mappedEntity);

            await _repository.StoreAsync(registeredEntity, _cancellationToken);

            _containerMock.Verify(c => c.UpsertItemAsync(
                    It.Is<CosmosRegisteredEntity>(ce => ce == mappedEntity),
                    It.IsAny<PartitionKey>(),
                    null,
                    _cancellationToken),
                Times.Once);
            _mapperMock.Verify(m => m.Map(registeredEntity), Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldUsePartitionableIdForPartitionKey(RegisteredEntity registeredEntity, CosmosRegisteredEntity mappedEntity)
        {
            _mapperMock.Setup(m => m.Map(It.IsAny<RegisteredEntity>()))
                .Returns(mappedEntity);
        
            await _repository.StoreAsync(registeredEntity, _cancellationToken);
        
            _containerMock.Verify(x => x.UpsertItemAsync(
                    It.IsAny<CosmosRegisteredEntity>(),
                    It.Is<PartitionKey>(pk => PartitionKeyValueIs(pk, mappedEntity.PartitionableId)),
                    null,
                    _cancellationToken),
                Times.Once);
        }

        private bool PartitionKeyValueIs(PartitionKey pk, string value)
        {
            return pk.ToString() == $"[\"{value}\"]";
        }

        private static bool AreEqual<T>(T[] expected, T[] actual)
        {
            if ((expected == null && actual != null) || (actual == null && expected != null))
            {
                return false;
            }

            if (expected.Length != actual.Length)
            {
                return false;
            }

            foreach (var expectedItem in expected)
            {
                if (!actual.Any(actualItem => actualItem != null && actualItem.Equals(expectedItem)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}