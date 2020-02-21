using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Application.LearningProviders;
using Dfe.Spi.Registry.Application.ManagementGroups;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.ManagementGroups
{
    public class WhenSyncingManagementGroup
    {
        private Mock<IEntityManager> _entityManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private ManagementGroupManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityManagerMock = new Mock<IEntityManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new ManagementGroupManager(
                _entityManagerMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldMapManagementGroupToEntity(string source, ManagementGroup managementGroup)
        {
            await _manager.SyncManagementGroupAsync(source, managementGroup, _cancellationToken);

            _entityManagerMock.Verify(m => m.SyncEntityAsync(
                    It.Is<Entity>(e =>
                        e.Type == TypeNames.ManagementGroup &&
                        e.SourceSystemName == source &&
                        e.SourceSystemId == managementGroup.Code &&
                        e.Data != null),
                    _cancellationToken),
                Times.Once);
        }
    }
}