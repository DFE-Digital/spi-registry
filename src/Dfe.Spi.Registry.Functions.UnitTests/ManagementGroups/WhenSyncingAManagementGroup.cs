using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Registry.Application.ManagementGroups;
using Dfe.Spi.Registry.Functions.ManagementGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.ManagementGroups
{
    public class WhenSyncingAManagementGroup
    {
        private Mock<IManagementGroupManager> _managementGroupManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private Mock<IHttpSpiExecutionContextManager> _executionContextManagerMock;
        private SyncManagementGroup _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _managementGroupManagerMock = new Mock<IManagementGroupManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _executionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _function = new SyncManagementGroup(
                _managementGroupManagerMock.Object,
                _loggerMock.Object,
                _executionContextManagerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldCallManagementGroupManagerWithDeserializedManagementGroup(ManagementGroup managementGroup)
        {
            var req = new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(managementGroup))),
            };

            await _function.RunAsync(req, SourceSystemNames.GetInformationAboutSchools, _cancellationToken);

            _managementGroupManagerMock.Verify(m =>
                    m.SyncManagementGroupAsync(
                        SourceSystemNames.GetInformationAboutSchools,
                        It.Is<ManagementGroup>(mg =>
                            mg.Code == managementGroup.Code),
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
                Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new ManagementGroup()))),
            };

            await _function.RunAsync(req, actual, _cancellationToken);

            _managementGroupManagerMock.Verify(m =>
                    m.SyncManagementGroupAsync(
                        expected,
                        It.IsAny<ManagementGroup>(),
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