using GitHook.BusinessLayer;
using GitHook.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;

namespace BranchProtect
{
    public static class BranchProtectionFunc
    {


        [FunctionName("BranchProtectionFunc")]
        public static void Run([QueueTrigger("branchqueue", Connection = "queueConnection")] string myQueueItem, ILogger log)
        {
            //Get the Payload in Base64 format
            log.LogInformation("C# Queue trigger function processed: " + myQueueItem);

            //Convert the Payload to JSON Format
            PayloadInfo payloadInfo;
            try
            {
                var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(myQueueItem));
                payloadInfo = JsonConvert.DeserializeObject<PayloadInfo>(payloadJson);

            }
            catch (Exception ex)
            {
                log.LogError($"The Payload was either not base64 encoded or not valid Payload Json ! The Payload is being discarded {ex}");
                return;
            }

            //Get all the Configurations & Print in Logs.
            string GitHubToken = GetEnvironmentVariable("GitHubToken");
            string GitHubAppName = GetEnvironmentVariable("GitHubAppName");
            string GitHubTeamName = GetEnvironmentVariable("GitHubTeamName");

            log.LogInformation($"GitHubAppName is {GitHubAppName}");
            log.LogInformation($"GitHubTeamName is {GitHubTeamName}");

            log.LogInformation(payloadInfo.branchName);
            log.LogInformation(payloadInfo.repoName);
            log.LogInformation(payloadInfo.orgName);
            Console.WriteLine(payloadInfo.branchName);
            Console.WriteLine(payloadInfo.repoName);
            Console.WriteLine(payloadInfo.orgName);
            

            BranchProtection branchProtection = new BranchProtection(log);

            branchProtection.ProtectRepo(payloadInfo, GitHubAppName, GitHubToken, GitHubTeamName).GetAwaiter().GetResult();
            log.LogInformation("Queue trigger for Payload completed Successfully " + myQueueItem);
        }

        private static string GetEnvironmentVariable(string name) =>  Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

    }
}
