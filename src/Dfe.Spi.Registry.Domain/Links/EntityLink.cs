using System;

namespace Dfe.Spi.Registry.Domain.Links
{
    public class EntityLink
    {
        public string EntityType { get; set; }
        public string EntitySourceSystemName { get; set; }
        public string EntitySourceSystemId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedReason { get; set; }
    }
}