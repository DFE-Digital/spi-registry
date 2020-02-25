using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Entities;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities
{
    public class TableEntityRepository : TableRepository<EntityEntity, Entity>, IEntityRepository
    {
        public TableEntityRepository(EntitiesConfiguration configuration)
            : base(configuration.TableStorageConnectionString, configuration.TableStorageTableName)
        {
        }

        public async Task<Entity> GetEntityAsync(string type, string sourceSystemName, string sourceSystemId,
            CancellationToken cancellationToken)
        {
            var keys = GetKeyPair(type, sourceSystemName, sourceSystemId);
            var entity = await GetSingleEntityAsync(keys.PartitionKey, keys.RowKey, cancellationToken);
            return entity;
        }

        public async Task<Entity[]> GetEntitiesOfTypeAsync(string type, CancellationToken cancellationToken)
        {
            return await GetEntitiesInPartition(type.ToLower(), cancellationToken);
        }


        public async Task StoreAsync(Entity entity, CancellationToken cancellationToken)
        {
            await InsertOrReplaceAsync(entity, cancellationToken);
        }

        private TableEntityKeyPair GetKeyPair(Entity model)
        {
            return GetKeyPair(model.Type, model.SourceSystemName, model.SourceSystemId);
        }

        
        
        private TableEntityKeyPair GetKeyPair(string type, string sourceSystemName, string sourceSystemId)
        {
            return new TableEntityKeyPair
            {
                PartitionKey = type.ToLower(),
                RowKey = $"{sourceSystemName.ToUpper()}:{sourceSystemId.ToLower()}",
            };
        }

        protected override Entity ConvertEntityToModel(EntityEntity entity)
        {
            return new Entity
            {
                SourceSystemName = entity.SourceSystemName,
                SourceSystemId = entity.SourceSystemId,
                Type = entity.Type,
                Data = !string.IsNullOrEmpty(entity.Data)
                    ? JsonConvert.DeserializeObject<Dictionary<string, string>>(entity.Data)
                    : null,
                Links = !string.IsNullOrEmpty(entity.Links)
                    ? JsonConvert.DeserializeObject<LinkPointer[]>(entity.Links)
                    : null,
            };
        }

        protected override EntityEntity ConvertModelToEntity(Entity model)
        {
            var keys = GetKeyPair(model);
            return new EntityEntity
            {
                PartitionKey = keys.PartitionKey,
                RowKey = keys.RowKey,
                SourceSystemName = model.SourceSystemName,
                SourceSystemId = model.SourceSystemId,
                Type = model.Type,
                Data = model.Data != null ? JsonConvert.SerializeObject(model.Data) : null,
                Links = model.Links != null ? JsonConvert.SerializeObject(model.Links) : null,
            };
        }
    }

    public class EntityEntity : TableEntity
    {
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
        public string Type { get; set; }
        public string Data { get; set; }
        public string Links { get; set; }
    }
}