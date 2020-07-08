using System;

namespace Dfe.Spi.Registry.Domain
{
    public class Entity : EntityPointer
    {
        public string Name {get;set;}
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Status { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public long? Urn { get; set; }
        public long? Ukprn { get; set; }
        public string Uprn { get; set; }
        public string CompaniesHouseNumber { get; set; }
        public string CharitiesCommissionNumber { get; set; }
        public string AcademyTrustCode { get; set; }
        public string DfeNumber { get; set; }
        public string LocalAuthorityCode { get; set; }
        public string ManagementGroupType { get; set; }
        public string ManagementGroupId { get; set; }
        public string ManagementGroupCode { get; set; }
        public long? ManagementGroupUkprn { get; set; }
        public string ManagementGroupCompaniesHouseNumber { get; set; }
    }
}