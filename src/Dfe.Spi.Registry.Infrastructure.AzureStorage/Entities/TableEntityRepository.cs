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
        
        public async Task<Entity> GetEntityAsync(string type, string sourceSystemName, string sourceSystemId, CancellationToken cancellationToken)
        {
            var keys = GetKeyPair(type, sourceSystemName, sourceSystemId);
            var entity = await GetSingleEntityAsync(keys.PartitionKey, keys.RowKey, cancellationToken);
            return entity;
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
                Data = !string.IsNullOrEmpty(entity.Data) ? JsonConvert.DeserializeObject<Dictionary<string,string>>(entity.Data) : null,
                Links = !string.IsNullOrEmpty(entity.Links) ? JsonConvert.DeserializeObject<LinkPointer[]>(entity.Links) : null,
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