using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Dfe.Spi.Registry.Functions.HealthCheck
{
    public static class HeartBeat
    {
        [FunctionName(nameof(HeartBeat))]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "GET", Route = "HeartBeat")]
            HttpRequest httpRequest)
        {
            OkResult toReturn = new OkResult();

            // Just needs to return 200/OK.
            return toReturn;
        }
    }
}
