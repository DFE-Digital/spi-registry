using System;

namespace Dfe.Spi.Registry.Domain
{
    public class RegisteredEntity
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public LinkedEntity[] Entities { get; set; }
        public Link[] Links { get; set; }
        
        public string ETag { get; set; }
    }
}