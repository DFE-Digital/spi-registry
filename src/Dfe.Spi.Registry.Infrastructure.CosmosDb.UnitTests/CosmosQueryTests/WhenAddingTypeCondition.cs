using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenAddingTypeCondition
    {
        [TestCase("Learning-Provider")]
        [TestCase("Management-Group")]
        public void ThenItShouldAppendEqualsConditionsToQuery(string type)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddTypeCondition(type);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE re.type = '{type.ToLower()}'", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
    }
}