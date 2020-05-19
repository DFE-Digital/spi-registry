using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace TransferStorageToSql
{
    class StorageReader
    {
        private readonly ILoggerWrapper _logger;
        private CloudTable _entitiesTable;
        private CloudTable _linksTable;

        public StorageReader(string connectionString, ILoggerWrapper logger)
        {
            _logger = logger;
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _entitiesTable = tableClient.GetTableReference("entities");
            _linksTable = tableClient.GetTableReference("links");
        }

        public async Task<Entity[]> ReadAllEntitiesAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("Reading rows from table");
            var rows = await ReadAllQueryResults(_entitiesTable, new TableQuery<EntitiesEntity>(), cancellationToken);

            _logger.Debug($"Restructuring {rows.Length} rows to model");
            var orderedRows = rows
                .OrderBy(row => row.PartitionKey)
                .ThenBy(row => row.RowKey)
                .ToArray();
            var entities = new Dictionary<string, Entity>();
            
            foreach (var row in orderedRows)
            {
                if (string.IsNullOrEmpty(row.LinkId))
                {
                    var entity = new Entity
                    {
                        Type = row.Type,
                        SourceSystemName = row.SourceSystemName,
                        SourceSystemId = row.SourceSystemId,
                        Data = JsonConvert.DeserializeObject<Dictionary<string,string>>(row.Data),
                    };
                    entities.Add($"{row.Type}:{row.SourceSystemName}:{row.SourceSystemId}".ToLower(), entity);
                }
                else
                {
                    if (!entities.ContainsKey(row.PartitionKey.ToLower()))
                    {
                        throw new Exception($"Unable to find entity {row.PartitionKey} to add link pointer");
                    }

                    var entity = entities[row.PartitionKey.ToLower()];
                    entity.LinkPointers.Add(new LinkPointer
                    {
                        LinkType = row.LinkType,
                        LinkId = row.LinkId,
                    });
                }
            }

            return entities.Values.ToArray();
        }

        public async Task<Link[]> ReadAllLinks(CancellationToken cancellationToken)
        {
            _logger.Debug("Reading rows from table");
            var rows = await ReadAllQueryResults(_linksTable, new TableQuery<LinksEntity>(), cancellationToken);

            _logger.Debug($"Restructuring {rows.Length} rows to model");
            var orderedRows = rows
                .OrderBy(row => row.PartitionKey)
                .ThenBy(row => row.RowKey)
                .ToArray();
            var links = new Dictionary<string, Link>();
            
            foreach (var row in orderedRows)
            {
                var link = links.ContainsKey(row.LinkId.ToLower())
                    ? links[row.LinkId.ToLower()]
                    : null;
                if (link == null)
                {
                    link = new Link
                    {
                        Id = row.LinkId,
                        LinkType = row.LinkType,
                    };
                    links.Add(row.LinkId.ToLower(), link);
                }
                
                link.LinkedEntities.Add(new LinkedEntity
                {
                    CreatedBy = row.CreatedBy,
                    CreatedAt = row.CreatedAt,
                    CreatedReason = row.CreatedReason,
                    
                    EntityType = row.EntityType,
                    SourceSystemName = row.EntitySourceSystemName,
                    SourceSystemId = row.EntitySourceSystemId,
                });
            }

            return links.Values.ToArray();
        }
        
        private async Task<T[]> ReadAllQueryResults<T>(CloudTable table, TableQuery<T> query, CancellationToken cancellationToken)
            where T : TableEntity, new()
        {
            TableContinuationToken continuationToken = null;
            var allResults = new List<T>();
            
            do
            {
                _logger.Debug($"Reading next batch of rows. Read {allResults.Count} rows so far");
                var batchResults = await table.ExecuteQuerySegmentedAsync(query, continuationToken, cancellationToken);
                allResults.AddRange(batchResults.Results);

                continuationToken = batchResults.ContinuationToken;
            } while (continuationToken != null);

            return allResults.ToArray();
        }
        
        private class EntitiesEntity : TableEntity
        {
            public string Type { get; set; }
            public string SourceSystemName { get; set; }
            public string SourceSystemId { get; set; }
            public string Data { get; set; }
            public string LinkId { get; set; }
            public string LinkType { get; set; }
        }
        
        private class LinksEntity : TableEntity
        {
            public string LinkId { get; set; }
            public string LinkType { get; set; }
            public string EntityType { get; set; }
            public string EntitySourceSystemName { get; set; }
            public string EntitySourceSystemId { get; set; }
            public string CreatedBy { get; set; }
            public DateTime CreatedAt { get; set; }
            public string CreatedReason { get; set; }
        }
    }
}