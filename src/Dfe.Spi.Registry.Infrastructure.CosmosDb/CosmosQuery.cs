using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Models;

namespace Dfe.Spi.Registry.Infrastructure.CosmosDb
{
    internal class CosmosQuery
    {
        private static readonly ReadOnlyDictionary<DataOperator, string> ComparisonOperatorMapping =
            new ReadOnlyDictionary<DataOperator, string>(new Dictionary<DataOperator, string>
            {
                {DataOperator.GreaterThan, ">"},
                {DataOperator.LessThan, "<"},
                {DataOperator.GreaterThanOrEqualTo, ">="},
                {DataOperator.LessThanOrEqualTo, "<="},
            });

        private static readonly DataOperator[] ValidOperatorsForNumericTypes = new[]
        {
            DataOperator.Between,
            DataOperator.Equals,
            DataOperator.GreaterThan,
            DataOperator.GreaterThanOrEqualTo,
            DataOperator.LessThan,
            DataOperator.LessThanOrEqualTo,
            DataOperator.In,
            DataOperator.IsNull,
            DataOperator.IsNotNull,
        };

        private static readonly DataOperator[] ValidOperatorsForDateTypes = new[]
        {
            DataOperator.Between,
            DataOperator.Equals,
            DataOperator.GreaterThan,
            DataOperator.GreaterThanOrEqualTo,
            DataOperator.LessThan,
            DataOperator.LessThanOrEqualTo,
            DataOperator.IsNull,
            DataOperator.IsNotNull,
        };

        private static readonly DataOperator[] ValidOperatorsForStringTypes = new[]
        {
            DataOperator.Equals,
            DataOperator.Contains,
            DataOperator.In,
            DataOperator.IsNull,
            DataOperator.IsNotNull,
        };

        private static readonly ReadOnlyDictionary<string, Type> SearchablePropertyTypes =
            new ReadOnlyDictionary<string, Type>(
                typeof(CosmosRegisteredEntity)
                    .GetProperties()
                    .Where(p => p.Name.StartsWith("Searchable"))
                    .ToDictionary(p => p.Name, p => p.PropertyType));

        private readonly CosmosCombinationOperator _combinationOperator;
        private readonly StringBuilder _whereClause;
        private int? _skip;
        private int? _take;

        internal CosmosQuery() : this(CosmosCombinationOperator.And)
        {
        }

        internal CosmosQuery(CosmosCombinationOperator combinationOperator)
        {
            _combinationOperator = combinationOperator;
            _whereClause = new StringBuilder();
        }

        internal virtual CosmosQuery AddCondition(string field, DataOperator @operator, string value)
        {
            var property = GetSearchableProperty(field);
            var propertyName = property.Key;
            var propertyType = property.Value;

            EnsureOperatorIsValidForProperty(@operator, propertyName, propertyType);

            if (_whereClause.Length > 0)
            {
                _whereClause.Append($" {_combinationOperator.ToString().ToUpper()} ");
            }

            switch (@operator)
            {
                case DataOperator.Equals:
                    _whereClause.Append($"ARRAY_CONTAINS(re.{propertyName}, {GetValueForQuery(value, propertyType)})");
                    break;
                case DataOperator.Contains:
                    _whereClause.Append($"EXISTS (SELECT VALUE v FROM v IN re.{propertyName} WHERE CONTAINS(v, {GetValueForQuery(value, propertyType)}))");
                    break;
                case DataOperator.GreaterThan:
                case DataOperator.GreaterThanOrEqualTo:
                case DataOperator.LessThan:
                case DataOperator.LessThanOrEqualTo:
                    _whereClause.Append(
                        $"EXISTS (SELECT VALUE v FROM v IN re.{propertyName} WHERE v {ComparisonOperatorMapping[@operator]} {GetValueForQuery(value, propertyType)})");
                    break;
                case DataOperator.IsNull:
                    _whereClause.Append($"ARRAY_LENGTH(re.{propertyName}) = 0");
                    break;
                case DataOperator.IsNotNull:
                    _whereClause.Append($"ARRAY_LENGTH(re.{propertyName}) > 0");
                    break;
                case DataOperator.Between:
                    var bounds = value.Split(new[] {" to "}, StringSplitOptions.RemoveEmptyEntries);
                    if (bounds.Length != 2)
                    {
                        throw new ArgumentException($"A between query must be in the format of '{{lower-bound}} to {{upper-bound}}', but received {value}");
                    }

                    _whereClause.Append($"EXISTS (SELECT VALUE v FROM v IN re.{propertyName} " +
                                        $"WHERE v >= {GetValueForQuery(bounds[0], propertyType)} AND v <= {GetValueForQuery(bounds[1], propertyType)})");
                    break;
                case DataOperator.In:
                    var group = value
                        .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => $"ARRAY_CONTAINS(re.{propertyName}, {GetValueForQuery(v.Trim(), propertyType)})")
                        .Aggregate((x, y) => $"{x} OR {y}");
                    _whereClause.Append($"({group})");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@operator));
            }

            return this;
        }

        internal virtual CosmosQuery AddTypeCondition(string type)
        {
            if (_whereClause.Length > 0)
            {
                _whereClause.Append($" {_combinationOperator.ToString().ToUpper()} ");
            }

            _whereClause.Append($"re.type = '{type.ToLower()}'");

            return this;
        }

        internal virtual CosmosQuery AddSourceSystemIdCondition(string sourceSystemName, string sourceSystemId)
        {
            if (_whereClause.Length > 0)
            {
                _whereClause.Append($" {_combinationOperator.ToString().ToUpper()} ");
            }

            _whereClause.Append($"ARRAY_CONTAINS(re.searchableSourceSystemIdentifiers, '{sourceSystemName.ToLower()}:{sourceSystemId.ToLower()}')");

            return this;
        }

        internal virtual CosmosQuery AddPointInTimeCondition(DateTime pointInTime)
        {
            if (_whereClause.Length > 0)
            {
                _whereClause.Append($" {_combinationOperator.ToString().ToUpper()} ");
            }

            _whereClause.Append($"(re.validFrom <= '{pointInTime.ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z' " +
                                $"AND (ISNULL(re.validTo) OR re.validTo >= '{pointInTime.ToUniversalTime():yyyy-MM-ddTHH:mm:ss}Z'))");

            return this;
        }

        internal virtual CosmosQuery AddGroup(CosmosQuery group)
        {
            if (_whereClause.Length > 0)
            {
                _whereClause.Append($" {_combinationOperator.ToString().ToUpper()} ");
            }
            
            _whereClause.Append($"({group._whereClause})");

            return this;
        }

        internal virtual CosmosQuery TakeResultsBetween(int skip, int take)
        {
            _skip = skip;
            _take = take;
            return this;
        }


        internal virtual string ToString(bool countQuery)
        {
            return ToString(countQuery, !countQuery);
        }

        public override string ToString()
        {
            return ToString(false);
        }

        private string ToString(bool countQuery, bool includeOrderAndSkipTake)
        {
            var select = countQuery ? $"SELECT COUNT(1)" : "SELECT *";
            var orderBy = includeOrderAndSkipTake ? " ORDER BY re.id" : String.Empty;
            var offsetLimit = includeOrderAndSkipTake && _skip.HasValue ? $" OFFSET {_skip} LIMIT {_take}" : String.Empty;
            return $"{select} FROM re WHERE {_whereClause}{orderBy}{offsetLimit}";
        }

        internal static IDictionary<string, Type> GetSearchablePropertyNames()
        {
            return SearchablePropertyTypes
                .Select(kvp => new
                {
                    Name = kvp.Key.StartsWith("Searchable") ? kvp.Key.Substring(10) : kvp.Key,
                    DataType = kvp.Value
                })
                .ToDictionary(
                    x => x.Name,
                    x => x.DataType);
        }


        private KeyValuePair<string, Type> GetSearchableProperty(string propertyName)
        {
            if (!propertyName.StartsWith("Searchable", StringComparison.InvariantCultureIgnoreCase))
            {
                propertyName = $"Searchable{propertyName}";
            }

            var kvp = SearchablePropertyTypes.SingleOrDefault(k => k.Key.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));
            if (string.IsNullOrEmpty(kvp.Key))
            {
                throw new ArgumentOutOfRangeException(nameof(propertyName),
                    $"Unrecognised property name {propertyName} on type {nameof(CosmosRegisteredEntity)}");
            }

            var camelCaseKey = kvp.Key.Substring(0, 1).ToLower() + kvp.Key.Substring(1);
            return new KeyValuePair<string, Type>(camelCaseKey, kvp.Value);
        }

        private void EnsureOperatorIsValidForProperty(DataOperator @operator, string propertyName, Type propertyType)
        {
            var validOperators = ValidOperatorsForStringTypes;
            if (propertyType == typeof(long[]))
            {
                validOperators = ValidOperatorsForNumericTypes;
            }

            if (propertyType == typeof(DateTime[]))
            {
                validOperators = ValidOperatorsForDateTypes;
            }

            var operatorIsValid = validOperators.Any(x => x == @operator);
            if (operatorIsValid)
            {
                return;
            }

            string validOperatorsString;
            if (validOperators.Length == 1)
            {
                validOperatorsString = validOperators[0].ToString();
            }
            else
            {
                var validOperatorsStart =
                    validOperators
                        .Select(x => x.ToString())
                        .Take(validOperators.Length - 1)
                        .Aggregate((x, y) => $"{x}, {y}");
                validOperatorsString = $"{validOperatorsStart} and {validOperators[validOperators.Length - 1].ToString()}";
            }

            throw new ArgumentException($"Operator {@operator.ToString()} is not valid for field {propertyName}. Valid operators are {validOperatorsString}");
        }

        private string GetValueForQuery(string value, Type dataType)
        {
            if (dataType == typeof(long[]))
            {
                return value;
            }

            if (dataType == typeof(DateTime[]))
            {
                var dateTime = value.ToDateTime();
                return $"'{dateTime:yyyy-MM-ddTHH:mm:ss}'";
            }

            // Treat everything else as a string
            return $"'{value?.ToLower().Replace("'", "\\'")}'";
        }
    }
}