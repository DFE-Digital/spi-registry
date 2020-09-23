using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Cosmos;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosDbRepositoryTests
{
    public class StaticFeedResponse<T> : FeedResponse<T>
    {
        private readonly IEnumerable<T> _items;

        public StaticFeedResponse(params T[] items)
        {
            _items = items;
        }
        public override Headers Headers { get; }
        public override IEnumerable<T> Resource { get; }
        public override HttpStatusCode StatusCode { get; }
        public override CosmosDiagnostics Diagnostics { get; }
        public override IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public override string ContinuationToken { get; }
        public override int Count { get; }
    }
}