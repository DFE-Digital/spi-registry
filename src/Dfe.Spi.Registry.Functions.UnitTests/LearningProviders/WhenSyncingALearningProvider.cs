using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.LearningProviders;
using Dfe.Spi.Registry.Functions.LearningProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.LearningProviders
{
    public class WhenSyncingALearningProvider
    {
        private Mock<ILearningProviderManager> _learningProviderManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _executionContextManagerMock;
        private SyncLearningProvider _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _learningProviderManagerMock = new Mock<ILearningProviderManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _executionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _function = new SyncLearningProvider(
                _learningProviderManagerMock.Object,
                _loggerMock.Object,
                _executionContextManagerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldCallLearningManagerWithDeserializedLearningProvider(LearningProvider learningProvider)
        {
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(learningProvider))),
            };

            await _function.RunAsync(req, SourceSystemNames.GetInformationAboutSchools, _cancellationToken);

            _learningProviderManagerMock.Verify(m =>
                    m.SyncLearningProviderAsync(
                        SourceSystemNames.GetInformationAboutSchools,
                        It.Is<LearningProvider>(lp =>
                            lp.Urn == learningProvider.Urn),
                        _cancellationToken),
                Times.Once);
        }

        [TestCase("gias", SourceSystemNames.GetInformationAboutSchools)]
        [TestCase("ukrlp", SourceSystemNames.UkRegisterOfLearningProviders)]
        [TestCase("uKrLp", SourceSystemNames.UkRegisterOfLearningProviders)]
        public async Task ThenItShouldCorrectSourceSystemNameCasing(string actual, string expected)
        {
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new LearningProvider()))),
            };

            await _function.RunAsync(req, actual, _cancellationToken);

            _learningProviderManagerMock.Verify(m =>
                    m.SyncLearningProviderAsync(
                        expected,
                        It.IsAny<LearningProvider>(),
                        _cancellationToken),
                Times.Once);
        }
        
        [Test]
        public async Task ThenItShouldReturnNotFoundIfSourceSystemNotRecognised()
        {
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new LearningProvider()))),
            };

            var actual = await _function.RunAsync(req, "NotASystem", _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<NotFoundResult>(actual);
        }
        
        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldReturnAcceptedResult(LearningProvider learningProvider)
        {
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(learningProvider))),
            };

            var actual = await _function.RunAsync(req, SourceSystemNames.GetInformationAboutSchools, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<AcceptedResult>(actual);
        }
    }
}