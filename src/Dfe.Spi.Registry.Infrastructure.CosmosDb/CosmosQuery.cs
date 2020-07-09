using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    internal class CosmosQuery
    {

        private static readonly ReadOnlyDictionary<DataOperator, string> SimpleOperatorMapping =
            new ReadOnlyDictionary<DataOperator, string>(new Dictionary<DataOperator, string>
            {
                {DataOperator.Equals, "="},
                {DataOperator.GreaterThan, ">"},
                {DataOperator.LessThan, "<"},
                {DataOperator.GreaterThanOrEqualTo, ">="},
                {DataOperator.LessThanOrEqualTo, "<="},
            });

        private static readonly ReadOnlyDictionary<string, Type> EntityPropertyTypes =
            new ReadOnlyDictionary<string, Type>(
                typeof(Entity)
                    .GetProperties()
                    .ToDictionary(p => p.Name, p => p.PropertyType));

        private static readonly ReadOnlyDictionary<string, Type> RegisteredEntityPropertyTypes =
            new ReadOnlyDictionary<string, Type>(
                typeof(RegisteredEntity)
                    .GetProperties()
                    .Where(p => p.Name != "Entities" && p.Name != "Links")
                    .ToDictionary(p => p.Name, p => p.PropertyType));

        
        private StringBuilder _query = new StringBuilder();
        private int? _skip;
        private int? _take;

        public CosmosQuery(CosmosCombinationOperator combinationOperator)
        {
            CombinationOperator = combinationOperator;
        }

        public CosmosCombinationOperator CombinationOperator { get; }

        public CosmosQuery AddEntityCondition(string field, DataOperator @operator, string value)
        {
            var property = EntityPropertyTypes.SingleOrDefault(p => p.Key.Equals(field, StringComparison.InvariantCultureIgnoreCase));
            var fieldType = property.Value;
            field = property.Key;

            var condition = GetCondition("e", field, fieldType, @operator, value);

            AppendToQuery(condition);

            return this;
        }

        public CosmosQuery AddRegisteredEntityCondition(string field, DataOperator @operator, string value)
        {
            var property = RegisteredEntityPropertyTypes.SingleOrDefault(p => p.Key.Equals(field, StringComparison.InvariantCultureIgnoreCase));
            var fieldType = property.Value;
            field = property.Key;

            var condition = GetCondition("c", field, fieldType, @operator, value);

            AppendToQuery(condition);

            return this;
        }

        public CosmosQuery AddGroup(CosmosQuery group)
        {
            AppendToQuery($"({group._query})");
            
            return this;
        }

        public CosmosQuery AddPointInTimeConditions(DateTime pointInTime)
        {
            return this
                .AddRegisteredEntityCondition("validFrom", DataOperator.LessThanOrEqualTo, $"{pointInTime.ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z")
                .AddGroup(new CosmosQuery(CosmosCombinationOperator.Or)
                    .AddRegisteredEntityCondition("validTo", DataOperator.IsNull, null)
                    .AddRegisteredEntityCondition("validTo", DataOperator.GreaterThan, $"{pointInTime.ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z"));
        }

        public CosmosQuery TakeResultsBetween(int skip, int take)
        {
            _skip = skip;
            _take = take;
            return this;
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool forCount)
        {
            var querySelection = forCount ? "COUNT(c)" : "*";
            var fullQuery = $"SELECT {querySelection} FROM c WHERE EXISTS (SELECT VALUE e FROM e IN c.entities WHERE {_query}) ORDER BY c.id";
            
            if (_skip.HasValue && _take.HasValue)
            {
                return $"{fullQuery} OFFSET {_skip.Value} LIMIT {_take.Value}";
            }

            return fullQuery;
        }

        public CosmosQuery Clone()
        {
            var clone = new CosmosQuery(CombinationOperator);
            clone._query = _query;
            clone._skip = _skip;
            clone._take = _take;
            return clone;
        }


        private string GetCondition(string tableAlias, string field, Type fieldType, DataOperator @operator, string value)
        {
            var camelCaseField = field.Substring(0, 1).ToLower() + field.Substring(1);
            var aliasedField = $"{tableAlias}.{camelCaseField}";

            // Simple conditions (Equals, GreaterThan, etc)
            if (SimpleOperatorMapping.ContainsKey(@operator))
            {
                var cosmosOperator = SimpleOperatorMapping[@operator];
                return fieldType == typeof(string)
                    ? $"UPPER({aliasedField}) {cosmosOperator} {FormatValueForQuery(value, fieldType)}"
                    : $"{aliasedField} {cosmosOperator} {FormatValueForQuery(value, fieldType)}";
            }

            // Null check conditions
            if (@operator == DataOperator.IsNull)
            {
                return $"IS_NULL({aliasedField})";
            }

            if (@operator == DataOperator.IsNotNull)
            {
                return $"IS_NULL({aliasedField}) = false";
            }

            // Between
            if (@operator == DataOperator.Between)
            {
                var bounds = value.Split(new[] {" to "}, StringSplitOptions.RemoveEmptyEntries);

                if (bounds.Length != 2)
                {
                    throw new ArgumentException($"A between query must be in the format of '{{lower-bound}} to {{upper-bound}}', but received {value}");
                }

                return $"{aliasedField} BETWEEN {FormatValueForQuery(bounds[0], fieldType)} AND {FormatValueForQuery(bounds[1], fieldType)}";
            }

            // Contains
            if (@operator == DataOperator.Contains)
            {
                // Contains only applies to strings
                return $"CONTAINS(UPPER({aliasedField}), '{value.ToUpper()}')";
            }

            // In
            if (@operator == DataOperator.In)
            {
                var values = value
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => FormatValueForQuery(x.Trim(), fieldType))
                    .Aggregate((x, y) => $"{x}, {y}");
                return $"{aliasedField} IN ({values})";
            }

            throw new NotImplementedException($"Cannot support operator {@operator}");
        }

        private string FormatValueForQuery(string value, Type fieldType)
        {
            if (fieldType == typeof(short) || fieldType == typeof(int) || fieldType == typeof(long) ||
                fieldType == typeof(short?) || fieldType == typeof(int?) || fieldType == typeof(long?))
            {
                return value;
            }

            if (fieldType == typeof(DateTime) || fieldType == typeof(DateTime?))
            {
                return $"'{value.ToDateTime().ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z'";
            }

            // Treat everything else as a string
            return $"'{value.ToUpper()}'";
        }

        private void AppendToQuery(string condition)
        {
            if (_query.Length > 0)
            {
                _query.Append($" {CombinationOperator.ToString().ToUpper()} ");
            }

            _query.Append(condition);
        }
    }

    internal enum CosmosCombinationOperator
    {
        And,
        Or
    }
}