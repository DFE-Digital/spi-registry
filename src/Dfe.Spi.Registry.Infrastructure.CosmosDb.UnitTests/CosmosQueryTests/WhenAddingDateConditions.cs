using System;
using System.Linq;
using System.Text;
using Dfe.Spi.Common.Models;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenAddingDateConditions
    {
        [TestCase("searchableOpenDate", "2020-08-21")]
        public void ThenItShouldAppendEqualsConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.Equals, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_CONTAINS(re.{field}, '{value}T00:00:00')", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableOpenDate", "2020-08-21")]
        public void ThenItShouldAppendGreaterThanConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.GreaterThan, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v > '{value}T00:00:00')", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableOpenDate", "2020-08-21")]
        public void ThenItShouldAppendGreaterThanOrEqualToConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.GreaterThanOrEqualTo, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v >= '{value}T00:00:00')", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableOpenDate", "2020-08-21")]
        public void ThenItShouldAppendLessThanConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.LessThan, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v < '{value}T00:00:00')", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableOpenDate", "2020-08-21")]
        public void ThenItShouldAppendLessThanOrEqualToConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.LessThanOrEqualTo, value);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v <= '{value}T00:00:00')", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableOpenDate")]
        public void ThenItShouldAppendIsNullConditionsToQuery(string field)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.IsNull, null);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_LENGTH(re.{field}) = 0", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableOpenDate")]
        public void ThenItShouldAppendIsNotNullConditionsToQuery(string field)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.IsNotNull, null);

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_LENGTH(re.{field}) > 0", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableOpenDate", "2020-07-23", "2020-08-21")]
        public void ThenItShouldAppendBetweenConditionsToQuery(string field, string lowerBound, string upperBound)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.Between, $"{lowerBound} to {upperBound}");

            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE v >= '{lowerBound}T00:00:00' AND v <= '{upperBound}T00:00:00')",
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
        
        [TestCase("searchableOpenDate", DataOperator.Contains, "2020-08-21")]
        [TestCase("searchableOpenDate", DataOperator.In, "2020-08-21")]
        public void ThenItShouldThrowExceptionIfInvalidOperatorUsed(string field, DataOperator @operator, string value)
        {
            var actual = Assert.Throws<ArgumentException>(() =>
                new CosmosQuery(CosmosCombinationOperator.And)
                    .AddCondition(field, @operator, value));
            Assert.AreEqual($"Operator {@operator.ToString()} is not valid for field {field}. Valid operators are Between, Equals, GreaterThan, GreaterThanOrEqualTo, LessThan, LessThanOrEqualTo, IsNull and IsNotNull", actual.Message);
        }
    }
}