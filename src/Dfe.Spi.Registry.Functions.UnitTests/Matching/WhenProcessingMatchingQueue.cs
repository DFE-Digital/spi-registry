using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Functions.Matching;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.Matching
{
    public class WhenProcessingMatchingQueue
    {
        private Mock<IMatchManager> _matchManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _executionContextManagerMock;
        private ProcessMatchingQueue _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _matchManagerMock = new Mock<IMatchManager>();
            
            _loggerMock = new Mock<ILoggerWrapper>();
            
            _executionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();
            
            _function = new ProcessMatchingQueue(
                _matchManagerMock.Object,
                _loggerMock.Object,
                _executionContextManagerMock.Object);
            
            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldSetContext()
        {
            await _function.RunAsync(JsonConvert.SerializeObject(new EntityForMatching()), _cancellationToken);
            
            _executionContextManagerMock.Verify(c=>
                c.SetInternalRequestId(It.IsAny<Guid>()),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldUpdateLinksOfEntityThatHasBeenDequeued(EntityForMatching entityForMatching)
        {
            await _function.RunAsync(JsonConvert.SerializeObject(entityForMatching), _cancellationToken);
            
            _matchManagerMock.Verify(m=>m.UpdateLinksAsync(
                It.Is<EntityForMatching>(e => 
                    e.Type == entityForMatching.Type &&
                    e.SourceSystemName == entityForMatching.SourceSystemName &&
                    e.SourceSystemId == entityForMatching.SourceSystemId), _cancellationToken),
                Times.Once);
        }
    }
}