using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Entities;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Functions.LearningProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Functions.UnitTests.LearningProviders
{
    public class WhenGettingLearningProviderSynonyms
    {
        private Mock<IEntityManager> _entityManagerMock;
        private Mock<ILoggerWrapper> _loggerWrapperMock;
        private Mock<IHttpSpiExecutionContextManager> _executionContextManagerMock;
        private GetLearningProviderSynonyms _function;
        private DefaultHttpRequest _request;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityManagerMock = new Mock<IEntityManager>();

            _loggerWrapperMock = new Mock<ILoggerWrapper>();

            _executionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _function = new GetLearningProviderSynonyms(
                _entityManagerMock.Object,
                _loggerWrapperMock.Object,
                _executionContextManagerMock.Object);

            _request = new DefaultHttpRequest(new DefaultHttpContext());
            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldGetSynonymsFromEntityManager(string system, string id)
        {
            await _function.RunAsync(_request, system, id, _cancellationToken);

            _entityManagerMock.Verify(m => m.GetSynonymousEntitiesAsync(
                    TypeNames.LearningProvider, system, id, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnNotFoundIfEntityManagerReturnsNull(string system, string id)
        {
            _entityManagerMock.Setup(m => m.GetSynonymousEntitiesAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EntityPointer[]) null);

            var actual = await _function.RunAsync(_request, system, id, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<NotFoundResult>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnEntityPointersInResponse(string system, string id, EntityPointer[] pointers)
        {
            _entityManagerMock.Setup(m => m.GetSynonymousEntitiesAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(pointers);

            var actual = await _function.RunAsync(_request, system, id, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<OkObjectResult>(actual);
            Assert.IsInstanceOf<GetSynonymsResponse>(((OkObjectResult) actual).Value);
            Assert.AreEqual(pointers, ((GetSynonymsResponse) ((OkObjectResult) actual).Value).Synonyms);
        }
    }
}