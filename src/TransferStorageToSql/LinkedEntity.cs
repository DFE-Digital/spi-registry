using System;

namespace TransferStorageToSql
{
    class LinkedEntity
    {
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedReason { get; set; }
        
        public string EntityType { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
    }
}