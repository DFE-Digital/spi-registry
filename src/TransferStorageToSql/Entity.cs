using System.Collections.Generic;

namespace TransferStorageToSql
{
    class Entity
    {
        public string Type { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public List<LinkPointer> LinkPointers { get; set; } = new List<LinkPointer>();
    }
}