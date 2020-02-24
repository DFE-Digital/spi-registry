using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Functions.Matching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.Matching
{
    public class WhenUpdatingEntityLinks
    {
        private Mock<IMatchManager> _matchManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _executionContextManagerMock;
        private UpdateEntityLinks _function;
        private HttpRequest _request;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _matchManagerMock = new Mock<IMatchManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _executionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _function = new UpdateEntityLinks(
                _matchManagerMock.Object,
                _loggerMock.Object,
                _executionContextManagerMock.Object);

            _request = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(
                    JsonConvert.SerializeObject(new EntityForMatching
                    {
                        Type = "thing",
                        SourceSystemName = "somewhere",
                        SourceSystemId = "the-id",
                    }))),
            };
            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldSetupContext()
        {
            await _function.RunAsync(_request, _cancellationToken);

            _executionContextManagerMock.Verify(cm => cm.SetContext(_request.Headers), 
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldCallManagerWithDeserializedDetails(EntityForMatching entityForMatching)
        {
            _request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(entityForMatching)));
            
            await _function.RunAsync(_request, _cancellationToken);
            
            _matchManagerMock.Verify(m=>m.UpdateLinksAsync(
                It.Is<EntityForMatching>(actual=>
                    actual.Type == entityForMatching.Type &&
                    actual.SourceSystemName == entityForMatching.SourceSystemName &&
                    actual.SourceSystemId == entityForMatching.SourceSystemId),
                _cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldReturnAccepted()
        {
            var actual = await _function.RunAsync(_request, _cancellationToken);
            
            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<AcceptedResult>(actual);
        }
    }
}