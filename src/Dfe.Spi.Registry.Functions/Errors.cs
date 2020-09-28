namespace Dfe.Spi.Registry.Functions
{
    public static class Errors
    {
        public static readonly ErrorDetails SearchMalformedRequest = new ErrorDetails("SPI-REG-SEARCH01", "The supplied body was either empty, or not well-formed JSON.");
        public static readonly ErrorDetails SearchSchemaValidation = new ErrorDetails("SPI-REG-SEARCH02", null);
        public static readonly ErrorDetails SearchCodeValidation = new ErrorDetails("SPI-REG-SEARCH03", "The supplied body was well-formed JSON but it failed validation");
    }

    public class ErrorDetails
    {
        public ErrorDetails(string code, string message)
        {
            Code = code;
            Message = message;
        }
        public string Code { get; }
        public string Message { get; }
    }
}