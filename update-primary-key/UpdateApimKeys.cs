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
    public class Keys
    {
      public string OriginalKey { get; set; }
      public string NewKey { get; set; }
    }

    public class ApiManagementSubscriptionSecrets {
      public string primaryKey { get; set; }
      public string secondaryKey { get; set; }
    }

    public class ApiManagementSubscriptionProperties {
      public ApiManagementSubscriptionSecrets properties { get; set; }
    }

    public class UpdateApimKeys
    {
       IConfidentialClientApplication confidentialClientApplication;
       HttpClient httpClient;

       static string APIM_MANAGEMENT_ENDPOINT_FORMAT = "https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.ApiManagement/service/{2}/subscriptions/{3}";
       readonly string APIM_MANAGEMENT_ENDPOINT;
        public UpdateApimKeys() {
            confidentialClientApplication = ConfidentialClientApplicationBuilder.Create(System.Environment.GetEnvironmentVariable("AzureAD__ClientID"))
              .WithClientSecret(System.Environment.GetEnvironmentVariable("AzureAD__ClientSecret"))
              .WithAuthority(new Uri($"{System.Environment.GetEnvironmentVariable("AzureAD__Instance")}/{System.Environment.GetEnvironmentVariable("AzureAD__Tenant")}"))
              .Build();
            httpClient = HttpClientFactory.Create();
            APIM_MANAGEMENT_ENDPOINT = string.Format(APIM_MANAGEMENT_ENDPOINT_FORMAT, System.Environment.GetEnvironmentVariable("APIM__AzureSubscriptionID"), 
                                                                                      System.Environment.GetEnvironmentVariable("APIM__ResourceGroupName"),
                                                                                      System.Environment.GetEnvironmentVariable("APIM__ServiceName"),
                                                                                      System.Environment.GetEnvironmentVariable("APIM__ApimSubscriptionID"));
        }

        [FunctionName("RegeneratePrimaryKey")]
        public async Task<IActionResult> RegeneratePrimaryKey(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var tokenResult = await confidentialClientApplication.AcquireTokenForClient(new List<string>{"https://management.azure.com/.default"}).ExecuteAsync();

            Keys apimKeys = new Keys();

            //get original APIM primary subscription key
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{APIM_MANAGEMENT_ENDPOINT}/listSecrets?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimListSecretsResult = await httpClient.SendAsync(requestMessage);

              var secrets = await apimListSecretsResult.Content.ReadFromJsonAsync<ApiManagementSubscriptionSecrets>();
              apimKeys.OriginalKey = secrets.primaryKey;
            }

            //regenerate primary key
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{APIM_MANAGEMENT_ENDPOINT}/regeneratePrimaryKey?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimRegeneratePrimaryKeyResult = await httpClient.SendAsync(requestMessage);
            }

            //get new APIM primary subscription key
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{APIM_MANAGEMENT_ENDPOINT}/listSecrets?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimListSecretsResult = await httpClient.SendAsync(requestMessage);

              var secrets = await apimListSecretsResult.Content.ReadFromJsonAsync<ApiManagementSubscriptionSecrets>();
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

            var tokenResult = await confidentialClientApplication.AcquireTokenForClient(new List<string>{"https://management.azure.com/.default"}).ExecuteAsync();

            Keys apimKeys = new Keys();

            //get original APIM primary subscription key
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{APIM_MANAGEMENT_ENDPOINT}/listSecrets?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimListSecretsResult = await httpClient.SendAsync(requestMessage);

              var secrets = await apimListSecretsResult.Content.ReadFromJsonAsync<ApiManagementSubscriptionSecrets>();
              apimKeys.OriginalKey = secrets.primaryKey;
            }

            //set keys
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Patch, $"{APIM_MANAGEMENT_ENDPOINT}?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              string requestBody = String.Empty;
              using(StreamReader streamReader = new StreamReader(req.Body)) {
                requestBody = await streamReader.ReadToEndAsync();
              }
              ApiManagementSubscriptionSecrets newKeys = JsonConvert.DeserializeObject<ApiManagementSubscriptionSecrets>(requestBody);

              if(String.IsNullOrEmpty(newKeys.primaryKey) || String.IsNullOrEmpty(newKeys.secondaryKey)) {
                throw new ArgumentNullException("PrimaryKey and/or SecondaryKey must not be NULL");
              }
              
              ApiManagementSubscriptionProperties apiManagementSubscriptionProperties = new ApiManagementSubscriptionProperties();
              apiManagementSubscriptionProperties.properties = newKeys;
              requestMessage.Content = new StringContent(JsonConvert.SerializeObject(apiManagementSubscriptionProperties), Encoding.UTF8, "application/json");

              var apimRegeneratePrimaryKeyResult = await httpClient.SendAsync(requestMessage);
            }

            //get new APIM primary subscription key
            using(var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{APIM_MANAGEMENT_ENDPOINT}/listSecrets?api-version=2020-12-01"))
            {
              requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

              var apimListSecretsResult = await httpClient.SendAsync(requestMessage);

              var secrets = await apimListSecretsResult.Content.ReadFromJsonAsync<ApiManagementSubscriptionSecrets>();
              apimKeys.NewKey = secrets.primaryKey;
            }
           
            return new OkObjectResult(apimKeys);
        }
    }
}
