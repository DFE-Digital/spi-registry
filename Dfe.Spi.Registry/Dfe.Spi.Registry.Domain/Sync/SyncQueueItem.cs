using System;

namespace Dfe.Spi.Registry.Domain.Sync
{
    public class SyncQueueItem
    {
        public Entity Entity { get; set; }
        public DateTime PointInTime { get; set; }
    }
}