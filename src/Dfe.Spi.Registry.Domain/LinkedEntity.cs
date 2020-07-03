using System;

namespace Dfe.Spi.Registry.Domain
{
    public class LinkedEntity : Entity
    {
        public string LinkType { get; set; }

        public string LinkedBy { get; set; }

        public string LinkedReason { get; set; }
        
        public DateTime? LinkedAt { get; set; }
    }
}