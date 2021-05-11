using GitHook.Models;
using Microsoft.Extensions.Logging;
using Octokit;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitHook.BusinessLayer
{
    public class BranchProtection
    {
        private readonly ILogger _logger;

        public BranchProtection(ILogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task ProtectRepo(
          PayloadInfo payloadInfo,
          string GitHubAppName,
          string token,
          string teamName)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue(GitHubAppName));
            client.Credentials = new Credentials(token);
            var user = await client.User.Get(payloadInfo.ownerName);
            _logger.LogInformation(user.Followers.ToString() + " folks love " + payloadInfo.ownerName + "!");
            _logger.LogInformation("Initialize a new instance of the SearchRepositoriesRequest class");
            SearchRepositoriesRequest search = new SearchRepositoriesRequest(payloadInfo.repoName);
            search.User = payloadInfo.orgName;
            try
            {
                _logger.LogInformation("Search Repo using SearchRepositoriesRequest");
                SearchRepositoryResult repositoryResult = await client.Search.SearchRepo(search);
                _logger.LogInformation($"Search Repo count {repositoryResult.TotalCount}");
                if (repositoryResult.TotalCount >= 0)
                {
                    _logger.LogInformation("found the repository(s)");
                    foreach (Octokit.Repository repository in repositoryResult.Items)
                    {
                        Octokit.Repository repo = repository;
                        _logger.LogInformation("Get all the Branches in the Repository");
                        IReadOnlyList<Branch> allBranches = await client.Repository.Branch.GetAll(payloadInfo.orgName, repo.Name);
                        if (allBranches.Count > 0)
                        {
                            foreach (Branch branch in allBranches)
                            {
                                if (!branch.Protected)
                                {
                                    var branchProtectionSettings = ProtectBranch(client, repo, payloadInfo, teamName).GetAwaiter().GetResult();
                                    CreateIssue(client, repo, payloadInfo, branchProtectionSettings);
                                }
                                else if (branch.Protected)
                                {
                                    BranchProtectionSettings branchProtection = await client.Repository.Branch.GetBranchProtection(repo.Id, branch.Name);
                                    _logger.LogInformation("Branch is already protected as below");
                                    _logger.LogInformation(branchProtection.GetString());
                                }
                            }
                        }
                        else
                            _logger.LogInformation("There are no branches created ! No Branch Protection can be applied");
                        repo = null;
                    }
                    client = null;
                }
                else
                {
                    _logger.LogInformation("0 repository[s] found");
                    client = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while applying branch protection settings {ex}");
            }
        }

        private async Task<BranchProtectionSettings> ProtectBranch(
          GitHubClient client,
          Octokit.Repository repo,
          PayloadInfo payloadInfo,
          string teamName)
        {

            try
            {
                List<string> contexts = new List<string>();
                //contexts.Add("contexts");
                BranchProtectionRequiredStatusChecksUpdate requiredStatusChecks = new BranchProtectionRequiredStatusChecksUpdate(true, contexts);
                BranchProtectionRequiredReviewsUpdate requiredPullRequestReviews = new BranchProtectionRequiredReviewsUpdate(true, true, 2);
                BranchProtectionPushRestrictionsUpdate restrictions = new BranchProtectionPushRestrictionsUpdate();

                IReadOnlyList<Team> teams = await client.Organization.Team.GetAll((await client.Organization.Get(payloadInfo.orgName)).Login);
                //List<Team> teamList1 = new List<Team>();
                foreach (Team team in teams)
                {
                    if (team.Name == teamName)
                        restrictions.Teams.Add(team.Name);
                }



                bool enforceAdmins = true;


                BranchProtectionSettingsUpdate settingsUpdate = new BranchProtectionSettingsUpdate(
                    requiredStatusChecks,
                    requiredPullRequestReviews,
                    restrictions,
                    enforceAdmins);
                _logger.LogInformation("Calling UpdateBranchProtection");

                await client.Repository.Branch.UpdateBranchProtection(repo.Id, payloadInfo.branchName, settingsUpdate);

                _logger.LogInformation("Call to UpdateBranchProtection completed");
                var branchProtectionSettings = await client.Repository.Branch.GetBranchProtection(repo.Id, payloadInfo.branchName);
                _logger.LogInformation($"Get branchProtectionSettings Result:  {branchProtectionSettings.GetString()}");
                return branchProtectionSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Branch Protection {ex}");
                return null;
            }
        }

        private void CreateIssue(GitHubClient client, Octokit.Repository repo, PayloadInfo payloadInfo, BranchProtectionSettings branchProtectionSettings)
        {
            try
            {
                NewIssue newIssue = new NewIssue($"{payloadInfo.branchName } Branch Protection settings Applied");
                newIssue.Body = $"The Branch Protection is applied by @{payloadInfo.ownerName} as below: "  + branchProtectionSettings.GetString();
                newIssue.Assignees.Add(payloadInfo.ownerName);
                _logger.LogInformation("Creating Issue Now");
                Issue result = client.Issue.Create(payloadInfo.orgName, repo.Name, newIssue).GetAwaiter().GetResult();
                _logger.LogInformation($"Created Issue {result.Number}  {result.Url}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating the first issues " + ex.ToString());
            }
        }
    }
    public static class BranchProtectionSettingsExtension
    {
        public static string GetString(this BranchProtectionSettings branchProtectionSettings)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"\r\nThe **Branch Protection settings** were applied using Octokit.Net through GitHub REST API as below:-");
            sb.Append($"\r\n1. **Pull Request Restrictions**");
            sb.Append($"\r\n    - Dismissal Restrictions = `Not Applicable`");
            sb.Append($"\r\n    - Dismiss Stale Reviews = `{branchProtectionSettings.RequiredPullRequestReviews.DismissStaleReviews}`");
            sb.Append($"\r\n    - Require Code Owner Reviews = `{branchProtectionSettings.RequiredPullRequestReviews.RequireCodeOwnerReviews}`");
            sb.Append($"\r\n    - Required Approving Review Count = `{branchProtectionSettings.RequiredPullRequestReviews.RequiredApprovingReviewCount}`");

            sb.Append($"\r\n\r\n2. **Required Status Checks Strict** = `{branchProtectionSettings.RequiredStatusChecks.Strict}`");
            if (branchProtectionSettings.RequiredStatusChecks.Contexts.Count > 0)
            {
                sb.Append($"\r\n  - Required Status Checks Contexts : ");
                int i = 1;
                foreach (var context in branchProtectionSettings.RequiredStatusChecks.Contexts)
                {
                    sb.Append($"\r\n   {i++}. {context} ");
                }
            }
            sb.Append($"\r\n\r\n");

            if (branchProtectionSettings.Restrictions.Teams.Count > 0)
            {
                sb.Append($"\r\n  - Required Team Restrictions : ");
                int i = 1;
                foreach (var team in branchProtectionSettings.Restrictions.Teams)
                {
                    sb.Append($"\r\n   {i++}. {team} ");
                }
            }
            sb.Append($"\r\n");
            sb.Append($"\r\n3. EnforceAdmins = `{branchProtectionSettings.EnforceAdmins.Enabled}`");
            sb.Append($"\r\n\r\n");
            sb.Append($"\r\n\r\n");


            return sb.ToString();
        }
    }
}
