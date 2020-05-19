using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;

namespace TransferStorageToSql
{
    class SqlWriter : IDisposable
    {
        private SqlConnection _sqlConnection;

        public SqlWriter(string connectionString, ILoggerWrapper logger)
        {
            _sqlConnection = new SqlConnection(connectionString);
        }

        public async Task StoreAsync(Entity entity, CancellationToken cancellationToken)
        {
            if (_sqlConnection.State != ConnectionState.Open)
            {
                await _sqlConnection.OpenAsync(cancellationToken);
            }

            using var transaction = _sqlConnection.BeginTransaction();
            using var command = _sqlConnection.CreateCommand();
            command.Transaction = transaction;

            var entityId = Guid.NewGuid();

            // Add entity
            command.CommandText = "INSERT INTO Registry.Entity (Id, EntityType, SourceSystemName, SourceSystemId) "
                                  + "VALUES (@Id, @EntityType, @SourceSystemName, @SourceSystemId)";
            command.Parameters.Set(
                new KeyValuePair<string, object>("Id", entityId),
                new KeyValuePair<string, object>("EntityType", entity.Type),
                new KeyValuePair<string, object>("SourceSystemName", entity.SourceSystemName),
                new KeyValuePair<string, object>("SourceSystemId", entity.SourceSystemId));
            await command.ExecuteNonQueryAsync(cancellationToken);

            // Add attributes
            foreach (var name in entity.Data.Keys)
            {
                var value = entity.Data[name];

                command.Parameters.Clear();
                command.CommandText = "INSERT INTO Registry.EntityAttribute (EntityId, AttributeName, AttributeValue) "
                                      + "VALUES (@EntityId, @AttributeName, @AttributeValue)";
                command.Parameters.Set(
                    new KeyValuePair<string, object>("EntityId", entityId),
                    new KeyValuePair<string, object>("AttributeName", name),
                    new KeyValuePair<string, object>("AttributeValue", value));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            transaction.Commit();
        }

        public async Task StoreAsync(Link link, CancellationToken cancellationToken)
        {
            if (_sqlConnection.State != ConnectionState.Open)
            {
                await _sqlConnection.OpenAsync(cancellationToken);
            }

            using var transaction = _sqlConnection.BeginTransaction();
            using var command = _sqlConnection.CreateCommand();
            command.Transaction = transaction;

            // Store link
            command.CommandText = "INSERT INTO Registry.Link (Id, LinkType) "
                                  + "VALUES (@Id, @LinkType)";
            command.Parameters.Set(
                new KeyValuePair<string, object>("Id", link.Id),
                new KeyValuePair<string, object>("LinkType", link.LinkType));
            await command.ExecuteNonQueryAsync(cancellationToken);
            
            // Store linked entities
            foreach (var linkedEntity in link.LinkedEntities)
            {
                command.CommandText = "INSERT INTO Registry.LinkedEntity (LinkId, EntityId, CreatedAt, CreatedBy, CreatedReason) "
                                      + "SELECT @LinkId, Id, @CreatedAt, @CreatedBy, @CreatedReason "
                                      + "FROM Registry.Entity "
                                      + "WHERE EntityType = @EntityType "
                                      + "AND SourceSystemName = @SourceSystemName "
                                      + "AND SourceSystemId = @SourceSystemId ";
                command.Parameters.Set(
                    new KeyValuePair<string, object>("LinkId", link.Id),
                    new KeyValuePair<string, object>("CreatedAt", linkedEntity.CreatedAt),
                    new KeyValuePair<string, object>("CreatedBy", linkedEntity.CreatedBy),
                    new KeyValuePair<string, object>("CreatedReason", linkedEntity.CreatedReason),
                    new KeyValuePair<string, object>("EntityType", linkedEntity.EntityType),
                    new KeyValuePair<string, object>("SourceSystemName", linkedEntity.SourceSystemName),
                    new KeyValuePair<string, object>("SourceSystemId", linkedEntity.SourceSystemId));
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            
            transaction.Commit();
        }

        public void Dispose()
        {
            _sqlConnection?.Dispose();
        }
    }


    public static class SqlExtensions
    {
        public static void Set(this SqlParameterCollection parameters, params KeyValuePair<string, object>[] parameterSet)
        {
            parameters.Clear();
            foreach (var nameValuePair in parameterSet)
            {
                parameters.AddWithValue(nameValuePair.Key, nameValuePair.Value);
            }
        }
    }
}