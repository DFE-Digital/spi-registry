using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Newtonsoft.Json;

namespace Dfe.Spi.Registry.Functions.UnitTests
{
    public class HttpRequestBuilder
    {
        public static HttpRequestBuilder CreateHttpRequest()
        {
            return new HttpRequestBuilder();
        }

        public HttpRequestBuilder WithJsonBody(object content)
        {
            _content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content));
            return this;
        }

        public static implicit operator HttpRequest(HttpRequestBuilder builder)
        {
            var request = new DefaultHttpRequest(new DefaultHttpContext());

            if (builder._content != null)
            {
                request.Body = new MemoryStream(builder._content);
            }

            return request;
        }


        private byte[] _content;
    }
}