using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace Company.Function
{
    public class CallBackendApiWithSubscriptionKey
    {
       HttpClient httpClient;

        public CallBackendApiWithSubscriptionKey() {
            httpClient = HttpClientFactory.Create();
        }

        [FunctionName("CallBackendApiWithSubscriptionKey")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var result = "";

            //regenerate primary key
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Get, System.Environment.GetEnvironmentVariable("BackendAPI__Uri")))
            {
              requestMessage.Headers.Add("SUBSCRIPTION-KEY", System.Environment.GetEnvironmentVariable("BackendAPI__SubscriptionKey"));

              var requestResult = await httpClient.SendAsync(requestMessage);

              result = await requestResult.Content.ReadAsStringAsync();
            }
           
            return new OkObjectResult(result);
        }
    }
}
