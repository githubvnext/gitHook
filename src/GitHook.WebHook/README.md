## **AppSettings Set up in Azure Web App Service**

````javascript
[
  {
    "name": "AppSettings__GitHubAppName",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "AppSettings__GitHubSecret",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "AppSettings__GitHubTeamName",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "AppSettings__GitHubToken",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "AppSettings__QueueConnection",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "AppSettings__QueueName",
    "value": "",
    "slotSetting": false
  },
  {
    "name": "AppSettings__UseQueue",
    "value": "",
    "slotSetting": false
  }
]
````

The above settings are required to be set in appsettings.Development.json in case you are running the WebHook Project locally for Debugging.

Once the App is deployed to the Azure App Service, the Settings need to be set up in Application Configuration as per sample above. 

---
## **KeyVault Setup**

If you intend to setup the App Settings with Key Vault, then below actions need to be performed. 

1. Store Key Vault Secrets as below: - 
    - `GitHubAppName` - App Name which is used durin Octokit GitHubClient Initialization
    - `GitHubSecret` - GitHub WebHook Secret configured on GitHub side as well as on GitHook Configuration for HMAC Validation
    - `GitHubTeamName` - Team which is responsible for Code Reviews 
    - `GitHubToken` - Required Attribute for authenticating with GitHub REST APIs. This is only required when `UseQueue == false`
    - `queueConnection` - This is used in both queueConnection and AzureWebJobsStorage attribute setting in [`AppSettings_Configuration.json`](AppSettings_Configuration.json)
    - `QueueName` - Queue Name to which the PayLoad is queued and then processed by BranchProtect Azure Function
    - `UseQueue` - Set to `true` or `false` based on Asynchronous / Synchronous behavior of processing desired.

 

2. Enable System-Assigned Identity for the WebHook App Service.
3. Set the KeyVault Access Policy to allow System-Assigned Identity `(MSi)` for the WebHook App Service to have `Get` and `List` Access for secrets in the Key Vault.

4. Setup App Settings as per Sample provided in [`AppSettings_Configuration.json`](AppSettings_Configuration.json). You just need to update the **`<VaultName>`** 

