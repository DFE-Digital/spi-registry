using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting;
using Dfe.Spi.Registry.Application.Matching;
using Dfe.Spi.Registry.Application.Sync;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;
using Dfe.Spi.Registry.Domain.Sync;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Application.UnitTests.Sync.SyncManagerTests
{
    public class WhenProcessingSyncQueueItem
    {
        private Mock<ISyncQueue> _syncQueueMock;
        private Mock<IRepository> _repositoryMock;
        private Mock<IMatcher> _matcherMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private SyncManager _syncManager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _syncQueueMock = new Mock<ISyncQueue>();

            _repositoryMock = new Mock<IRepository>();

            _matcherMock = new Mock<IMatcher>();
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MatchResult
                {
                    Synonyms = new MatchResultItem[0],
                    Links = new MatchResultLink[0],
                });

            _loggerMock = new Mock<ILoggerWrapper>();

            _syncManager = new SyncManager(
                _syncQueueMock.Object,
                _repositoryMock.Object,
                _matcherMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreNewUnmatchedEntity(SyncQueueItem queueItem)
        {
            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Length == 1 &&
                        entitiesToUpdate[0].Type == queueItem.Entity.EntityType &&
                        entitiesToUpdate[0].ValidFrom == queueItem.PointInTime &&
                        !entitiesToUpdate[0].ValidTo.HasValue &&
                        entitiesToUpdate[0].Entities != null &&
                        entitiesToUpdate[0].Entities.Length == 1 &&
                        ObjectAssert.AreEqual(queueItem.Entity, entitiesToUpdate[0].Entities[0]) &&
                        entitiesToUpdate[0].Links != null &&
                        entitiesToUpdate[0].Links.Length == 0),
                    It.Is<RegisteredEntity[]>(entitiesToDelete =>
                        entitiesToDelete.Length == 0),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldSetValidToOfOldUnmatchedEntityIfBeforePointInTime(SyncQueueItem queueItem)
        {
            _repositoryMock.Setup(r =>
                    r.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RegisteredEntity
                {
                    Id = "old-entity",
                    Type = queueItem.Entity.Type,
                    ValidFrom = queueItem.PointInTime.AddDays(-1),
                    Entities = new[]
                    {
                        CloneLinkedEntity(queueItem.Entity, newName: "different name"),
                    },
                    Links = new Link[0],
                });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Length == 2 &&
                        entitiesToUpdate.Single(x => x.Id == "old-entity").ValidTo == queueItem.PointInTime),
                    It.Is<RegisteredEntity[]>(entitiesToDelete =>
                        entitiesToDelete.Length == 0),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldDeleteOldUnmatchedEntityIfSameAsPointInTime(SyncQueueItem queueItem)
        {
            _repositoryMock.Setup(r =>
                    r.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RegisteredEntity
                {
                    Id = "old-entity",
                    Type = queueItem.Entity.Type,
                    ValidFrom = queueItem.PointInTime,
                    Entities = new[]
                    {
                        CloneLinkedEntity(queueItem.Entity, newName: "different name"),
                    },
                    Links = new Link[0],
                });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Length == 1),
                    It.Is<RegisteredEntity[]>(entitiesToDelete =>
                        entitiesToDelete.Length == 1 &&
                        entitiesToDelete.Any(x => x.Id == "old-entity")),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreNewMatchedEntity(SyncQueueItem queueItem, Entity synonymEntity)
        {
            synonymEntity.EntityType = queueItem.Entity.EntityType;
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MatchResult
                {
                    Synonyms = new[]
                    {
                        new MatchResultItem
                        {
                            MatchReason = "Matched for testing",
                            RegisteredEntity = new RegisteredEntity
                            {
                                Id = "other-entity",
                                Type = queueItem.Entity.EntityType,
                                ValidFrom = queueItem.PointInTime,
                                Entities = new[]
                                {
                                    CloneLinkedEntity(synonymEntity)
                                }
                            }
                        },
                    },
                    Links = new MatchResultLink[0],
                });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Length == 1 &&
                        entitiesToUpdate[0].Type == queueItem.Entity.EntityType &&
                        entitiesToUpdate[0].ValidFrom == queueItem.PointInTime &&
                        !entitiesToUpdate[0].ValidTo.HasValue &&
                        entitiesToUpdate[0].Entities != null &&
                        entitiesToUpdate[0].Entities.Length == 2 &&
                        ObjectAssert.AreEqual(queueItem.Entity, entitiesToUpdate[0].Entities[0]) &&
                        ObjectAssert.AreEqual(synonymEntity, entitiesToUpdate[0].Entities[1]) &&
                        entitiesToUpdate[0].Links != null &&
                        entitiesToUpdate[0].Links.Length == 0),
                    It.IsAny<RegisteredEntity[]>(),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldSetValidToOfOldMatchedEntityIfBeforePointInTime(SyncQueueItem queueItem, Entity synonymEntity)
        {
            synonymEntity.EntityType = queueItem.Entity.EntityType;
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MatchResult
                {
                    Synonyms = new[]
                    {
                        new MatchResultItem
                        {
                            MatchReason = "Matched for testing",
                            RegisteredEntity = new RegisteredEntity
                            {
                                Id = "other-entity",
                                Type = queueItem.Entity.EntityType,
                                ValidFrom = queueItem.PointInTime.AddDays(-1),
                                Entities = new[]
                                {
                                    CloneLinkedEntity(synonymEntity)
                                }
                            }
                        },
                    },
                    Links = new MatchResultLink[0],
                });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Length == 2 &&
                        entitiesToUpdate.Single(x => x.Id == "other-entity").ValidTo == queueItem.PointInTime),
                    It.IsAny<RegisteredEntity[]>(),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldDeleteOldMatchedEntityIfSameAsPointInTime(SyncQueueItem queueItem, Entity synonymEntity)
        {
            synonymEntity.EntityType = queueItem.Entity.EntityType;
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MatchResult
                {
                    Synonyms = new[]
                    {
                        new MatchResultItem
                        {
                            MatchReason = "Matched for testing",
                            RegisteredEntity = new RegisteredEntity
                            {
                                Id = "other-entity",
                                Type = queueItem.Entity.EntityType,
                                ValidFrom = queueItem.PointInTime,
                                Entities = new[]
                                {
                                    CloneLinkedEntity(synonymEntity)
                                }
                            }
                        },
                    },
                    Links = new MatchResultLink[0],
                });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Length == 1),
                    It.Is<RegisteredEntity[]>(entitiesToDelete =>
                        entitiesToDelete.Length == 1 &&
                        entitiesToDelete.Any(x => x.Id == "other-entity")),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreNewVersionOfEntitiesThatAreNewlyLinked(SyncQueueItem queueItem, RegisteredEntity linkedEntity)
        {
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new MatchResult
                    {
                        Synonyms = new MatchResultItem[0],
                        Links = new[]
                        {
                            new MatchResultLink
                            {
                                RegisteredEntity = linkedEntity,
                                Entity = linkedEntity.Entities[0],
                                LinkType = "test",
                                MatchReason = "Linked for testing",
                            },
                        },
                    });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Count(update => update.Entities.Any(entity => ObjectAssert.AreEqual(queueItem.Entity, entity))) == 1 &&
                        entitiesToUpdate.Count(update => AreEqual(linkedEntity.Entities, update.Entities) && update.Id != linkedEntity.Id) == 1 &&
                        entitiesToUpdate.Count(update => update.Links.Count(link => link.LinkType == "test") == 1) == 2),
                    It.IsAny<RegisteredEntity[]>(),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldSetValidToOfLinkedEntityIfValidFromBeforePointInTime(SyncQueueItem queueItem, RegisteredEntity linkedEntity)
        {
            linkedEntity.ValidFrom = queueItem.PointInTime.AddDays(-1);
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new MatchResult
                    {
                        Synonyms = new MatchResultItem[0],
                        Links = new[]
                        {
                            new MatchResultLink
                            {
                                RegisteredEntity = linkedEntity,
                                Entity = linkedEntity.Entities[0],
                                LinkType = "test",
                                MatchReason = "Linked for testing",
                            },
                        },
                    });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.SingleOrDefault(update => update.Id == linkedEntity.Id).ValidTo == queueItem.PointInTime),
                    It.IsAny<RegisteredEntity[]>(),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldDeleteLinkedEntityIfValidFromSameAsPointInTime(SyncQueueItem queueItem, RegisteredEntity linkedEntity)
        {
            linkedEntity.ValidFrom = queueItem.PointInTime;
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new MatchResult
                    {
                        Synonyms = new MatchResultItem[0],
                        Links = new[]
                        {
                            new MatchResultLink
                            {
                                RegisteredEntity = linkedEntity,
                                Entity = linkedEntity.Entities[0],
                                LinkType = "test",
                                MatchReason = "Linked for testing",
                            },
                        },
                    });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.IsAny<RegisteredEntity[]>(),
                    It.Is<RegisteredEntity[]>(entitiesToDelete =>
                        entitiesToDelete.Count(update => update.Id == linkedEntity.Id) == 1),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldNotUpdateLinkedEntityIfLinkAlreadyExists(SyncQueueItem queueItem, RegisteredEntity linkedEntity)
        {
            linkedEntity.ValidFrom = queueItem.PointInTime.AddDays(-1);
            linkedEntity.Links = new[]
            {
                new Link
                {
                    EntityType = queueItem.Entity.EntityType,
                    SourceSystemName = queueItem.Entity.SourceSystemName,
                    SourceSystemId = queueItem.Entity.SourceSystemId,
                    LinkedAt = queueItem.PointInTime.AddDays(-60),
                    LinkedBy = "Unit test fixture",
                    LinkedReason = "Testing existing links",
                    LinkType = "test",
                },
            };
            _matcherMock.Setup(m => m.MatchAsync(It.IsAny<Entity>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    new MatchResult
                    {
                        Synonyms = new MatchResultItem[0],
                        Links = new[]
                        {
                            new MatchResultLink
                            {
                                RegisteredEntity = linkedEntity,
                                Entity = linkedEntity.Entities[0],
                                LinkType = "test",
                                MatchReason = "Linked for testing",
                            },
                        },
                    });

            await _syncManager.ProcessSyncQueueItemAsync(queueItem, _cancellationToken);

            _repositoryMock.Verify(r => r.StoreAsync(
                    It.Is<RegisteredEntity[]>(entitiesToUpdate =>
                        entitiesToUpdate.Any(update => update.Id == linkedEntity.Id)),
                    It.IsAny<RegisteredEntity[]>(),
                    _cancellationToken),
                Times.Never);
            
        }

        private static bool AreEqual<T>(T[] expected, T[] actual)
        {
            if (expected == null && actual == null)
            {
                return true;
            }
            
            if (expected?.Length != actual?.Length)
            {
                return false;
            }
            
            for (var i = 0; i < expected.Length; i++)
            {
                if (!ObjectAssert.AreEqual(expected[i], actual[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static LinkedEntity CloneLinkedEntity(Entity linkedEntity, string newName = null)
        {
            return new LinkedEntity
            {
                EntityType = linkedEntity.EntityType,
                SourceSystemName = linkedEntity.SourceSystemName,
                SourceSystemId = linkedEntity.SourceSystemId,
                Name = newName ?? linkedEntity.Name,
                Type = linkedEntity.Type,
                SubType = linkedEntity.SubType,
                Status = linkedEntity.Status,
                OpenDate = linkedEntity.OpenDate,
                CloseDate = linkedEntity.CloseDate,
                Urn = linkedEntity.Urn,
                Ukprn = linkedEntity.Ukprn,
                Uprn = linkedEntity.Uprn,
                CompaniesHouseNumber = linkedEntity.CompaniesHouseNumber,
                CharitiesCommissionNumber = linkedEntity.CharitiesCommissionNumber,
                AcademyTrustCode = linkedEntity.AcademyTrustCode,
                DfeNumber = linkedEntity.DfeNumber,
                LocalAuthorityCode = linkedEntity.LocalAuthorityCode,
                ManagementGroupType = linkedEntity.ManagementGroupType,
                ManagementGroupId = linkedEntity.ManagementGroupId,
                ManagementGroupUkprn = linkedEntity.ManagementGroupUkprn,
                ManagementGroupCompaniesHouseNumber = linkedEntity.ManagementGroupCompaniesHouseNumber,
            };
        }
    }
}