using System;
using System.Collections.Generic;
using System.Linq;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Registry.Domain;
using Dfe.Spi.Registry.Domain.Data;

namespace Dfe.Spi.Registry.Application.SearchAndRetrieve
{
    internal interface ISearchRequestValidator
    {
        SearchRequestValidatorResult Validate(SearchRequest request);
    }

    internal class SearchRequestValidator : ISearchRequestValidator
    {
        private readonly IRepository _repository;
        private readonly ILoggerWrapper _logger;

        public SearchRequestValidator(
            IRepository repository,
            ILoggerWrapper logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public SearchRequestValidatorResult Validate(SearchRequest request)
        {
            var validationErrors = new List<string>();

            if (request.Take < 1 || request.Take > 100)
            {
                validationErrors.Add($"request has invalid Take ({request.Take}). Must between 1 and 100 inclusive");
            }
            
            if (request.Skip < 0)
            {
                validationErrors.Add($"request has invalid Skip ({request.Skip}). Must be 0 or greater");
            }
            
            if (!IsValidCombinationOperator(request.CombinationOperator))
            {
                validationErrors.Add($"request has invalid CombinationOperator ({request.CombinationOperator}). Valid values are and, or");
            }
            
            
            if (request.Groups?.Length < 1)
            {
                validationErrors.Add("request must have at least 1 group");
            }

            var searchableFields = _repository.GetSearchableFieldNames();
            var searchableFieldsForMessage = AggregateStringArrayForDisplay(searchableFields.Keys.ToArray());
            for (var groupIndex = 0; groupIndex < request.Groups?.Length; groupIndex++)
            {
                var group = request.Groups[groupIndex];
                
                if (!IsValidCombinationOperator(group.CombinationOperator))
                {
                    validationErrors.Add($"group at index {groupIndex} has invalid CombinationOperator ({group.CombinationOperator}). Valid values are and, or");
                }

                if (group.Filter?.Length < 1)
                {
                    validationErrors.Add($"group at index {groupIndex} must have at least 1 filter");
                }

                for (var filterIndex = 0; filterIndex < group.Filter?.Length; filterIndex++)
                {
                    var filter = group.Filter[filterIndex];
                    var fieldName = searchableFields.Keys.SingleOrDefault(x => x.ToLower() == filter.Field?.ToLower());

                    if (string.IsNullOrEmpty(fieldName))
                    {
                        validationErrors.Add($"filter at index {filterIndex} of group at index {groupIndex} has invalid field {filter.Field}. " +
                                             $"Valid values are {searchableFieldsForMessage}");
                    }
                    else
                    {
                        var dataType = searchableFields[fieldName];

                        if ((TypeIsLong(dataType) && !ValueIsLong(filter.Value)) ||
                            (TypeIsInt(dataType) && !ValueIsInt(filter.Value)))
                        {
                            validationErrors.Add($"filter at index {filterIndex} of group at index {groupIndex} has invalid field value. " +
                                                 $"Must be a number");
                        }
                    }
                }
            }
            
            return new SearchRequestValidatorResult(validationErrors);
        }

        private bool IsValidCombinationOperator(string combinationOperator)
        {
            return combinationOperator?.ToLower() == "and" ||
                   combinationOperator?.ToLower() == "or";
        }

        private string AggregateStringArrayForDisplay(string[] items)
        {
            var message = items.Aggregate((x, y) => $"{x}, {y}");

            var lastCommaIndex = message.LastIndexOf(',');
            if (lastCommaIndex > -1)
            {
                var prefix = message.Substring(0, lastCommaIndex).Trim();
                var suffix = message.Substring(lastCommaIndex + 1).Trim();
                message = $"{prefix} and {suffix}";
            }

            return message;
        }

        private bool TypeIsLong(Type dataType)
        {
            return dataType == typeof(long) ||
                   dataType == typeof(long[]);
        }
        private bool ValueIsLong(string value)
        {
            return long.TryParse(value, out var parsed);
        }

        private bool TypeIsInt(Type dataType)
        {
            return dataType == typeof(int) ||
                   dataType == typeof(int[]);
        }
        private bool ValueIsInt(string value)
        {
            return int.TryParse(value, out var parsed);
        }
    }

    internal class SearchRequestValidatorResult
    {
        public SearchRequestValidatorResult()
        {
            ValidationErrors = new string[0];
            IsValid = true;
        }

        public SearchRequestValidatorResult(IEnumerable<string> validationErrors)
        {
            ValidationErrors = validationErrors?.ToArray() ?? new string[0];
            IsValid = ValidationErrors.Length == 0;
        }

        public string[] ValidationErrors { get; private set; }

        public bool IsValid { get; private set; }
    }
}