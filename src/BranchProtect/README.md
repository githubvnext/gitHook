## **AppSettings which are set up in Azure Function Configuration**

````javascript
{
    "IsEncrypted": false,
    "Values": {
        "queueConnection": "<Full Storage Account Connection String>",
        "AzureWebJobsStorage": "<Full Storage Account Connection String>",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "GitHubToken": "<PAT Token for GitHub account>",
        "GitHubAppName": "<App Name which is used durin Octokit GitHubClient Initialization>",
        "GitHubTeamName": "<Team which is responsible for Code Reviews>"
     }
}
````

The above settings are required to be set in local.settings.json in case you are running the BranchProtect Azure Function locally
