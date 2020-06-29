using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Functions.Sync;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.Sync.ReceiveEntityEventTests
{
    public class WhenReceivingPostedEntityEvent
    {
        private Mock<ISyncManager> _syncManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _executionContextManagerMock;
        private ReceiveEntityEvent _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _syncManagerMock = new Mock<ISyncManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _executionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _function = new ReceiveEntityEvent(
                _syncManagerMock.Object,
                _loggerMock.Object,
                _executionContextManagerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldSetExecutionContext()
        {
            var request = (HttpRequest)HttpRequestBuilder
                .CreateHttpRequest()
                .WithJsonBody(new SyncEntityEvent<LearningProvider>());
            
            await _function.RunAsync(request, EntityNameTranslator.LearningProviderPlural, "anything", _cancellationToken);
            
            _executionContextManagerMock.Verify(c=>c.SetContext(request.Headers),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldSyncUsingTheDeserialisedLearningProviderEvent(SyncEntityEvent<LearningProvider> @event, string sourceSystemName)
        {
            var request = HttpRequestBuilder
                .CreateHttpRequest()
                .WithJsonBody(@event);

            await _function.RunAsync(request, EntityNameTranslator.LearningProviderPlural, sourceSystemName, _cancellationToken);

            _syncManagerMock.Verify(m => m.ReceiveSyncEntityAsync(
                    It.Is<SyncEntityEvent<LearningProvider>>(actual => ObjectAssert.AreEqual(@event, actual)),
                    sourceSystemName,
                    _cancellationToken),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldSyncUsingTheDeserialisedManagementGroupEvent(SyncEntityEvent<ManagementGroup> @event, string sourceSystemName)
        {
            var request = HttpRequestBuilder
                .CreateHttpRequest()
                .WithJsonBody(@event);

            await _function.RunAsync(request, EntityNameTranslator.ManagementGroupPlural, sourceSystemName, _cancellationToken);

            _syncManagerMock.Verify(m => m.ReceiveSyncEntityAsync(
                    It.Is<SyncEntityEvent<ManagementGroup>>(actual => ObjectAssert.AreEqual(@event, actual)),
                    sourceSystemName,
                    _cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldReturnAcceptedResult()
        {
            var request = HttpRequestBuilder
                .CreateHttpRequest()
                .WithJsonBody(new SyncEntityEvent<LearningProvider>());

            var actual = await _function.RunAsync(request, EntityNameTranslator.LearningProviderPlural, "anything", _cancellationToken);
            
            Assert.IsInstanceOf<AcceptedResult>(actual);
        }

        [Test]
        public async Task ThenItShouldReturnBadRequestResultIfEntityTypeInvalid()
        {
            var request = HttpRequestBuilder
                .CreateHttpRequest()
                .WithJsonBody(new SyncEntityEvent<LearningProvider>());

            var actual = await _function.RunAsync(request, "bad-type", "anything", _cancellationToken);
            
            Assert.IsInstanceOf<BadRequestResult>(actual);
        }
    }
}