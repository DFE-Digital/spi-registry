using System;

namespace Dfe.Spi.Registry.Domain.Search
{
    public class SearchDocument
    {
        public virtual string Id { get; set; }
        public virtual string EntityType { get; set; }
        public virtual string ReferencePointer { get; set; }
        public virtual string SortableEntityName { get; set; }
        public virtual string[] Name { get; set; }
        public virtual string[] Type { get; set; }
        public virtual string[] SubType { get; set; }
        public virtual DateTime[] OpenDate { get; set; }
        public virtual DateTime[] CloseDate { get; set; }
        public virtual long[] Urn { get; set; }
        public virtual long[] Ukprn { get; set; }
        public virtual string[] Uprn { get; set; }
        public virtual string[] CompaniesHouseNumber { get; set; }
        public virtual string[] CharitiesCommissionNumber { get; set; }
        public virtual string[] AcademyTrustCode { get; set; }
        public virtual string[] DfeNumber { get; set; }
        public virtual string[] LocalAuthorityCode { get; set; }
        public virtual string[] ManagementGroupType { get; set; }
        public virtual string[] ManagementGroupId { get; set; }
    }
}