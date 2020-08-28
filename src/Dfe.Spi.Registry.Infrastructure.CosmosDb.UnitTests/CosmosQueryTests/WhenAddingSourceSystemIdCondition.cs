using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenAddingSourceSystemIdCondition
    {
        [TestCase("GIAS", "12345")]
        public void ThenItShouldAppendEqualsConditionsToQuery(string sourceSystemName, string sourceSystemId)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddSourceSystemIdCondition(sourceSystemName, sourceSystemId);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_CONTAINS(re.searchableSourceSystemIdentifiers, '{sourceSystemName.ToLower()}:{sourceSystemId.ToLower()}')", 
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
    }
}