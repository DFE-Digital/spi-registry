using System;
using System.Collections.Generic;
using System.Linq;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Models;

namespace Dfe.Spi.Registry.Infrastructure.AzureCognitiveSearch
{
    public class AcsSearch
    {
        public AcsSearch(string combinationOperator)
        {
            CombinationOperator = combinationOperator;
        }

        public string CombinationOperator { get; }
        public string Query { get; set; } = "";
        public string Filter { get; set; }

        
        public void AppendQuery(SearchFieldDefinition field, string value)
        {
            if (IsNumericType(field.DataType))
            {
                AppendQuery($"{field.Name}: {value})");
            }
            else
            {
                AppendQuery($"{field.Name}: \"{value}\"");
            }
        }

        public void AppendQuery(string value)
        {
            if (Query?.Length > 0)
            {
                Query += $" {CombinationOperator} {value}";
            }
            else
            {
                Query = value;
            }
        }

        public void AppendFilter(SearchFieldDefinition field, DataOperator filterOperator, string value)
        {
            if (field.IsSearchable)
            {
                AppendFilter($"search.ismatch('\"{FormatStringValueForFilter(value)}\"', '{field.Name}')");
                return;
            }
            
            if (filterOperator == DataOperator.Between)
            {
                string[] dateParts = value.Split(
                    new string[] { " to " },
                    StringSplitOptions.RemoveEmptyEntries);

                if (dateParts.Length != 2)
                {
                    // Then get upset 💢
                    throw new FormatException(
                        $"Between values need to contain 2 valid " +
                        $"{nameof(DateTime)}s, seperated by the keyword " +
                        $"\"to\". For example, \"2018-06-29T00:00:00Z\" to " +
                        $"\"2018-07-01T00:00:00Z\".");
                }

                // Else...
                // Try and build up a group query of our own.
                AcsSearch between = new AcsSearch("and");
                between.AppendFilter(field, DataOperator.LessThan, dateParts.Last());
                between.AppendFilter(field, DataOperator.GreaterThan, dateParts.First());

                AddGroup(between);
            }
            else {

                if (filterOperator == DataOperator.In)
                {
                    var values = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                    var conditionValue = values.Aggregate((x, y) => $"{x},{y}");

                    if (field.IsArray)
                    {
                        AppendFilter($"{field.Name}/any(x: search.in(x, '{FormatStringValueForFilter(conditionValue)}', ','))");
                    }
                    else
                    {
                        AppendFilter($"search.in({field.Name}, '{FormatStringValueForFilter(conditionValue)}', ',')");
                    }
                }
                else
                {
                    string conditionValue;
                    if (filterOperator == DataOperator.IsNull || filterOperator == DataOperator.IsNotNull)
                    {
                        conditionValue = "null";
                    }
                    else
                    {
                        if (IsNumericType(field.DataType))
                        {
                            conditionValue = value;
                        }
                        else if (IsDateType(field.DataType))
                        {
                            var dtm = value.ToDateTime();

                            conditionValue = dtm.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }
                        else
                        {
                            conditionValue = $"'{FormatStringValueForFilter(value)}'";
                        }
                    }

                    var acsOperator = OperatorMappings[filterOperator];

                    if (field.IsArray)
                    {
                        AppendFilter($"{field.Name}/any(x: x {acsOperator} {conditionValue})");
                    }
                    else
                    {
                        AppendFilter($"{field.Name} {acsOperator} {conditionValue}");
                    }
                }
            }
        }

        public void AppendFilter(string value)
        {
            if (Filter?.Length > 0)
            {
                Filter += $" {CombinationOperator} {value}";
            }
            else
            {
                Filter = value;
            }
        }

        public void AddGroup(AcsSearch group)
        {
            if (!string.IsNullOrEmpty(group.Query) && group.Query != "*")
            {
                AppendQuery($"({group.Query})");
            }
            if (!string.IsNullOrEmpty(group.Filter))
            {
                AppendFilter($"({group.Filter})");
            }
        }



        private string FormatStringValueForFilter(string value)
        {
            return value.Replace("'", "''");
        }
        private bool IsNumericType(Type type)
        {
            return type == typeof(int) ||
                   type == typeof(int?) ||
                   type == typeof(long) ||
                   type == typeof(long?);
        }

        private bool IsDateType(Type type)
        {
            return type == typeof(DateTime) ||
                   type == typeof(DateTime?);
        }

        private static readonly Dictionary<DataOperator, string> OperatorMappings = new Dictionary<DataOperator, string>
        {
            {DataOperator.Equals, "eq"},
            {DataOperator.GreaterThan, "gt"},
            {DataOperator.GreaterThanOrEqualTo, "ge"},
            {DataOperator.LessThan, "lt"},
            {DataOperator.LessThanOrEqualTo, "le"},
            {DataOperator.IsNull, "eq"},
            {DataOperator.IsNotNull, "ne"}
        };
    }
}