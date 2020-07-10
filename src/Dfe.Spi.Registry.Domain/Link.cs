using System;

namespace Dfe.Spi.Registry.Domain
{
    public class Link : EntityPointer
    {
        public string LinkType { get; set; }

        public string LinkedBy { get; set; }

        public string LinkedReason { get; set; }
        
        public DateTime LinkedAt { get; set; }
    }
}