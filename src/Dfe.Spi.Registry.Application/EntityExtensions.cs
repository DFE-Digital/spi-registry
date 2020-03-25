using System;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Application
{
    public static class EntityExtensions
    {
        public static SearchDocument ToSearchDocument(this Entity entity)
        {
            return new SearchDocument
            {
                Id = Guid.NewGuid().ToString(),
                EntityType = entity.Type,
                ReferencePointer = $"entity:{entity.Type}:{entity.SourceSystemName}:{entity.SourceSystemId}",
                SortableEntityName = entity.Data.GetValue(DataAttributeNames.Name)?.ToLower(),
                Name = ValueToArray(entity.Data.GetValue(DataAttributeNames.Name)),
                Type = ValueToArray(entity.Data.GetValue(DataAttributeNames.Type)),
                SubType = ValueToArray(entity.Data.GetValue(DataAttributeNames.SubType)),
                OpenDate = ValueToArray(entity.Data.GetValueAsDateTime(DataAttributeNames.OpenDate)),
                CloseDate = ValueToArray(entity.Data.GetValueAsDateTime(DataAttributeNames.CloseDate)),
                Urn = ValueToArray(entity.Data.GetValueAsLong(DataAttributeNames.Urn)),
                Ukprn = ValueToArray(entity.Data.GetValueAsLong(DataAttributeNames.Ukprn)),
                Uprn = ValueToArray(entity.Data.GetValue(DataAttributeNames.Uprn)),
                CompaniesHouseNumber = ValueToArray(entity.Data.GetValue(DataAttributeNames.CompaniesHouseNumber)),
                CharitiesCommissionNumber =
                    ValueToArray(entity.Data.GetValue(DataAttributeNames.CharitiesCommissionNumber)),
                AcademyTrustCode = ValueToArray(entity.Data.GetValue(DataAttributeNames.AcademyTrustCode)),
                DfeNumber = ValueToArray(entity.Data.GetValue(DataAttributeNames.DfeNumber)),
                LocalAuthorityCode = ValueToArray(entity.Data.GetValue(DataAttributeNames.LocalAuthorityCode)),
                ManagementGroupType = ValueToArray(entity.Data.GetValue(DataAttributeNames.ManagementGroupType)),
                ManagementGroupId = ValueToArray(entity.Data.GetValue(DataAttributeNames.ManagementGroupId)),
            };
        }

        private static string[] ValueToArray(string value)
        {
            return string.IsNullOrEmpty(value)
                ? new string[0]
                : new[] {value};
        }

        private static DateTime[] ValueToArray(DateTime? value)
        {
            return !value.HasValue
                ? new DateTime[0]
                : new[] {value.Value};
        }

        private static long[] ValueToArray(long? value)
        {
            return !value.HasValue
                ? new long[0]
                : new[] {value.Value};
        }
    }
}