using System;
using System.Linq;
using System.Text;
using Dfe.Spi.Common.Models;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenAddingNumericConditions
    {
        [TestCase("searchableUrn", "12345")]
        public void ThenItShouldAppendEqualsConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.Equals, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_CONTAINS(re.{field}, {value})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn", "12345")]
        public void ThenItShouldAppendGreaterThanConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.GreaterThan, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v > {value})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn", "12345")]
        public void ThenItShouldAppendGreaterThanOrEqualToConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.GreaterThanOrEqualTo, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v >= {value})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn", "12345")]
        public void ThenItShouldAppendLessThanConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.LessThan, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v < {value})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn", "12345")]
        public void ThenItShouldAppendLessThanOrEqualToConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.LessThanOrEqualTo, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v <= {value})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn")]
        public void ThenItShouldAppendIsNullConditionsToQuery(string field)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.IsNull, null);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_LENGTH(re.{field}) = 0", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn")]
        public void ThenItShouldAppendIsNotNullConditionsToQuery(string field)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.IsNotNull, null);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_LENGTH(re.{field}) > 0", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn", 12345, 1300)]
        public void ThenItShouldAppendBetweenConditionsToQuery(string field, long lowerBound, long upperBound)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.Between, $"{lowerBound} to {upperBound}");

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v >= {lowerBound} AND v <= {upperBound})",
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableUrn", 12345, 65432)]
        public void ThenItShouldAppendInConditionsToQuery(string field, params long[] values)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.In, values.Select(v => v.ToString()).Aggregate((x, y) => $"{x}, {y}"));

            var expectedGroupConditions = new StringBuilder();
            foreach (var value in values)
            {
                if (expectedGroupConditions.Length > 0)
                {
                    expectedGroupConditions.Append(" OR ");
                }

                expectedGroupConditions.Append($"ARRAY_CONTAINS(re.{field}, {value})");
            }
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ({expectedGroupConditions})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
        
        [TestCase("searchableUrn", DataOperator.Contains, "some-name")]
        public void ThenItShouldThrowExceptionIfInvalidOperatorUsed(string field, DataOperator @operator, string value)
        {
            var actual = Assert.Throws<ArgumentException>(() =>
                new CosmosQuery(CosmosCombinationOperator.And)
                    .AddCondition(field, @operator, value));
            Assert.AreEqual($"Operator {@operator.ToString()} is not valid for field {field}. Valid operators are Between, Equals, GreaterThan, GreaterThanOrEqualTo, LessThan, LessThanOrEqualTo, In, IsNull and IsNotNull", actual.Message);
        }
    }
}