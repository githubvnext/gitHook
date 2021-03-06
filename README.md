## **GitHub WebHook**
GitHook WebHook based on Asynchronous WebHook processing


---

GitHub Actions Status 

Web Hook CI | BranchProtect Function
------------ | -------------
[![WebHookDeploy](https://github.com/githubvnext/gitHook/actions/workflows/webhook-ci.yml/badge.svg)](https://github.com/githubvnext/gitHook/actions/workflows/webhook-ci.yml) | [![FunctionDeploy](https://github.com/githubvnext/gitHook/actions/workflows/function-ci.yml/badge.svg)](https://github.com/githubvnext/gitHook/actions/workflows/function-ci.yml) 

---

The Components are described as in below diagram

![Process Flow](docs/GitHubWebhookProcessFlow.png)

The above Diagram is just indicative of the major components.

---

### **Components Definition**

This Repository contains an ASP.NET Core Web Api based WebHook with an Azure Function to process payload Asynchronously. The asynchronous processing is based on **[GitHub WebHook Integrations Best Practices recommendation](https://docs.github.com/en/rest/guides/best-practices-for-integrators#favor-asynchronous-work-over-synchronous)**

There are 4 Visual Studio Projects through which the objectives below are achieved. 

- Use Web API to get webhook Calls from GitHub installation
- Web API queues the payload in Azure Storage Queues
- Azure Functions (Queue Trigger) processes the GitHub WebHook payloads
- Use GitHub REST API to Protect Master / main branches

---

### **Projects in the repository**

1.  **[GitHook.Models](src/GitHook.Models)** -  Contains common model definitions that are used throughout in other Projects.
  - _[GitHubPayload](src/GitHook.Models/GitHubPayload.cs)_ typed format of GitHub Webhook Payload JSON.
  - _[PayloadInfo](src/GitHook.Models/PayloadInfo.cs)_ which is used for Seriliazing and Deserializing the payload between different components.

2. **[GitHook.BusinessLayer](src/GitHook.BusinessLayer)** - A C# Class Library that contains 2 Classes which help in Parsing the payload and running Branch Protection Logics. 

    a. **[BranchProtection](src/GitHook.BusinessLayer/BranchProtection.cs)** - achieves 2 below objectives. The Component Calls GitHub REST APIs below through _[Octokit.Net v 0.5.0](https://www.nuget.org/packages/Octokit/0.50.0)_
      - Protect the Branch through GitHub REST API: -
        - [Get Repository](https://docs.github.com/en/rest/reference/repos#get-a-repository)
        - [Get Branch Protection](https://docs.github.com/en/rest/reference/repos#get-branch-protection)
        - [Retrieve Teams](https://docs.github.com/en/rest/reference/teams#list-teams)
        - [Update Branch Protection](https://docs.github.com/en/rest/reference/repos#update-branch-protection)
      - [Create an Issue describing Branch Protection applied using @mention tag](https://docs.github.com/en/rest/reference/issues#create-an-issue)

    b. **[PayloadParser](src/GitHook.BusinessLayer/PayloadParser.cs)** - This is used in WebHook Project for achieving below objectives 
      - Parse the incoming GitHub WebHook Payload JSON into Typed Payload [GitHubPayload](src/GitHook.Models/GitHubPayload.cs)
      - Cherry Picks the properties from GitHubPayload to fetch [PayloadInfo](src/GitHook.Models/PayloadInfo.cs)

3. **[GitHook.WebHook](src/GitHook.WebHook)** - A ASP.NET Core Web API (C#) Class Library that contains _[GitHookController](src/GitHook.WebHook/Controllers/GitHookController.cs)_ REST API Controller to Receive the Web Hook Calls from GitHub. The WebAPI project has capabilities to do Asynchronous and Synchronous Branch Protection function. 
  The GitHook.WebHook project contains Dependency Injection based _[IPayloadProcessor](src/GitHook.WebHook/Processors/IPayloadProcessor.cs)_ which is set during Application _[Startup.cs](GitHook.WebHook/Startup.cs)_. If UseQueue is set, then _[QueueProcessor](src/GitHook.WebHook/Processors/QueueProcessor.cs)_ is used, else _[DirectProcessor](src/GitHook.WebHook/Processors/DirectProcessor.cs)_ is used. It is recommended to set "UseQueue" to true to achieve below objectives as per **[GitHub WebHook Integrations Best Practices recommendation](https://docs.github.com/en/rest/guides/best-practices-for-integrators#favor-asynchronous-work-over-synchronous)**
    - Be able to return the WebHook response to GitHub in 10 seconds.
    - Process the Payload Asynchronously using [BranchProtect Azure Function](src/BranchProtect)

    Full Description on the App Settings can be found in Individual [README.md for WebHook project](src/GitBook.WebHook/README.md). When `AppSetting:UseQueue` is set to true, then the below 2 attributes need to be set mandatorily
      - QueueConnection - Full Connection String to a Storage Queue
      - QueueName - Queue Name which would be used by the Asynchronous processor.

4. **[BranchProtect](src/BranchProtect)** - A C# based Azure Function which has QueueTrigger. The QueueConnection and QueueName are set in the local.settings.json which are also set into the Application Configurations on Azure FunctionApp on Azure. The BrantchProtect Azure Function App has QueueTrigger based Function _BranchProtectionFunc_ which does below tasks
    - Retrieve the Base64 Encoded PayloadInfo (QueueTrigger based Functions receive Base64 encoded Messages as per [Official Azure Function Doc](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger?tabs=csharp#encoding))
    - Deserialize into `PayloadInfo` Object using _[Newtonsoft.Json v 13.0.1](https://www.nuget.org/packages/Newtonsoft.Json/13.0.1)_
    - Call `ProtectRepo` function in [GitHook.BusinessLayer.BranchProtection](src/GitHook.BusinessLayer/BranchProtection.cs)

The Complete Overview of the Processflow can be seen in [ProcessFlow](docs/ProcessFlow.md)

---

## **Issues Experienced**
1. The **default_branch** name sent in the Webhook calls from Github sometimes contains master and other times main. The Solution needed to be adjusted to pick the branches which exist in the repository and then take action on the existing branch rather than spurious / non-existing branch.
2. **Synchronous processing was a problem** - The solution was initially created using Synchronous call to GitHub REST API, however many times during the webhook processing, the Branch was actually not found through REST APIs. This was probably due to GitHub still in process of stabilizing the newly created Repository / Branch. The Solution was changed to Asynchronous processing mode by use of Azure Storage Queues.
3. **PayloadProcessor DI** - The Dependency Injection technique was used to define the processing mechanism using AppSettings only.
4. Usage of **ASP.NET Core 3.1 LTS** throughout - Azure Functions were failing to start / initialize / bind when ASP.NET Core 5.0 runtime was used, so whole solution was coded using ASP.NET Core 3.1 LTS which is very well supported by Azure Functions and Azure App Service both.
5. Typed Payload Object **GitHubPayload.cs** was created to Parse the GitHub Wehook JSON.
6. Move all the App Configurations from App Service to Azure KeyVault. Relevant README.md for [BranchProtect](src/BranchProtect/README.md) and [WebHook](src/GitHook.WebHook/README.md) have been updated. The Issue [#1 - Move all the App Configurations from App Service to Azure KeyVault](/../../issues/1) has also been closed.
---

## **Next Steps (Future Improvements)**
[#2](/../../issues/2). Create Azure App Services through GitHub Actions.

