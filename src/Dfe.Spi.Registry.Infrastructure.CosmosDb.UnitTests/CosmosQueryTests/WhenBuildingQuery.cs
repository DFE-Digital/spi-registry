using System;
using Dfe.Spi.Common.Models;
using NUnit.Framework;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb.UnitTests.CosmosQueryTests
{
    public class WhenBuildingQuery
    {
        [TestCase("Name", "searchableName", "some-name", true, true)]
        [TestCase("Type", "searchableType", "type", true, true)]
        [TestCase("SubType", "searchableSubType", "sub-type", true, true)]
        [TestCase("Status", "searchableStatus", "Open", true, true)]
        [TestCase("OpenDate", "searchableOpenDate", "2020-07-21T00:00:00", true, false)]
        [TestCase("CloseDate", "searchableCloseDate", "2020-08-21T00:00:00", true, false)]
        [TestCase("Urn", "searchableUrn", "12345", false, true)]
        [TestCase("Ukprn", "searchableUkprn", "12345678", false, true)]
        [TestCase("Uprn", "searchableUprn", "6325", true, true)]
        [TestCase("CompaniesHouseNumber", "searchableCompaniesHouseNumber", "012345678", true, true)]
        [TestCase("CharitiesCommissionNumber", "searchableCharitiesCommissionNumber", "951357", true, true)]
        [TestCase("AcademyTrustCode", "searchableAcademyTrustCode", "3698", true, true)]
        [TestCase("DfeNumber", "searchableDfeNumber", "852-123", true, true)]
        [TestCase("LocalAuthorityCode", "searchableLocalAuthorityCode", "852", true, true)]
        [TestCase("ManagementGroupType", "searchableManagementGroupType", "type", true, true)]
        [TestCase("ManagementGroupId", "searchableManagementGroupId", "987", true, true)]
        [TestCase("ManagementGroupCode", "searchableManagementGroupCode", "type-123", true, true)]
        [TestCase("ManagementGroupUkprn", "searchableManagementGroupUkprn", "87654321", false, true)]
        [TestCase("ManagementGroupCompaniesHouseNumber", "searchableManagementGroupCompaniesHouseNumber", "087654321", true, true)]
        public void ThenItShouldMapEntityPropertyNamesToSearchableNames(string field, string searchableField, string value, bool expectValueQuoted, bool expectValueLowercased)
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition(field, DataOperator.Equals, value);

            var expectedValue = expectValueQuoted 
                ? (expectValueLowercased ? $"'{value.ToLower()}'" : $"'{value}'") 
                : value;
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_CONTAINS(re.{searchableField}, {expectedValue})", 
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [Test]
        public void ThenItShouldPrefixQueryWithSelect()
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "some-thing");
            
            Assert.AreEqual(CosmosQueryTestConstants.QueryPrefix, query.ToString().Substring(0, CosmosQueryTestConstants.QueryPrefix.Length));
        }

        [Test]
        public void ThenItShouldPrefixCountQueryWithSelectCount()
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "some-thing");
            
            Assert.AreEqual(CosmosQueryTestConstants.CountQueryPrefix, query.ToString(true).Substring(0, CosmosQueryTestConstants.CountQueryPrefix.Length));
        }

        [Test]
        public void ThenItShouldNotOrderByForCountQuery()
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "some-thing");

            var orderByIndex = query.ToString(true).IndexOf(CosmosQueryTestConstants.OrderBy, StringComparison.InvariantCultureIgnoreCase);
            Assert.AreEqual(-1, orderByIndex);
        }

        [Test]
        public void ThenItShouldNotOffsetLimitForCountQuery()
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "some-thing")
                .TakeResultsBetween(10, 50);

            var offsetLimitIndex = query.ToString(true).IndexOf("OFFSET 10 LIMIT 50", StringComparison.InvariantCultureIgnoreCase);
            Assert.AreEqual(-1, offsetLimitIndex);
        }

        [TestCase("searchableUrn", "123")]
        [TestCase("searchableUrn", "123-456")]
        [TestCase("searchableOpenDate", "123")]
        [TestCase("searchableOpenDate", "123-456")]
        public void ThenItShouldThrowExceptionIfBetweenConditionDoesNotHaveValidValue(string field, string value)
        {
            var actual = Assert.Throws<ArgumentException>(() =>
                new CosmosQuery(CosmosCombinationOperator.And)
                    .AddCondition(field, DataOperator.Between, value));
            Assert.AreEqual($"A between query must be in the format of '{{lower-bound}} to {{upper-bound}}', but received {value}", actual.Message);
        }

        [TestCase(CosmosCombinationOperator.And, "AND")]
        [TestCase(CosmosCombinationOperator.Or, "OR")]
        public void ThenItShouldCombineMultipleConditions(CosmosCombinationOperator @operator, string expectedStringOperator)
        {
            var query = new CosmosQuery(@operator)
                .AddCondition("Name", DataOperator.Equals, "some-thing")
                .AddCondition("Status", DataOperator.Equals, "Open");
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ARRAY_CONTAINS(re.searchableName, 'some-thing') {expectedStringOperator} ARRAY_CONTAINS(re.searchableStatus, 'open')", 
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase(CosmosCombinationOperator.And, "AND")]
        [TestCase(CosmosCombinationOperator.Or, "OR")]
        public void ThenItShouldCombineMultipleGroups(CosmosCombinationOperator @operator, string expectedStringOperator)
        {
            var group1 = new CosmosQuery(CosmosCombinationOperator.Or)
                .AddCondition("Name", DataOperator.Equals, "some-thing")
                .AddCondition("Status", DataOperator.Equals, "Open");
            var group2 = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "another-thing")
                .AddCondition("Status", DataOperator.Equals, "Closed");

            var query = new CosmosQuery(@operator)
                .AddGroup(group1)
                .AddGroup(group2);
            
            Assert.AreEqual($"{CosmosQueryTestConstants.QueryPrefix} WHERE ({CosmosQueryTestConstants.QueryWhereClause(group1)}) {expectedStringOperator} ({CosmosQueryTestConstants.QueryWhereClause(group2)})", 
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [TestCase(CosmosCombinationOperator.And, "AND")]
        [TestCase(CosmosCombinationOperator.Or, "OR")]
        public void ThenItShouldCombineMultipleGroupsAndConditions(CosmosCombinationOperator @operator, string expectedStringOperator)
        {
            var group1 = new CosmosQuery(CosmosCombinationOperator.Or)
                .AddCondition("Name", DataOperator.Equals, "some-thing")
                .AddCondition("Status", DataOperator.Equals, "Open");
            var group2 = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "another-thing")
                .AddCondition("Status", DataOperator.Equals, "Closed");

            var query = new CosmosQuery(@operator)
                .AddGroup(group1)
                .AddGroup(group2)
                .AddCondition("Urn", DataOperator.IsNotNull, null);
            
            Assert.AreEqual(
                $"{CosmosQueryTestConstants.QueryPrefix} WHERE ({CosmosQueryTestConstants.QueryWhereClause(group1)}) {expectedStringOperator} ({CosmosQueryTestConstants.QueryWhereClause(group2)}) {expectedStringOperator} ARRAY_LENGTH(re.searchableUrn) > 0", 
                CosmosQueryTestConstants.QueryWithoutOrderByOrSkip(query));
        }

        [Test]
        public void ThenItShouldHaveAConsistentOrder()
        {
            var query = new CosmosQuery(CosmosCombinationOperator.And)
                .AddCondition("Name", DataOperator.Equals, "some-thing");

            var queryOrderBy = query.ToString().Substring(query.ToString().Length - CosmosQueryTestConstants.OrderBy.Length);
            Assert.AreEqual(CosmosQueryTestConstants.OrderBy, queryOrderBy);
        }
    }
}