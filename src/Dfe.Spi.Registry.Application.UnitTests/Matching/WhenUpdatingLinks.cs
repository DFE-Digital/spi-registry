using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Search;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.Matching
{
    public class WhenUpdatingLinks
    {
        private Mock<IEntityRepository> _entityRepositoryMock;
        private Mock<IMatchingProfileRepository> _profileRepositoryMock;
        private Mock<IMatchProfileProcessor> _matchProfileProcessorMock;
        private Mock<ISearchIndex> _searchIndexMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private MatchManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityRepositoryMock = new Mock<IEntityRepository>();
            _entityRepositoryMock.Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Entity());

            _profileRepositoryMock = new Mock<IMatchingProfileRepository>();

            _matchProfileProcessorMock = new Mock<IMatchProfileProcessor>();
            
            _searchIndexMock = new Mock<ISearchIndex>();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new SearchDocument[0],
                    Skipped = 0,
                    Taken = 100,
                    TotalNumberOfRecords = 0,
                });

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new MatchManager(
                _entityRepositoryMock.Object,
                _profileRepositoryMock.Object,
                _matchProfileProcessorMock.Object,
                _searchIndexMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldLookupSourceEntity(EntityForMatching entityForMatching)
        {
            await _manager.UpdateLinksAsync(entityForMatching, _cancellationToken);

            _entityRepositoryMock.Verify(r => r.GetEntityAsync(
                    entityForMatching.Type, entityForMatching.SourceSystemName, entityForMatching.SourceSystemId,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldGetAllProfiles(EntityForMatching entityForMatching)
        {
            await _manager.UpdateLinksAsync(entityForMatching, _cancellationToken);
            
            _profileRepositoryMock.Verify(r=>r.GetMatchingProfilesAsync(_cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldOnlyProcessProfilesWhereEntityTypeIsSourceOrCandidate(EntityForMatching entityForMatching)
        {
            // Arrange
            var source = new Entity();
            _entityRepositoryMock.Setup(r => r.GetEntityAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(source);
            
            var profileWhereSource = new MatchingProfile
            {
                SourceType = entityForMatching.Type,
                CandidateType = Guid.NewGuid().ToString(),
            };
            var profileWhereCandidate = new MatchingProfile
            {
                SourceType = Guid.NewGuid().ToString(),
                CandidateType = entityForMatching.Type,
            };
            var profileWhereNeither = new MatchingProfile
            {
                SourceType = Guid.NewGuid().ToString(),
                CandidateType = Guid.NewGuid().ToString(),
            };
            _profileRepositoryMock.Setup(r => r.GetMatchingProfilesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    profileWhereSource,
                    profileWhereCandidate,
                    profileWhereNeither,
                });
            
            // Act
            await _manager.UpdateLinksAsync(entityForMatching, _cancellationToken);
            
            // Assert
            _matchProfileProcessorMock.Verify(p=>p.UpdateLinksAsync(
                source, profileWhereSource, _cancellationToken),
                Times.Once);
            _matchProfileProcessorMock.Verify(p=>p.UpdateLinksAsync(
                source, profileWhereCandidate, _cancellationToken),
                Times.Once);
            _matchProfileProcessorMock.Verify(p=>p.UpdateLinksAsync(
                source, profileWhereNeither, _cancellationToken),
                Times.Never);
            _matchProfileProcessorMock.Verify(p=>p.UpdateLinksAsync(
                It.IsAny<Entity>(), It.IsAny<MatchingProfile>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }
    }
}