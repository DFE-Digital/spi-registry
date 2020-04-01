using System;

namespace Dfe.Spi.Registry.Domain.Search
{
    public class SearchDocument
    {
        public virtual string Id { get; set; }
        
        public virtual string EntityType { get; set; }
        
        public virtual string ReferencePointer { get; set; }
        
        public virtual string SortableEntityName { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.Searchable)]
        public virtual string[] Name { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.Enum)]
        public virtual string[] Type { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.Enum)]
        public virtual string[] SubType { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.Enum)]
        public virtual string[] Status { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.Date)]
        public virtual DateTime[] OpenDate { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.Date)]
        public virtual DateTime[] CloseDate { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual long[] Urn { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual long[] Ukprn { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual string[] Uprn { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual string[] CompaniesHouseNumber { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual string[] CharitiesCommissionNumber { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual string[] AcademyTrustCode { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual string[] DfeNumber { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual string[] LocalAuthorityCode { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.Enum)]
        public virtual string[] ManagementGroupType { get; set; }
        
        [SearchFieldTypeAttribute(SearchFieldType.String)]
        public virtual string[] ManagementGroupId { get; set; }
    }
}