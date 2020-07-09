using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dfe.Spi.Registry.Functions
{
    // Created this as having issues with .NET 3.1 and customising serialization settings:
    //    - OTB you cannot provide JsonSerializerSettings as the default serialization is no longer Newtonsoft
    //    - Adding the Newtonsoft path package fixed when running but caused issues with the use of DefaultHttpRequest in tests
    //    - Adding json formatters in startup
    // If a solution to one of the above can be found then the use of the OTB JsonResult might be more appropriate
    public class FormattedJsonResult : ActionResult
    {
        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };
        
        public FormattedJsonResult(object value, HttpStatusCode statusCode, JsonSerializerSettings serializerSettings)
        {
            Value = value;
            StatusCode = statusCode;
            SerializerSettings = serializerSettings;
        }
        public FormattedJsonResult(object value)
            : this(value, HttpStatusCode.OK, DefaultSerializerSettings)
        {
        }

        public object Value { get; }
        public HttpStatusCode StatusCode { get; }
        public JsonSerializerSettings SerializerSettings { get; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var json = JsonConvert.SerializeObject(Value, SerializerSettings);
            var bodyBuffer = Encoding.UTF8.GetBytes(json);
            
            context.HttpContext.Response.StatusCode = (int)StatusCode;
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.Body.WriteAsync(bodyBuffer, 0, bodyBuffer.Length);
        }
    }
}