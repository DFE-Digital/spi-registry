using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;

namespace Dfe.Spi.Registry.Functions.HealthCheck
{
    public class Heartbeat
    {
        [FunctionName("Heartbeat")]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req)
        {
            return new OkResult();
        }
    }
}