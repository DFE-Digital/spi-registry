using System;
using System.Linq;
using System.Text;
using Dfe.Spi.Common.Models;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenAddingStringConditions
    {
        [TestCase("searchableName", "some-name")]
        public void ThenItShouldAppendEqualsConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.Equals, value);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_CONTAINS(re.{field}, '{value}')", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
        
        [TestCase("searchableName", "some-name")]
        public void ThenItShouldAppendContainsConditionsToQuery(string field, string value)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.Contains, value);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE EXISTS (SELECT VALUE v FROM v IN re.{field} WHERE CONTAINS(v, '{value}'))", 
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
        
        [TestCase("searchableName")]
        public void ThenItShouldAppendIsNullConditionsToQuery(string field)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.IsNull, null);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_LENGTH(re.{field}) = 0", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
        
        [TestCase("searchableName")]
        public void ThenItShouldAppendIsNotNullConditionsToQuery(string field)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And);

            query.AddCondition(field, DataOperator.IsNotNull, null);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_LENGTH(re.{field}) > 0", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase("searchableName", "some-name", "some-other-name")]
        public void ThenItShouldAppendInConditionsToQuery(string field, params string[] values)
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

                expectedGroupConditions.Append($"ARRAY_CONTAINS(re.{field}, '{value}')");
            }
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ({expectedGroupConditions})", CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }
        
        [TestCase("searchableName", DataOperator.GreaterThan, "some-name")]
        [TestCase("searchableName", DataOperator.GreaterThanOrEqualTo, "some-name")]
        [TestCase("searchableName", DataOperator.LessThan, "some-name")]
        [TestCase("searchableName", DataOperator.LessThanOrEqualTo, "some-name")]
        [TestCase("searchableName", DataOperator.Between, "some-name")]
        public void ThenItShouldThrowExceptionIfInvalidOperatorUsed(string field, DataOperator @operator, string value)
        {
            var actual = Assert.Throws<ArgumentException>(() =>
                new CosmosQuery(CosmosCombinationOperator.And)
                    .AddCondition(field, @operator, value));
            Assert.AreEqual($"Operator {@operator.ToString()} is not valid for field {field}. Valid operators are Equals, Contains, In, IsNull and IsNotNull", actual.Message);
        }
    }
}