using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace func_backend_api_with_key
{
    public static class GetBackendData
    {
        [FunctionName("GetBackendData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var subscriptionKey = System.Environment.GetEnvironmentVariable("BackendAPI__SubscriptionKey");

            var result = "{ \"status\": \"Not authorized to query data\"}";

            if(req.Headers["SUBSCRIPTION-KEY"] == subscriptionKey) {
              result = "{ \"status\": \"Authorized to query data\", \"data\": \"Bears, beets, battlestar galactica!\"}";
            }

            return new OkObjectResult(result);
        }
    }
}
