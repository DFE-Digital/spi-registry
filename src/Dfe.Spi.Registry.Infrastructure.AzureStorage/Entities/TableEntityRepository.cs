using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Registry.Domain.Configuration;
using Dfe.Spi.Registry.Domain.Entities;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Infrastructure.AzureStorage.Entities
{
    internal interface ITableEntityRepository
    {
        Task<Entity> GetEntityAsync(string type, string sourceSystemName, string sourceSystemId,
            CancellationToken cancellationToken);

        Task<Entity[]> GetEntitiesOfTypeAsync(string type, CancellationToken cancellationToken);
        Task StoreAsync(Entity entity, CancellationToken cancellationToken);
    }

    internal class TableEntityRepository : TableRepository<EntityEntity, Entity>, ITableEntityRepository
    {
        internal TableEntityRepository(EntitiesConfiguration configuration)
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
            try
            {
                await InsertOrReplaceAsync(entity, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to store entity {entity}: {ex.Message}", ex);
            }
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
            };
        }
    }
}