using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Application.LearningProviders;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.LearningProviders
{
    public class WhenSyncingLearningProvider
    {
        private Mock<IEntityManager> _entityManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private LearningProviderManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityManagerMock = new Mock<IEntityManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new LearningProviderManager(
                _entityManagerMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldMapProviderToEntity(string source, LearningProvider learningProvider)
        {
            await _manager.SyncLearningProviderAsync(source, learningProvider, _cancellationToken);

            _entityManagerMock.Verify(m => m.SyncEntityAsync(
                    It.Is<Entity>(e =>
                        e.Type == TypeNames.LearningProvider &&
                        e.SourceSystemName == source &&
                        e.SourceSystemId == learningProvider.Urn.ToString() &&
                        e.Data != null &&
                        e.Data["urn"] == learningProvider.Urn.ToString() &&
                        e.Data["ukprn"] == learningProvider.Ukprn.Value.ToString()),
                    _cancellationToken),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldNotMapProviderManagementGroupCodeToEntityIfNotAvailable(string source, LearningProvider learningProvider)
        {
            learningProvider.ManagementGroup = null;
            
            await _manager.SyncLearningProviderAsync(source, learningProvider, _cancellationToken);

            _entityManagerMock.Verify(m => m.SyncEntityAsync(
                    It.Is<Entity>(e =>
                        e.Data != null &&
                        !e.Data.ContainsKey("managementGroupCode")),
                    _cancellationToken),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldMapSourceSystemIdFromUrnForGias(LearningProvider learningProvider)
                 {
            await _manager.SyncLearningProviderAsync(SourceSystemNames.GetInformationAboutSchools, learningProvider, _cancellationToken);

            _entityManagerMock.Verify(m => m.SyncEntityAsync(
                    It.Is<Entity>(e => 
                        e.SourceSystemId == learningProvider.Urn.ToString()),
                    _cancellationToken),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldMapSourceSystemIdFromUkprnForUkrlp(LearningProvider learningProvider)
                 {
            await _manager.SyncLearningProviderAsync(SourceSystemNames.UkRegisterOfLearningProviders, learningProvider, _cancellationToken);

            _entityManagerMock.Verify(m => m.SyncEntityAsync(
                    It.Is<Entity>(e => 
                        e.SourceSystemId == learningProvider.Ukprn.Value.ToString()),
                    _cancellationToken),
                Times.Once);
        }
    }
}