using System.Linq;
using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    internal interface IMapper
    {
        CosmosRegisteredEntity Map(RegisteredEntity registeredEntity);
        RegisteredEntity Map(CosmosRegisteredEntity registeredEntity);
    }

    internal class Mapper : IMapper
    {
        public CosmosRegisteredEntity Map(RegisteredEntity registeredEntity)
        {
            var partitionableId = registeredEntity.Entities.FirstOrDefault(e => e.Urn.HasValue)?.Urn?.ToString();
            if (string.IsNullOrEmpty(partitionableId))
            {
                partitionableId = registeredEntity.Entities.FirstOrDefault(e => e.Ukprn.HasValue)?.Ukprn?.ToString();
            }

            if (string.IsNullOrEmpty(partitionableId))
            {
                partitionableId = registeredEntity.Entities.FirstOrDefault(e => !string.IsNullOrEmpty(e.ManagementGroupCode))?.ManagementGroupCode;
            }

            return new CosmosRegisteredEntity
            {
                Id = registeredEntity.Id,
                Type = registeredEntity.Type,
                ValidFrom = registeredEntity.ValidFrom,
                ValidTo = registeredEntity.ValidTo,
                Entities = registeredEntity.Entities,
                Links = registeredEntity.Links,

                PartitionableId = partitionableId,
                SearchableSourceSystemIdentifiers = registeredEntity.GetSearchableValues(e => $"{e.SourceSystemName}:{e.SourceSystemId}"),
                SearchableName = registeredEntity.GetSearchableValues(e => e.Name),
                SearchableType = registeredEntity.GetSearchableValues(e => e.Type),
                SearchableSubType = registeredEntity.GetSearchableValues(e => e.SubType),
                SearchableStatus = registeredEntity.GetSearchableValues(e => e.Status),
                SearchableOpenDate = registeredEntity.GetSearchableValues(e => e.OpenDate),
                SearchableCloseDate = registeredEntity.GetSearchableValues(e => e.CloseDate),
                SearchableUrn = registeredEntity.GetSearchableValues(e => e.Urn),
                SearchableUkprn = registeredEntity.GetSearchableValues(e => e.Ukprn),
                SearchableUprn = registeredEntity.GetSearchableValues(e => e.Uprn),
                SearchableCompaniesHouseNumber = registeredEntity.GetSearchableValues(e => e.CompaniesHouseNumber),
                SearchableCharitiesCommissionNumber = registeredEntity.GetSearchableValues(e => e.CharitiesCommissionNumber),
                SearchableAcademyTrustCode = registeredEntity.GetSearchableValues(e => e.AcademyTrustCode),
                SearchableDfeNumber = registeredEntity.GetSearchableValues(e => e.DfeNumber),
                SearchableLocalAuthorityCode = registeredEntity.GetSearchableValues(e => e.LocalAuthorityCode),
                SearchableManagementGroupType = registeredEntity.GetSearchableValues(e => e.ManagementGroupType),
                SearchableManagementGroupId = registeredEntity.GetSearchableValues(e => e.ManagementGroupId),
                SearchableManagementGroupCode = registeredEntity.GetSearchableValues(e => e.ManagementGroupCode),
                SearchableManagementGroupUkprn = registeredEntity.GetSearchableValues(e => e.ManagementGroupUkprn),
                SearchableManagementGroupCompaniesHouseNumber = registeredEntity.GetSearchableValues(e => e.ManagementGroupCompaniesHouseNumber),
            };
        }

        public RegisteredEntity Map(CosmosRegisteredEntity registeredEntity)
        {
            return new RegisteredEntity
            {
                Id = registeredEntity.Id,
                Type = registeredEntity.Type,
                ValidFrom = registeredEntity.ValidFrom,
                ValidTo = registeredEntity.ValidTo,
                Entities = registeredEntity.Entities,
                Links = registeredEntity.Links,
            };
        }
    }
}