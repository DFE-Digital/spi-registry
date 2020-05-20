using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain.Entities;
using Dfe.Spi.Registry.Domain.Search;
using Meridian.MeaningfulToString;

namespace Dfe.Spi.Registry.Infrastructure.SqlServer
{
    public class SqlServerSpikeRepository
    {
        private string _connectionString;

        public SqlServerSpikeRepository()
        {
            _connectionString = Environment.GetEnvironmentVariable("SPI_Sql__ConnectionString");
        }

        public async Task<SynonymousEntitiesSearchResult> SearchAsync(SearchRequest criteria, string entityType, CancellationToken cancellationToken)
        {

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                var count = await GetTotalNumberOfRecords(connection, criteria, entityType);
                var results = await GetPageOfData(connection, criteria, entityType);

                return new SynonymousEntitiesSearchResult
                {
                    Results = results,
                    Skipped = criteria.Skip,
                    Taken = results.Length,
                    TotalNumberOfRecords = count,
                };
            }
        }


        private async Task<int> GetTotalNumberOfRecords(SqlConnection connection, SearchRequest criteria, string entityType)
        {
            var countQuery = GetCountQuery(criteria, entityType);
            var count = await connection.ExecuteScalarAsync<int>(countQuery);
            return count;
        }
        private async Task<SynonymousEntities[]> GetPageOfData(SqlConnection connection, SearchRequest criteria, string entityType)
        {
            var dataQuery = GetPagedDataQuery(criteria, entityType);
            var page = await connection.QueryAsync<SearchResultEntity>(dataQuery);
            
            var results = new List<SynonymousEntities>();
            var currentPointerId = Guid.Empty;
            SynonymousEntities currentResult = null;
            foreach (var searchResultEntity in page)
            {
                // Check if we are on a new result
                if (currentPointerId != searchResultEntity.PointerId)
                {
                    currentResult = new SynonymousEntities
                    {
                        Entities = new EntityPointer[0],
                        IndexedData = new Dictionary<string, string>(),
                    };
                    currentPointerId = searchResultEntity.PointerId;
                    results.Add(currentResult);
                }

                // Check if we need to added the synonym
                var referenceAlreadyAdded = currentResult.Entities.Any(e =>
                    e.SourceSystemName.Equals(searchResultEntity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                    e.SourceSystemId.Equals(searchResultEntity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase));
                if (!referenceAlreadyAdded)
                {
                    currentResult.Entities =
                        currentResult.Entities
                            .Concat(new[]
                            {
                                new EntityPointer
                                {
                                    SourceSystemName = searchResultEntity.SourceSystemName,
                                    SourceSystemId = searchResultEntity.SourceSystemId,
                                },
                            })
                            .ToArray();
                }
                
                // Check if we need to add data
                var resultForFirstReference =
                    currentResult.Entities[0].SourceSystemName.Equals(searchResultEntity.SourceSystemName, StringComparison.InvariantCultureIgnoreCase) &&
                    currentResult.Entities[0].SourceSystemId.Equals(searchResultEntity.SourceSystemId, StringComparison.InvariantCultureIgnoreCase);
                if (resultForFirstReference)
                {
                    currentResult.IndexedData.Add(searchResultEntity.AttributeName, searchResultEntity.AttributeValue);
                }
            }

            return results.ToArray();
        }
        private string GetPagedDataQuery(SearchRequest criteria, string entityType)
        {
            var resultsQuery = GetResultsQuery(criteria, entityType);
            var pagesResultsQuery = $"{resultsQuery} ORDER BY COALESCE(synonyms.LinkId, results.EntityId) " +
                                    $"OFFSET {criteria.Skip} ROWS FETCH NEXT {criteria.Take} ROWS ONLY";
            var linkedQuery =
                "SELECT le.LinkId, le.EntityId, e.EntityType, e.SourceSystemName, e.SourceSystemId " +
                "FROM Registry.LinkedEntity le " +
                "JOIN Registry.Entity e ON le.EntityId = e.Id";
            return "SELECT " +
                "paged.PointerId," +
                // "paged.PointerType," +
                // "linked.EntityId EntityId," +
                // "COALESCE(linked.EntityType, main.EntityType) EntityType," +
                "COALESCE(linked.SourceSystemName, main.SourceSystemName) SourceSystemName," +
                "COALESCE(linked.SourceSystemId, main.SourceSystemId) SourceSystemId," +
                "ea.AttributeName," +
                "ea.AttributeValue " +
                $"FROM ({pagesResultsQuery}) paged " +
                $"LEFT JOIN ({linkedQuery}) linked ON paged.PointerId = linked.LinkId AND paged.PointerType = 'link' " +
                "JOIN Registry.Entity main ON paged.EntityId = main.Id " +
                "JOIN Registry.EntityAttribute ea ON main.Id = ea.EntityId " +
                "ORDER BY paged.PointerId, linked.EntityId";
        }
        private string GetCountQuery(SearchRequest criteria, string entityType)
        {
            var resultsQuery = GetResultsQuery(criteria, entityType);
            return $"SELECT COUNT(1) FROM ({resultsQuery}) results";
        }
        private string GetResultsQuery(SearchRequest criteria, string entityType)
        {
            var searchQuery = GetSearchQuery(criteria, entityType);
            var synonymsQuery = "SELECT le.LinkId, le.EntityId " +
                                "FROM Registry.LinkedEntity le " +
                                "JOIN Registry.Link l ON le.LinkId = l.Id " +
                                "WHERE l.LinkType = 'Synonym'";
            return "SELECT COALESCE(synonyms.LinkId, results.EntityId) PointerId, " +
                   "IIF(synonyms.LinkId IS NOT NULL, 'link', 'entity') PointerType, " +
                   "MIN(results.EntityId) EntityId " +
                   $"FROM ({searchQuery}) results " +
                   $"LEFT JOIN ({synonymsQuery}) synonyms ON results.EntityId = synonyms.EntityId " +
                   "GROUP BY COALESCE(synonyms.LinkId, results.EntityId), IIF(synonyms.LinkId IS NOT NULL, 'link', 'entity')";
        }
        private string GetSearchQuery(SearchRequest criteria, string entityType)
        {
            var groupQueries = criteria.Groups
                .Select(group => GetGroupQuery(group, entityType))
                .Aggregate((x, y) => $"{x} UNION ALL {y}");
            var fulfilmentCheck = criteria.CombinationOperator.Equals("and", StringComparison.InvariantCultureIgnoreCase)
                ? $" HAVING COUNT(1) = {criteria.Groups.Length}"
                : string.Empty;
            return "SELECT Entityid " +
                   $"FROM ({groupQueries}) groupresults " +
                   "GROUP BY EntityId" +
                   fulfilmentCheck;
        }
        private string GetGroupQuery(SearchGroup group, string entityType)
        {
            var filterQueries = group.Filter
                .Select(filter => $"({GetFilterQuery(filter)})")
                .Aggregate((x, y) => $"{x} OR {y}");
            var fulfilmentCheck = group.CombinationOperator.Equals("and", StringComparison.InvariantCultureIgnoreCase)
                ? $" HAVING COUNT(1) = {group.Filter.Length}"
                : string.Empty;
            return "SELECT EntityId " +
                   "FROM Registry.EntityAttribute sea " +
                   $"JOIN Registry.Entity se ON sea.EntityId = se.Id AND se.EntityType = '{entityType}'" +
                   $"WHERE {filterQueries} " +
                   $"GROUP BY EntityId" +
                   fulfilmentCheck;
        }
        private string GetFilterQuery(DataFilter filter)
        {
            var valueCondition = $"AttributeValue = '{filter.Value}'"; // TODO: Handle all filter operators
            return $"AttributeName = '{filter.Field}' AND {valueCondition}";
        }
    }

    public class SearchResultEntity
    {
        public Guid PointerId { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeValue { get; set; }
    }
}