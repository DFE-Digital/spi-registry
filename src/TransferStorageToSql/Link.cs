using System.Collections.Generic;

namespace TransferStorageToSql
{
    class Link
    {
        public string Id { get; set; }
        public string LinkType { get; set; }
        public List<LinkedEntity> LinkedEntities { get; set; } = new List<LinkedEntity>();
    }
}