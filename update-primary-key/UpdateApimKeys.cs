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

namespace Company.Function
{
    public class Keys
    {
      public string OriginalKey { get; set; }
      public string NewKey { get; set; }
    }

    public class ApiManagementSubscriptionListSecrets {
      public string primaryKey { get; set; }
      public string secondaryKey { get; set; }
    }

    public class UpdateApimKeys
    {
       IConfidentialClientApplication confidentialClientApplication;
       HttpClient httpClient;
        public UpdateApimKeys() {
            confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(System.Environment.GetEnvironmentVariable("AzureAD__ClientID"))
              .WithClientSecret(System.Environment.GetEnvironmentVariable("AzureAD__ClientSecret"))
              .WithAuthority(new Uri($"{System.Environment.GetEnvironmentVariable("AzureAD__Instance")}/{System.Environment.GetEnvironmentVariable("AzureAD__Tenant")}"))
              .Build();
            httpClient = HttpClientFactory.Create();
        }

        [FunctionName("RegeneratePrimaryKey")]
        public async Task<IActionResult> RegeneratePrimaryKey(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var tokenResult = await confidentialClientApplication.AcquireTokenForClient(new List<string>{"https://management.azure.com/.default"}).ExecuteAsync();

            Keys apimKeys = new Keys();

            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://management.azure.com/subscriptions/dcf66641-6312-4ee1-b296-723bb0a999ba/resourceGroups/rg-apim-ussc-demo/providers/Microsoft.ApiManagement/service/apim-dev-ussc-demo/subscriptions/6169bbb8a952b1005f070001/listSecrets?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimListSecretsResult = await httpClient.SendAsync(requestMessage);

              var secrets = await apimListSecretsResult.Content.ReadFromJsonAsync<ApiManagementSubscriptionListSecrets>();
              apimKeys.OriginalKey = secrets.primaryKey;
            }

            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://management.azure.com/subscriptions/dcf66641-6312-4ee1-b296-723bb0a999ba/resourceGroups/rg-apim-ussc-demo/providers/Microsoft.ApiManagement/service/apim-dev-ussc-demo/subscriptions/6169bbb8a952b1005f070001/regeneratePrimaryKey?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimRegeneratePrimaryKeyResult = await httpClient.SendAsync(requestMessage);
            }

            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://management.azure.com/subscriptions/dcf66641-6312-4ee1-b296-723bb0a999ba/resourceGroups/rg-apim-ussc-demo/providers/Microsoft.ApiManagement/service/apim-dev-ussc-demo/subscriptions/6169bbb8a952b1005f070001/listSecrets?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimListSecretsResult = await httpClient.SendAsync(requestMessage);

              var secrets = await apimListSecretsResult.Content.ReadFromJsonAsync<ApiManagementSubscriptionListSecrets>();
              apimKeys.NewKey = secrets.primaryKey;
            }
           
            return new OkObjectResult(apimKeys);
        }

        [FunctionName("SetPrimaryKey")]
        public async Task<IActionResult> SetPrimaryKey(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }
}
