## **GitHub Web Hook**
GitHook WebHook based on Asynchronous WebHook processing

GitHub Actions Status 

[![FunctionDeploy](https://github.com/githubvnext/gitHook/actions/workflows/function-ci.yml/badge.svg)](https://github.com/githubvnext/gitHook/actions/workflows/function-ci.yml)

[![WebHookDeploy](https://github.com/githubvnext/gitHook/actions/workflows/webhook-ci.yml/badge.svg)](https://github.com/githubvnext/gitHook/actions/workflows/webhook-ci.yml)



The Components are described as in below diagram

![Process Flow](docs/GitHubWebhookProcessFlow.png)


This Repository contains an Web Api based in C# ASP.NET Core Web Api templating. 

There are 4 Visual Studio Projects which through which the objectives below are achieved. 

- Use GitHub Powerful API to Protect Master / main branches
- Use Web API to get webhook Calls from GitHub installation
- Web API queues the payload in Azure Storage Queues
- Azure Functions (Queue Trigger) processes the GitHub WebHook payloads

### **Projects in the repository**

1.  **GitHook.Models** -  A generic common model _PayloadInfo.cs_ which is used for Seriliazing and Deserializing the payload between different components
2. **GitHook.BusinessLayer** - A C# Class Library that contains _BranchProtection_ class to manage 2 objectives
    -  Protect the Branch (Under process as appropriate REST API to protect a branch directly from REST API couldn't be discovered using GitHub REST API Documentation)
    - Create an Issue describing Branch Protection applied using @mention tag.

3. **GitHook.WebHook** - A ASP.NET Core Web API (C#) Class Library that contains _GitHookController_ REST API Controller to Receive the Web Hook Calls from GitHub. The WebAPI project has capabilities to do Asynchronous and Synchronous Branch Protection function.   - The GitHook.WebHook project contains Dependency Injection based IPayloadProcessor which is set during Application [GitHook.WebHook/Startup.cs](GitHook.WebHook/Startup.cs). If UseQueue is set, then QueueProcessor is used, else DirectProcessor is used. It is recommended to set "UseQueue" to true to achieve below objectives as per [GitHub WebHook Best Practices Document](https://docs.github.com/en/rest/guides/best-practices-for-integrators#favor-asynchronous-work-over-synchronous)
    - Be able to return the WebHook response to GitHub in 10 seconds.
    - Process the Payload Asynchronously using [BranchProtect Azure Function](BranchProtect)

    > When **AppSetting:UseQueue** is set to true, then the below 2 attributes need to be set mandatorily
    > - QueueConnection - Full Connection String to a Storage Queue
    > - QueueName - Queue Name which would be used by the Asynchronous processor.

4. **BranchProtect** - A C# based Azure Function which has QueueTrigger. The QueueConnection and QueueName are set in the local.settings.json which are also set into the Application Configurations on Azure FunctionApp on Azure. The BrantchProtect Azure Function App has QueueTrigger based Function _BranchProtectionFunc_ which does below tasks
    - Retrieve the Base64 Encoded PayloadInfo
    - Deserialize into PayloadInfo Object
    - Call GitHub REST API below through [octokit.net Nuget Package](https://www.nuget.org/packages/Octokit/0.50.0) version 0.5.0
      - [Get Repository](https://docs.github.com/en/rest/reference/repos#get-a-repository)
      - [Get Branch Protection](https://docs.github.com/en/rest/reference/repos#get-branch-protection)
      - [Update Branch Protection](https://docs.github.com/en/rest/reference/repos#update-branch-protection)
      - [Retrieve Teams](https://docs.github.com/en/rest/reference/teams#list-teams)
      - [Add team access restrictions](https://docs.github.com/en/rest/reference/repos#add-team-access-restrictions)
      - [Add user access restrictions](https://docs.github.com/en/rest/reference/repos#add-user-access-restrictions)
      - [Add status check contexts](https://docs.github.com/en/rest/reference/repos#add-status-check-contexts)
      - [Set admin branch protection](https://docs.github.com/en/rest/reference/repos#set-admin-branch-protection)
      - [Create an Issue](https://docs.github.com/en/rest/reference/issues#create-an-issue)


The Complete Overview of the Processflow can be seen in [ProcessFlow](docs/ProcessFlow.md)