using System.Linq;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Registry.Domain;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.MapperTests
{
    public class WhenMappingRegisteredEntityToCosmosRegisteredEntity
    {
        private Mapper _mapper;

        [SetUp]
        public void Arrange()
        {
            _mapper = new Mapper();
        }
        
        [Test, AutoData]
        public async Task ThenItShouldMapEntityToCosmosEntityForStorage(RegisteredEntity registeredEntity)
        {
            var actual = _mapper.Map(registeredEntity);
            
            Assert.IsNotNull(actual);
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => $"{e.SourceSystemName}:{e.SourceSystemId}"), actual.SearchableSourceSystemIdentifiers));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.Name), actual.SearchableName));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.Type), actual.SearchableType));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.SubType), actual.SearchableSubType));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.Status), actual.SearchableStatus));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.OpenDate), actual.SearchableOpenDate));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.CloseDate), actual.SearchableCloseDate));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.Urn), actual.SearchableUrn));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.Ukprn), actual.SearchableUkprn));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.Uprn), actual.SearchableUprn));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.CompaniesHouseNumber), actual.SearchableCompaniesHouseNumber));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.CharitiesCommissionNumber), actual.SearchableCharitiesCommissionNumber));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.AcademyTrustCode), actual.SearchableAcademyTrustCode));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.DfeNumber), actual.SearchableDfeNumber));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.LocalAuthorityCode), actual.SearchableLocalAuthorityCode));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.ManagementGroupType), actual.SearchableManagementGroupType));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.ManagementGroupId), actual.SearchableManagementGroupId));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.ManagementGroupCode), actual.SearchableManagementGroupCode));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.ManagementGroupUkprn), actual.SearchableManagementGroupUkprn));
            Assert.IsTrue(AreEqual(registeredEntity.GetSearchableValues(e => e.ManagementGroupCompaniesHouseNumber), actual.SearchableManagementGroupCompaniesHouseNumber));
        }

        [Test, AutoData]
        public async Task ThenItShouldUseUrnForPartitionableIdIfAvailable(RegisteredEntity registeredEntity, long urn)
        {
            registeredEntity.Entities.First().Urn = urn;

            var actual = _mapper.Map(registeredEntity);

            Assert.AreEqual(urn.ToString(), actual.PartitionableId);
        }

        [Test, AutoData]
        public async Task ThenItShouldUseUkprnForPartitionKeyIfUrnNotAvailable(RegisteredEntity registeredEntity, long ukprn)
        {
            foreach (var entity in registeredEntity.Entities)
            {
                entity.Urn = null;
            }
            registeredEntity.Entities.First().Ukprn = ukprn;

            var actual = _mapper.Map(registeredEntity);

            Assert.AreEqual(ukprn.ToString(), actual.PartitionableId);
        }

        [Test, AutoData]
        public async Task ThenItShouldUseManagementGroupCodeForPartitionKeyIfUrnAndUkprnNotAvailable(RegisteredEntity registeredEntity, string managementGroupCode)
        {
            foreach (var entity in registeredEntity.Entities)
            {
                entity.Urn = null;
                entity.Ukprn = null;
            }
            registeredEntity.Entities.First().ManagementGroupCode = managementGroupCode;

            var actual = _mapper.Map(registeredEntity);

            Assert.AreEqual(managementGroupCode, actual.PartitionableId);
        }

        private static bool AreEqual<T>(T[] expected, T[] actual)
        {
            if ((expected == null && actual != null) || (actual == null && expected != null))
            {
                return false;
            }

            if (expected.Length != actual.Length)
            {
                return false;
            }

            foreach (var expectedItem in expected)
            {
                if (!actual.Any(actualItem => actualItem != null && actualItem.Equals(expectedItem)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}