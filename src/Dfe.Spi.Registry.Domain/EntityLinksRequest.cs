using System;

namespace Dfe.Spi.Registry.Domain
{
    public class EntityLinksRequest
    {
        public EntityPointer[] Entities { get; set; }
        public DateTime? PointInTime { get; set; }
    }

    public class EntityLinksResponse
    {
        public EntityLinksResult[] Entities { get; set; }
    }

    public class EntityLinksResult
    {
        public EntityPointer Entity { get; set; }
        public Link[] Links { get; set; }
    }
}