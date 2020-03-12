using System;
using Dfe.Spi.Registry.Domain.Search;
using Microsoft.Azure.Search;

namespace Dfe.Spi.Registry.Infrastructure.AzureCognitiveSearch
{
    public class AcsSearchDocument : SearchDocument
    {
        [System.ComponentModel.DataAnnotations.Key]
        public override string Id { get; set; }
        
        [IsSortable]
        public override string SortableEntityName { get; set; }
        
        [IsFilterable, IsSortable]
        public override string EntityType { get; set; }
        
        [IsFilterable, IsSortable]
        public override string ReferencePointer { get; set; }
        
        [IsFilterable, IsSortable, IsSearchable]
        public override string[] Name { get; set; }
        
        [IsFilterable]
        public override string[] Type { get; set; }
        
        [IsFilterable]
        public override string[] SubType { get; set; }
        
        [IsFilterable]
        public override DateTime[] OpenDate { get; set; }
        
        [IsFilterable]
        public override DateTime[] CloseDate { get; set; }
        
        [IsFilterable]
        public override long[] Urn { get; set; }
        
        [IsFilterable]
        public override long[] Ukprn { get; set; }
        
        [IsFilterable]
        public override string[] Uprn { get; set; }
        
        [IsFilterable]
        public override string[] CompaniesHouseNumber { get; set; }
        
        [IsFilterable]
        public override string[] CharitiesCommissionNumber { get; set; }
        
        [IsFilterable]
        public override string[] AcademyTrustCode { get; set; }
        
        [IsFilterable]
        public override string[] DfeNumber { get; set; }
        
        [IsFilterable]
        public override string[] LocalAuthorityCode { get; set; }
        
        [IsFilterable]
        public override string[] ManagementGroupType { get; set; }
        
        [IsFilterable]
        public override string[] ManagementGroupId { get; set; }
    }
}