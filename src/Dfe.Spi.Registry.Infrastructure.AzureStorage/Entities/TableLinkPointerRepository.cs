using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Entities;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities
{
    internal interface ITableLinkPointerRepository
    {
        Task<LinkPointer[]> GetEntityLinksAsync(string type, string sourceSystemName, string sourceSystemId,
            CancellationToken cancellationToken);

        Task<EntityLinkPointer[]> GetAllLinks(CancellationToken cancellationToken);

        Task StoreEntityLinkPointersAsync(Entity entity, CancellationToken cancellationToken);
    }

    internal class TableLinkPointerRepository : TableRepository<LinkPointerEntity, EntityLinkPointer>,
        ITableLinkPointerRepository
    {
        private readonly ILoggerWrapper _logger;

        internal TableLinkPointerRepository(EntitiesConfiguration configuration, ILoggerWrapper logger)
            : base(configuration.TableStorageConnectionString, configuration.TableStorageTableName)
        {
            _logger = logger;
        }

        public async Task<LinkPointer[]> GetEntityLinksAsync(string type, string sourceSystemName,
            string sourceSystemId, CancellationToken cancellationToken)
        {
            var keys = GetKeyPair(type, sourceSystemName, sourceSystemId,
                string.Empty, string.Empty);
            return await GetEntitiesInPartition(keys.PartitionKey, cancellationToken);
        }

        public async Task<EntityLinkPointer[]> GetAllLinks(CancellationToken cancellationToken)
        {
            var query = new TableQuery();
            var continuationToken = default(TableContinuationToken);
            var allEntities = new List<EntityLinkPointer>();
            var partitionKeyRegex = new Regex("^(.*)\\:(.*)\\:(.*)$", RegexOptions.IgnoreCase);
            
            _logger.Debug("Start reading all links...");
            
            do
            {
                var segment = await Table.ExecuteQuerySegmentedAsync(query, continuationToken, cancellationToken);

                foreach (var entity in segment.Results)
                {
                    var keyMatch = partitionKeyRegex.Match(entity.PartitionKey);
                    if (keyMatch.Success)
                    {
                        _logger.Debug($"Found matching link key {entity.PartitionKey}");
                        if (!entity.Properties.ContainsKey("Type"))
                        {
                            throw new Exception($"Link result {entity.PartitionKey} / {entity.RowKey} does not contain Type property");
                        }
                        
                        allEntities.Add(new EntityLinkPointer
                        {
                            EntityType = entity.Properties["Type"].StringValue,
                            SourceSystemName = entity.Properties["SourceSystemName"].StringValue,
                            SourceSystemId = entity.Properties["SourceSystemId"].StringValue,
                            LinkType = entity.Properties["LinkType"].StringValue,
                            LinkId = entity.Properties["LinkId"].StringValue,
                        });
                    }
                }
                

                continuationToken = segment.ContinuationToken;
            } while (continuationToken != null);

            return allEntities.ToArray();
        }

        public async Task StoreEntityLinkPointersAsync(Entity entity, CancellationToken cancellationToken)
        {
            var keys = GetKeyPair(entity.Type, entity.SourceSystemName, entity.SourceSystemName,
                string.Empty, string.Empty);
            var existingLinks = await GetEntitiesInPartition(keys.PartitionKey, cancellationToken);
            if (existingLinks.Length > 0)
            {
                await DeleteBatchAsync(existingLinks, cancellationToken);
            }

            if (entity.Links == null || entity.Links.Length == 0)
            {
                return;
            }

            var entityLinkPointers = entity.Links.Select(p =>
                new EntityLinkPointer
                {
                    EntityType = entity.Type,
                    SourceSystemName = entity.SourceSystemName,
                    SourceSystemId = entity.SourceSystemId,
                    LinkType = p.LinkType,
                    LinkId = p.LinkId,
                }).ToArray();
            await InsertOrReplaceBatchAsync(entityLinkPointers, cancellationToken);
        }

        protected override EntityLinkPointer ConvertEntityToModel(LinkPointerEntity entity)
        {
            return new EntityLinkPointer
            {
                EntityType = entity.Type,
                SourceSystemName = entity.SourceSystemName,
                SourceSystemId = entity.SourceSystemId,
                LinkType = entity.LinkType,
                LinkId = entity.LinkId,
            };
        }

        protected override LinkPointerEntity ConvertModelToEntity(EntityLinkPointer model)
        {
            var keys = GetKeyPair(model);
            return new LinkPointerEntity
            {
                PartitionKey = keys.PartitionKey,
                RowKey = keys.RowKey,
                Type = model.EntityType,
                SourceSystemName = model.SourceSystemName,
                SourceSystemId = model.SourceSystemId,
                LinkType = model.LinkType,
                LinkId = model.LinkId,
            };
        }

        private TableEntityKeyPair GetKeyPair(EntityLinkPointer model)
        {
            return GetKeyPair(
                model.EntityType,
                model.SourceSystemName,
                model.SourceSystemId,
                model.LinkType,
                model.LinkId);
        }

        private TableEntityKeyPair GetKeyPair(string entityType, string sourceSystemName, string sourceSystemId,
            string linkType, string linkId)
        {
            return new TableEntityKeyPair
            {
                PartitionKey = $"{entityType.ToLower()}:{sourceSystemName.ToUpper()}:{sourceSystemId.ToLower()}",
                RowKey = $"{linkType.ToLower()}:{linkId.ToLower()}",
            };
        }
    }
}