using System;

namespace Dfe.Spi.Registry.Application.Sync
{
    public class SyncEntityEvent<T> where T: Models.Entities.EntityBase
    {
        public T Details { get; set; }
        public DateTime PointInTime { get; set; }
    }
}