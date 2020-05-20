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
            var query = GetResultsQuery(criteria, entityType);
            var countQuery = GetCountQuery(criteria, entityType);
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                var searchResults = await connection.QueryAsync<SearchResultEntity>(query);
                var totalNumberOfRecords = await connection.ExecuteScalarAsync<int>(countQuery, cancellationToken);
                
                var results = new List<SynonymousEntities>();
                SynonymousEntities current = null;
                string currentPointer = null;
                foreach (var searchResultEntity in searchResults)
                {
                    if (currentPointer == null || searchResultEntity.PointerId != currentPointer)
                    {
                        current = new SynonymousEntities
                        {
                            IndexedData = new Dictionary<string, string>(),
                        };
                        currentPointer = searchResultEntity.PointerId;
                        results.Add(current);

                        if (searchResultEntity.PointerId.StartsWith("link:"))
                        {
                            var linkId = searchResultEntity.PointerId.Substring(5);
                            current.Entities = await GetLinkedEntitiesAsync(connection, linkId, cancellationToken);
                        }
                        else
                        {
                            current.Entities = new[]
                            {
                                new EntityPointer
                                {
                                    SourceSystemName = searchResultEntity.SourceSystemName,
                                    SourceSystemId = searchResultEntity.SourceSystemId,
                                },
                            };
                        }
                    }
                    
                    current.IndexedData.Add(searchResultEntity.AttributeName, searchResultEntity.AttributeValue);
                }
                
                return new SynonymousEntitiesSearchResult
                {
                    Results = results.ToArray(),
                    Skipped = criteria.Skip,
                    Taken = results.Count,
                    TotalNumberOfRecords = totalNumberOfRecords,
                };
            }
        }


        private string GetResultsQuery(SearchRequest criteria, string entityType)
        {
            var pagesSearchQuery = GetPagedSearchQuery(criteria, entityType);
            return "SELECT re.PointerId, e.SourceSystemName, e.SourceSystemId, ea.AttributeName, ea.AttributeValue " +
                   $"FROM ({pagesSearchQuery}) re " +
                   "JOIN Registry.EntityAttribute ea ON re.MinEntityId = ea.EntityId " +
                   "JOIN Registry.Entity e ON re.MinEntityId = e.Id " +
                   "ORDER BY re.PointerId";
        }
        private string GetCountQuery(SearchRequest criteria, string entityType)
        {
            var searchQuery = GetSearchQuery(criteria, entityType);
            return "SELECT COUNT(DISTINCT PointerId) " +
                   $"FROM ({searchQuery}) res";
        }
        private string GetPagedSearchQuery(SearchRequest criteria, string entityType)
        {
            var searchQuery = GetSearchQuery(criteria, entityType);
            return "SELECT res.PointerId, MIN(res.EntityId) MinEntityId " +
                   $"FROM ({searchQuery}) res " +
                   "GROUP BY res.PointerId " +
                   $"ORDER BY res.PointerId OFFSET {criteria.Skip} ROWS FETCH NEXT {criteria.Take} ROWS ONLY";
        }
        private string GetSearchQuery(SearchRequest criteria, string entityType)
        {
            var query = new StringBuilder(
                "SELECT CASE " +
                "WHEN ls.LinkId IS NOT NULL THEN 'link:' + CAST(ls.LinkId as char(36)) " +
                "ELSE 'entity:' + CAST(e.Id as char(36)) " + 
                "END PointerId, " +
                "e.Id EntityId " +
                "FROM Registry.Entity e ");
            var isAnd = criteria.CombinationOperator.Equals("and", StringComparison.InvariantCultureIgnoreCase);
            var joinType = isAnd ? "JOIN" : "LEFT JOIN";
            var orCondition = isAnd ? null : new StringBuilder();

            for (var i = 0; i < criteria.Groups.Length; i++)
            {
                var groupQuery = GetGroupQuery(criteria.Groups[i], i, entityType);
                query.Append($"{joinType} ({groupQuery}) g{i} ON e.Id = g{i}.Id ");

                if (orCondition != null)
                {
                    if (orCondition.Length > 0)
                    {
                        orCondition.Append("OR ");
                    }

                    orCondition.Append($"g{i}.EntityId IS NOT NULL");
                }
            }

            query.Append($"LEFT JOIN ({GetSynonymsQuery()}) ls ON ls.EntityId = e.Id");
            if (!isAnd)
            {
                query.Append($" AND ({orCondition})");
            }

            return query.ToString();
        }
        private string GetSynonymsQuery()
        {
            return "SELECT l.Id as LinkId, le.EntityId " +
                   "FROM Registry.Link l " +
                   "JOIN Registry.LinkedEntity le ON l.Id = le.LinkId " +
                   "WHERE l.LinkType = 'Synonym'";
        }
        private string GetGroupQuery(SearchGroup group, int groupIndex, string entityType)
        {
            var query = new StringBuilder(
                $"SELECT g{groupIndex}e.Id " +
                $"FROM Registry.Entity g{groupIndex}e ");
            var isAnd = group.CombinationOperator.Equals("and", StringComparison.InvariantCultureIgnoreCase);
            var joinType = isAnd ? "JOIN" : "LEFT JOIN";
            var orCondition = isAnd ? null : new StringBuilder();

            for (var i = 0; i < group.Filter.Length; i++)
            {
                var filterQuery = GetFilterQuery(group.Filter[i]);
                query.Append($"{joinType} ({filterQuery}) c{i} ON g{groupIndex}e.Id = c{i}.EntityId ");

                if (orCondition != null)
                {
                    if (orCondition.Length > 0)
                    {
                        orCondition.Append("OR ");
                    }

                    orCondition.Append($"c{i}.EntityId IS NOT NULL ");
                }
            }

            query.Append($"WHERE g{groupIndex}e.EntityType = '{entityType}'");
            if (!isAnd)
            {
                query.Append($" AND ({orCondition})");
            }

            return query.ToString();
        }
        private string GetFilterQuery(DataFilter filter)
        {
            string condition;
            switch (filter.Operator)
            {
                case DataOperator.Between:
                    var dateParts = filter.Value.Split(
                        new string[] {" to "},
                        StringSplitOptions.RemoveEmptyEntries);

                    condition = $"AttributeValue BETWEEN '{dateParts.First()} AND {dateParts.Last()}'";
                    break;
                default:
                    condition = $"AttributeValue = '{filter.Value}'";
                    break;
            }

            return "SELECT EntityId " +
                   "FROM Registry.EntityAttribute " +
                   $"WHERE AttributeName = '{filter.Field}'" +
                   $"AND {condition}";
        }

        private async Task<EntityPointer[]> GetLinkedEntitiesAsync(SqlConnection connection, string linkId, CancellationToken cancellationToken)
        {
            var query = "SELECT e.EntityType, e.SourceSystemName, e.SourceSystemId " +
                        "FROM Registry.LinkedEntity le " +
                        "JOIN Registry.Entity e ON le.EntityId = e.Id " +
                        "WHERE le.LinkId = @LinkId";
            var results = await connection.QueryAsync<EntityPointer>(query, param: new {linkId});
            return results.ToArray();
        }
    }
        
    public class SearchResultEntity
    {
        public string PointerId { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeValue { get; set; }
    }
}