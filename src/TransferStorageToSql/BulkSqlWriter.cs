using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;

namespace TransferStorageToSql
{
    internal class BulkSqlWriter : IDisposable
    {
        private readonly ILoggerWrapper _logger;
        private SqlConnection _sqlConnection;

        public BulkSqlWriter(string connectionString, ILoggerWrapper logger)
        {
            _logger = logger;
            _sqlConnection = new SqlConnection(connectionString);
        }

        public async Task StoreAsync(Entity[] entities, Link[] links, CancellationToken cancellationToken)
        {
            await _sqlConnection.OpenAsync(cancellationToken);
            
            using var transaction = _sqlConnection.BeginTransaction();
            
            var entityIdLookup = new Dictionary<string, Guid>();
            var entityEntities = new List<EntityEntity>();
            var linkEntities = new List<LinkEntity>();
            var linkedEntityEntities = new List<LinkedEntityEntity>();
            var entityAttributeEntities = new List<EntityAttributeEntity>();

            // Transpose entities
            _logger.Debug("Transposing entities for bulk copy");
            foreach (var entity in entities)
            {
                var id = Guid.NewGuid();
                entityIdLookup.Add($"{entity.SourceSystemName}:{entity.SourceSystemId}".ToLower(), id);
                entityEntities.Add(new EntityEntity
                {
                    Id = id,
                    EntityType = entity.Type,
                    SourceSystemName = entity.SourceSystemName,
                    SourceSystemId = entity.SourceSystemId,
                });
                entityAttributeEntities.AddRange(entity.Data
                    .Select(kvp=>
                        new EntityAttributeEntity
                        {
                            EntityId = id,
                            AttributeName = kvp.Key,
                            AttributeValue = kvp.Value,
                        }));
            }
            
            // Transpose links
            _logger.Debug("Transposing links for bulk copy");
            foreach (var link in links)
            {
                var id = Guid.NewGuid();
                linkEntities.Add(new LinkEntity
                {
                    Id = id,
                    LinkType = link.LinkType,
                });
                linkedEntityEntities.AddRange(link.LinkedEntities
                    .Select(linkedEntity => 
                        new LinkedEntityEntity
                        {
                            LinkId = id,
                            EntityId = entityIdLookup[$"{linkedEntity.SourceSystemName}:{linkedEntity.SourceSystemId}".ToLower()],
                            CreatedAt = linkedEntity.CreatedAt,
                            CreatedBy = linkedEntity.CreatedBy,
                            CreatedReason = linkedEntity.CreatedReason,
                        }));
            }
            
            // Bulk copy entities
            _logger.Debug("Copying entities");
            using (var bcp = MakeSqlBulkCopy("Registry.Entity", transaction))
            using (var reader = new ObjectDataReader<EntityEntity>(entityEntities))
            {
                await bcp.WriteToServerAsync(reader, cancellationToken);
            }
            
            // Bulk copy attributes
            _logger.Debug("Copying entity attributes");
            using (var bcp = MakeSqlBulkCopy("Registry.EntityAttribute", transaction))
            using (var reader = new ObjectDataReader<EntityAttributeEntity>(entityAttributeEntities))
            {
                await bcp.WriteToServerAsync(reader, cancellationToken);
            }
            
            // Bulk copy links
            _logger.Debug("Copying links");
            using (var bcp = MakeSqlBulkCopy("Registry.Link", transaction))
            using (var reader = new ObjectDataReader<LinkEntity>(linkEntities))
            {
                await bcp.WriteToServerAsync(reader, cancellationToken);
            }
            
            // Bulk copy linked entities
            _logger.Debug("Copying linked entities");
            using (var bcp = MakeSqlBulkCopy("Registry.LinkedEntity", transaction))
            using (var reader = new ObjectDataReader<LinkedEntityEntity>(linkedEntityEntities))
            {
                await bcp.WriteToServerAsync(reader, cancellationToken);
            }
            
            // commit
            transaction.Commit();
        }

        private SqlBulkCopy MakeSqlBulkCopy(string tableName, SqlTransaction transaction)
        {
            var bcp = new SqlBulkCopy(_sqlConnection, SqlBulkCopyOptions.Default, transaction);

            bcp.BatchSize = 100;
            bcp.DestinationTableName = tableName;

            return bcp;
        }

        public void Dispose()
        {
            _sqlConnection?.Dispose();
        }
    }

    internal class EntityEntity
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; }
        public string SourceSystemName { get; set; }
        public string SourceSystemId { get; set; }
    }

    internal class EntityAttributeEntity
    {
        public Guid EntityId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeValue { get; set; }
    }

    internal class LinkEntity
    {
        public Guid Id { get; set; }
        public string LinkType { get; set; }
    }

    internal class LinkedEntityEntity
    {
        public Guid LinkId { get; set; }
        public Guid EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedReason { get; set; }
    }
    
    internal class ObjectDataReader<T> : DbDataReader
    {
        private readonly T[] _items;
        private PropertyInfo[] _fields;
        private IEnumerator _enumerator;
        private T _current;

        public ObjectDataReader(IEnumerable<T> items)
        {
            _items = items.ToArray();

            _fields = typeof(T).GetRuntimeProperties().ToArray();
            _enumerator = _items.GetEnumerator();
        }



        public override int FieldCount => _fields.Length;

        public override object this[int ordinal] => GetValue(ordinal);

        public override object this[string name] => GetValue(GetOrdinal(name));

        public override int RecordsAffected => -1;
        public override bool HasRows => _items.Length > 0;
        public override bool IsClosed => false;
        public override int Depth => throw new NotImplementedException();



        public override bool GetBoolean(int ordinal)
        {
            return (bool) GetValue(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            return (byte)GetValue(ordinal);
        }

        public override char GetChar(int ordinal)
        {
            return (char)GetValue(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return (DateTime)GetValue(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return (decimal)GetValue(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return (double)GetValue(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return (float)GetValue(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return (Guid)GetValue(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return (short)GetValue(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return (int)GetValue(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return (long)GetValue(ordinal);
        }

        public override string GetString(int ordinal)
        {
            return (string)GetValue(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _fields[ordinal].Name;
        }

        public override int GetOrdinal(string name)
        {
            for (var i = 0; i < _fields.Length; i++)
            {
                if (_fields[i].Name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public override Type GetFieldType(int ordinal)
        {
            return _fields[ordinal].PropertyType;
        }

        public override string GetDataTypeName(int ordinal)
        {
            return GetFieldType(ordinal).Name;
        }

        public override object GetValue(int ordinal)
        {
            return _fields[ordinal].GetValue(_current);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }



        public override bool IsDBNull(int ordinal)
        {
            var value = GetValue(ordinal);
            return value == null || value == DBNull.Value;
        }

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            var result = _enumerator.MoveNext();
            _current = result ? (T)_enumerator.Current : default(T);
            return result;
        }


        public override IEnumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

    }
}