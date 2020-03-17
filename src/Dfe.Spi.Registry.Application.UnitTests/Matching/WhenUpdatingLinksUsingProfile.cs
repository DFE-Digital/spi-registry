using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Links;
using Dfe.Spi.Registry.Domain.Matching;
using Dfe.Spi.Registry.Domain.Search;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.Matching
{
    public class WhenUpdatingLinksUsingProfile
    {
        private const string Type1 = "thing";
        private const string Type2 = "otherthing";

        private Mock<IEntityRepository> _entityRepositoryMock;
        private Mock<ILinkRepository> _linkRepositoryMock;
        private Mock<ISearchIndex> _searchIndexMock;
        private Mock<IEntityLinker> _entityLinkerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private MatchProfileProcessor _processor;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _entityRepositoryMock = new Mock<IEntityRepository>();

            _linkRepositoryMock = new Mock<ILinkRepository>();

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

            _entityLinkerMock = new Mock<IEntityLinker>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _processor = new MatchProfileProcessor(
                _entityRepositoryMock.Object,
                _linkRepositoryMock.Object,
                _searchIndexMock.Object,
                _entityLinkerMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldNotCreateAnyLinksIfNoMatchesFound()
        {
            // Arrange
            var source = GetEntity();
            var profile = GetMatchingProfile();

            // Act
            await _processor.UpdateLinksAsync(source, profile, _cancellationToken);

            // Assert
            _linkRepositoryMock.Verify(r => r.StoreAsync(
                    It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _entityRepositoryMock.Verify(r => r.StoreAsync(
                    It.IsAny<Entity>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task ThenItShouldLinkSourceAndCandidateIfMatchSournce()
        {
            // Arrange
            var source = GetEntity();
            var candidate = GetEntity(Type2);
            var profile = GetMatchingProfile();
            _searchIndexMock.Setup(idx =>
                    idx.SearchAsync(It.IsAny<SearchRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SearchIndexResult
                {
                    Results = new[]
                    {
                        new SearchDocument
                        {
                            EntityType = candidate.Type,
                            ReferencePointer =
                                $"entity:{candidate.Type}:{candidate.SourceSystemName}:{candidate.SourceSystemId}",
                        },
                    },
                    Skipped = 0,
                    Taken = 100,
                    TotalNumberOfRecords = 0,
                });
            _entityRepositoryMock.Setup(r => r.GetEntityAsync(candidate.Type, candidate.SourceSystemName,
                    candidate.SourceSystemId, _cancellationToken))
                .ReturnsAsync(candidate);

            // Act
            await _processor.UpdateLinksAsync(source, profile, _cancellationToken);

            // Assert
            _entityLinkerMock.Verify(lnkr => lnkr.LinkEntitiesAsync(
                    source, candidate, profile, profile.Rules[0].Name, _cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldNotAddLinkAtAllIfBothAlreadyInLink()
        {
            // Arrange
            var source = GetEntity();
            var candidate = GetEntity(Type2);
            var profile = GetMatchingProfile();
            var linkId = Guid.NewGuid().ToString();

            source.Links = new[]
            {
                new LinkPointer {LinkId = linkId, LinkType = profile.LinkType},
            };
            candidate.Links = new[]
            {
                new LinkPointer {LinkId = linkId, LinkType = profile.LinkType},
            };

            _entityRepositoryMock.Setup(r => r.GetEntitiesOfTypeAsync(source.Type, _cancellationToken))
                .ReturnsAsync(new[] {candidate});

            _linkRepositoryMock.Setup(r => r.GetLinkAsync(profile.LinkType, linkId, _cancellationToken))
                .ReturnsAsync(new Link
                {
                    Id = linkId,
                    Type = profile.LinkType,
                    LinkedEntities = new[]
                    {
                        new EntityLink
                        {
                            EntityType = source.Type,
                            EntitySourceSystemName = source.SourceSystemName,
                            EntitySourceSystemId = source.SourceSystemId,
                        },
                        new EntityLink
                        {
                            EntityType = candidate.Type,
                            EntitySourceSystemName = candidate.SourceSystemName,
                            EntitySourceSystemId = candidate.SourceSystemId,
                        },
                    }
                });

            // Act
            await _processor.UpdateLinksAsync(source, profile, _cancellationToken);

            // Assert
            _linkRepositoryMock.Verify(r => r.StoreAsync(
                    It.IsAny<Link>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _entityRepositoryMock.Verify(r => r.StoreAsync(
                    It.IsAny<Entity>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private Entity GetEntity(string type = Type1)
        {
            return new Entity
            {
                Type = type,
                Data = new Dictionary<string, string>
                {
                    {"urn", "123456"},
                },
                SourceSystemName = "GIAS",
                SourceSystemId = "123456",
            };
        }

        private MatchingProfile GetMatchingProfile()
        {
            return new MatchingProfile
            {
                SourceType = "thing",
                CandidateType = "otherthing",
                LinkType = "friends",
                Name = "test-profile",
                Rules = new[]
                {
                    new MatchingRuleset
                    {
                        Name = "test-ruleset",
                        Criteria = new[]
                        {
                            new MatchingCriteria
                            {
                                SourceAttribute = "urn",
                                CandidateAttribute = "urn"
                            },
                        }
                    },
                },
            };
        }
    }
}