using Dfe.Spi.Common.Models;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenTakingResultsBetween
    {
        [Test]
        public void ThenItShouldAddOffsetAndLimitToQuery()
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "some-thing")
                .TakeResultsBetween(10, 50);

            var expectedOffsetLimit = $"OFFSET 10 LIMIT 50";
            Assert.AreEqual(expectedOffsetLimit, query.ToString().Substring(query.ToString().Length - expectedOffsetLimit.Length));
        }
    }
}