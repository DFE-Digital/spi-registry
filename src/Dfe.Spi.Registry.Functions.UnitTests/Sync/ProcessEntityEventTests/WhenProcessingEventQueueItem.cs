using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain.Sync;
using Dfe.Spi.Registry.Functions.Sync;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.Sync.ProcessEntityEventTests
{
    public class WhenProcessingEventQueueItem
    {
        private Mock<ISyncManager> _syncManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _executionContextManagerMock;
        private ProcessEntityEvent _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _syncManagerMock = new Mock<ISyncManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _executionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _function = new ProcessEntityEvent(
                _syncManagerMock.Object,
                _loggerMock.Object,
                _executionContextManagerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldSetContextToRequestIdsFromQueueItem(Guid internalRequestId, string externalRequestId)
        {
            var queueMessage = GetQueueMessage(new SyncQueueItem
            {
                InternalRequestId = internalRequestId,
                ExternalRequestId = externalRequestId,
            });
            await _function.RunAsync(queueMessage, _cancellationToken);
            
            _executionContextManagerMock.Verify(c=>c.SetInternalRequestId(internalRequestId));
        }

        [Test, AutoData]
        public async Task ThenItShouldDeserializeAndProcessTheQueueItem(SyncQueueItem syncQueueItem)
        {
            await _function.RunAsync(GetQueueMessage(syncQueueItem), _cancellationToken);

            _syncManagerMock.Verify(m => m.ProcessSyncQueueItemAsync(
                    It.Is<SyncQueueItem>(actualQueueItem => ObjectAssert.AreEqual(syncQueueItem, actualQueueItem)),
                    _cancellationToken),
                Times.Once);
        }


        private CloudQueueMessage GetQueueMessage(SyncQueueItem content)
        {
            return new CloudQueueMessage(JsonConvert.SerializeObject(content));
        }
    }
}