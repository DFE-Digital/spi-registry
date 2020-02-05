using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Links;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Links
{
    public class TableLinkRepository : TableRepository<EntityLinkEntity, Link>, ILinkRepository
    {
        public TableLinkRepository(LinksConfiguration configuration)
            : base(configuration.TableStorageConnectionString, configuration.TableStorageTableName)
        {
        }
        
        
        public async Task<Link> GetLinkAsync(string type, string id, CancellationToken cancellationToken)
        {
            var keys = GetKeyPair(type, id);
            var links = await GetEntitiesInPartition(keys.PartitionKey, cancellationToken);
            if (links == null || links.Length == 0)
            {
                return null;
            }

            var link = links.First();
            var linkedEntities = links.SelectMany(l => l.LinkedEntities).ToArray();
            return new Link
            {
                Type = link.Type,
                Id = link.Id,
                LinkedEntities = linkedEntities,
            };
        }

        public async Task StoreAsync(Link link, CancellationToken cancellationToken)
        {
            var toStore = link.LinkedEntities.Select(le =>
                new Link
                {
                    Type = link.Type,
                    Id = link.Id,
                    LinkedEntities = new[] {le},
                }).ToArray();

            await InsertOrReplaceBatchAsync(toStore, cancellationToken);
        }

        
        
        private TableEntityKeyPair GetKeyPair(Link model, int entityIndex = 0)
        {
            var entity = model.LinkedEntities[entityIndex];
            return new TableEntityKeyPair
            {
                PartitionKey = $"{model.Type.ToLower()}:{model.Id.ToLower()}",
                RowKey = $"{entity.EntityType.ToLower()}:{entity.EntitySourceSystemName.ToUpper()}:{entity.EntitySourceSystemId.ToLower()}",
            };
        }
        private TableEntityKeyPair GetKeyPair(string type, string id)
        {
            return new TableEntityKeyPair
            {
                PartitionKey = $"{type.ToLower()}:{id.ToLower()}",
            };
        }
        protected override Link ConvertEntityToModel(EntityLinkEntity entity)
        {
            return new Link
            {
                Type = entity.LinkType,
                Id = entity.LinkId,
                LinkedEntities = new[]
                {
                    new EntityLink
                    {
                        EntityType = entity.EntityType,
                        EntitySourceSystemName = entity.EntitySourceSystemName,
                        EntitySourceSystemId = entity.EntitySourceSystemId,
                        CreatedBy = entity.CreatedBy,
                        CreatedAt = entity.CreatedAt,
                        CreatedReason = entity.CreatedReason,
                    },
                }
            };
        }

        protected override EntityLinkEntity ConvertModelToEntity(Link model)
        {
            return ConvertModelToEntity(model, 0);
        }
        private EntityLinkEntity ConvertModelToEntity(Link model, int entityIndex)
        {
            var keys = GetKeyPair(model);
            var entity = model.LinkedEntities[entityIndex];

            return new EntityLinkEntity
            {
                PartitionKey = keys.PartitionKey,
                RowKey = keys.RowKey,
                LinkType = model.Type,
                LinkId = model.Id,
                EntityType = entity.EntityType,
                EntitySourceSystemName = entity.EntitySourceSystemName,
                EntitySourceSystemId = entity.EntitySourceSystemId,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                CreatedReason = entity.CreatedReason,
            };
        }
    }

    public class EntityLinkEntity : TableEntity
    {
        public string LinkType { get; set; }
        public string LinkId { get; set; }
        public string EntityType { get; set; }
        public string EntitySourceSystemName { get; set; }
        public string EntitySourceSystemId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedReason { get; set; }
    }
}