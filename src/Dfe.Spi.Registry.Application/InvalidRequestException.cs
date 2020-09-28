using System;

namespace Dfe.Spi.Registry.Application
{
    public class InvalidRequestException : Exception
    {
        public InvalidRequestException(string message, string[] reasons)
            : base(message)
        {
            Reasons = reasons;
        }
        
        public string[] Reasons { get; }
    }
}