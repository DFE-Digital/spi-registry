using System;

namespace Dfe.Spi.Registry.Application
{
    public class InvalidRequestException : Exception
    {
        public InvalidRequestException(SearchRequestValidationResult searchRequestValidationResult)
            : base("Invalid search request")
        {
            Reasons = searchRequestValidationResult.Errors;
        }
        
        public string[] Reasons { get; }
    }
}