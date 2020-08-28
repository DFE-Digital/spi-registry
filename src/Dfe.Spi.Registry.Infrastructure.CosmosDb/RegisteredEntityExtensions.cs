using System;
using System.Linq;
using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    internal static class RegisteredEntityExtensions
    {
        internal static string[] GetSearchableValues(this RegisteredEntity registeredEntity, Func<Entity, string> selector)
        {
            return registeredEntity.Entities
                .Select(selector)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.ToLower())
                .Distinct()
                .ToArray();
        }

        internal static long[] GetSearchableValues(this RegisteredEntity registeredEntity, Func<Entity, long?> selector)
        {
            return registeredEntity.Entities
                .Select(selector)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .ToArray();
        }

        internal static DateTime[] GetSearchableValues(this RegisteredEntity registeredEntity, Func<Entity, DateTime?> selector)
        {
            return registeredEntity.Entities
                .Select(selector)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .ToArray();
        }
    }
}