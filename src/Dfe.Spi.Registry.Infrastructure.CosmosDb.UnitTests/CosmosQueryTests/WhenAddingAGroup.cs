using Dfe.Spi.Common.Models;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenAddingAGroup
    {
        [Test]
        public void ThenItShouldAddGroupConditionsInParentheses()
        {
            var group = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "some-thing")
                .AddCondition("Status", DataOperator.Equals, "Open");

            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddGroup(group);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ({CosmosQueryTestConstants.QueryWhereClause(group)})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
    }
}