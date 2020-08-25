using System;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenAddingPointInTimeConditions
    {
        [Test]
        public void ThenItShouldAppendEqualsConditionsToQuery()
        {
            var pointInTime = DateTime.SpecifyKind(new DateTime(2020, 8, 21), DateTimeKind.Utc);

            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddPointInTimeCondition(pointInTime);

            var expectedPointInTime = "2020-08-21T00:00:00Z";
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE (re.validFrom <= '{expectedPointInTime}' AND (ISNULL(re.validTo) OR re.validTo >= '{expectedPointInTime}'))", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
    }
}