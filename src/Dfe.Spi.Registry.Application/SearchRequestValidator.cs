using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dfe.Spi.Common.Models;
using Dfe.Spi.Registry.Domain.Search;

namespace Dfe.Spi.Registry.Application
{
    public static class SearchRequestValidator
    {
        private static readonly Dictionary<string, SearchFieldType> SearchableFieldTypes;
        private static readonly string SearchableFieldNamesForMessage;

        static SearchRequestValidator()
        {
            SearchableFieldTypes = typeof(SearchDocument)
                .GetProperties()
                .Select(propertyInfo =>
                    new
                    {
                        Property = propertyInfo,
                        SearchableFieldTypeAttr = propertyInfo.GetCustomAttribute(typeof(SearchFieldTypeAttribute))
                    })
                .Where(x => x.SearchableFieldTypeAttr != null)
                .ToDictionary(
                    x => x.Property.Name,
                    x => ((SearchFieldTypeAttribute) x.SearchableFieldTypeAttr).FieldType);

            SearchableFieldNamesForMessage = SearchableFieldTypes
                .Select(x => x.Key)
                .Aggregate((x, y) => $"{x}, {y}");
        }
        
        public static SearchRequestValidationResult Validate(this SearchRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Cannot validate null SearchRequest");
            }

            var errors = new List<string>();

            if (!IsValidCombinationOperator(request.CombinationOperator))
            {
                errors.Add($"request has invalid CombinationOperator ({request.CombinationOperator}). " +
                           $"Valid values are and, or");
            }

            if (request.Skip < 0)
            {
                errors.Add($"request has invalid Skip ({request.Skip}). Must be 0 or greater");
            }

            if (request.Take < 1 || request.Take > 100)
            {
                errors.Add($"request has invalid Take ({request.Take}). Must between 1 and 100 inclusive");
            }

            ValidateGroups(request.Groups, errors);

            return new SearchRequestValidationResult(errors.ToArray());
        }

        private static void ValidateGroups(SearchGroup[] groups, List<string> errors)
        {
            if (groups == null || groups.Length == 0)
            {
                errors.Add("request must have at least 1 group");
                return;
            }
            
            for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                var group = groups[groupIndex];

                ValidateGroupFilters(group.Filter, groupIndex, errors);

                if (!IsValidCombinationOperator(group.CombinationOperator))
                {
                    errors.Add($"group {groupIndex} has invalid CombinationOperator ({group.CombinationOperator}). " +
                               $"Valid values are and, or");
                }
            }
        }

        private static void ValidateGroupFilters(DataFilter[] filters, int groupIndex, List<string> errors)
        {
            if (filters == null || filters.Length == 0)
            {
                errors.Add($"group {groupIndex} must have at least 1 filter");
                return;
            }

            for (var filterIndex = 0; filterIndex < filters.Length; filterIndex++)
            {
                var filter = filters[filterIndex];

                if (string.IsNullOrEmpty(filter.Field))
                {
                    errors.Add($"group {groupIndex}, filter {filterIndex} must specify Field");
                    return;
                }
                
                var fieldPropertyName = SearchableFieldTypes.Keys.SingleOrDefault(k =>
                    k.Equals(filter.Field, StringComparison.InvariantCultureIgnoreCase));
                if (string.IsNullOrEmpty(fieldPropertyName))
                {
                    errors.Add($"group {groupIndex}, filter {filterIndex} has invalid Field ({filter.Field}). " +
                               $"Valid values are {SearchableFieldNamesForMessage}");
                    return;
                }

                var fieldType = SearchableFieldTypes[fieldPropertyName];
                if (!IsValidOperatorForFieldType(fieldType, filter.Operator))
                {
                    errors.Add($"group {groupIndex}, filter {filterIndex} has invalid Operator ({filter.Operator}) " +
                               $"for field {filter.Field}");
                }
            }
        }

        private static bool IsValidCombinationOperator(string combinationOperator)
        {
            return !string.IsNullOrEmpty(combinationOperator) &&
                   (combinationOperator.Equals("and", StringComparison.InvariantCultureIgnoreCase) ||
                    combinationOperator.Equals("or", StringComparison.InvariantCultureIgnoreCase));
        }

        private static bool IsValidOperatorForFieldType(SearchFieldType fieldType, DataOperator @operator)
        {
            DataOperator[] validOperators;
            switch (fieldType)
            {
                case SearchFieldType.Searchable:
                    validOperators = SearchableOperators;
                    break;
                case SearchFieldType.Enum:
                    validOperators = EnumOperators;
                    break;
                case SearchFieldType.Date:
                    validOperators = DateOperators;
                    break;
                case SearchFieldType.String:
                    validOperators = StringOperators;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(@operator));
            }

            return validOperators.Any(validOperator => @operator == validOperator);
        }
        
        private static readonly DataOperator[] SearchableOperators = new[]
        {
            DataOperator.Contains,
            DataOperator.Equals,
            DataOperator.IsNull,
            DataOperator.IsNotNull,
        };

        private static readonly DataOperator[] EnumOperators = new[]
        {
            DataOperator.Equals,
            DataOperator.In,
            DataOperator.IsNull,
            DataOperator.IsNotNull,
        };

        private static readonly DataOperator[] DateOperators = new[]
        {
            DataOperator.Equals,
            DataOperator.GreaterThan,
            DataOperator.GreaterThanOrEqualTo,
            DataOperator.LessThan,
            DataOperator.LessThanOrEqualTo,
            DataOperator.IsNull,
            DataOperator.IsNotNull,
            DataOperator.Between,
        };

        private static readonly DataOperator[] StringOperators = new[]
        {
            DataOperator.Equals,
            DataOperator.In,
            DataOperator.IsNull,
            DataOperator.IsNotNull,
        };
    }

    public class SearchRequestValidationResult
    {
        public SearchRequestValidationResult(params string[] errors)
        {
            Errors = errors ?? new string[0];
        }
        
        public string[] Errors { get; }
        public bool IsValid => Errors.Length == 0;
    }
}