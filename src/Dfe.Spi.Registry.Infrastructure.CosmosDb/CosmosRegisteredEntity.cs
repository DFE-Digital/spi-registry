using System;
using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    public class CosmosRegisteredEntity : RegisteredEntity
    {
        public string PartitionableId { get; set; }

        public string[] SearchableSourceSystemIdentifiers { get; set; }
        public string[] SearchableName { get; set; }
        public string[] SearchableType { get; set; }
        public string[] SearchableSubType { get; set; }
        public string[] SearchableStatus { get; set; }
        public DateTime[] SearchableOpenDate { get; set; }
        public DateTime[] SearchableCloseDate { get; set; }
        public long[] SearchableUrn { get; set; }
        public long[] SearchableUkprn { get; set; }
        public string[] SearchableUprn { get; set; }
        public string[] SearchableCompaniesHouseNumber { get; set; }
        public string[] SearchableCharitiesCommissionNumber { get; set; }
        public string[] SearchableAcademyTrustCode { get; set; }
        public string[] SearchableDfeNumber { get; set; }
        public string[] SearchableLocalAuthorityCode { get; set; }
        public string[] SearchableManagementGroupType { get; set; }
        public string[] SearchableManagementGroupId { get; set; }
        public string[] SearchableManagementGroupCode { get; set; }
        public long[] SearchableManagementGroupUkprn { get; set; }
        public string[] SearchableManagementGroupCompaniesHouseNumber { get; set; }

        public string _ETag
        {
            get { return ETag; }
            set { ETag = value; }
        }
    }
}