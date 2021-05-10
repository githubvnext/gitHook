using GitHook.Models;
using Microsoft.Extensions.Logging;
using Octokit;

using System;
using System.Collections.Generic;
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
            _logger.LogInformation((await client.User.Get(payloadInfo.ownerName)).Followers.ToString() + " folks love " + payloadInfo.ownerName + "!");
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
                    foreach (Repository repository in repositoryResult.Items)
                    {
                        Repository repo = repository;
                        _logger.LogInformation("Get all the Branches in the Repository");
                        IReadOnlyList<Branch> all = await client.Repository.Branch.GetAll(payloadInfo.orgName, repo.Name);
                        if (all.Count > 0)
                        {
                            foreach (Branch branch in (IEnumerable<Branch>)all)
                            {
                                if (!branch.Protected)
                                {
                                    ProtectMasterBranch(client, repo, payloadInfo, teamName).GetAwaiter().GetResult();
                                    CreateIssue(client, repo, payloadInfo);
                                }
                                else if (branch.Protected)
                                {
                                    BranchProtectionSettings branchProtection = await client.Repository.Branch.GetBranchProtection(repo.Id, branch.Name);
                                    _logger.LogInformation("Branch is already protected as below");
                                    _logger.LogInformation(branchProtection.ToString());
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
                client = null;
            }
        }

        private async Task ProtectMasterBranch(
          GitHubClient client,
          Repository repo,
          PayloadInfo payloadInfo,
          string teamName)
        {
            IReadOnlyList<Team> teams = await client.Organization.Team.GetAll((await client.Organization.Get(payloadInfo.orgName)).Login);
            List<Team> teamList1 = new List<Team>();
            foreach (Team team in teams)
            {
                if (team.Name == teamName)
                    teamList1.Add(team);
            }
            try
            {
                IReadOnlyList<Team> teamList2 = await client.Repository.Branch.AddProtectedBranchTeamRestrictions(repo.Id, payloadInfo.branchName, new BranchProtectionTeamCollection((IList<string>)new List<string>()
                {
                  teamName
                }));
                IReadOnlyList<User> userList = await client.Repository.Branch.AddProtectedBranchUserRestrictions(repo.Id, payloadInfo.branchName, new BranchProtectionUserCollection((IList<string>)new List<string>()
                {
                    payloadInfo.ownerName
                }));
                IReadOnlyList<string> stringList = await client.Repository.Branch.AddRequiredStatusChecksContexts(payloadInfo.orgName, payloadInfo.orgName, payloadInfo.branchName, (IReadOnlyList<string>)new List<string>());
                EnforceAdmins enforceAdmins = await client.Repository.Branch.AddAdminEnforcement(repo.Id, payloadInfo.branchName);
                _logger.LogInformation((await client.Repository.Branch.GetBranchProtection(repo.Id, payloadInfo.branchName)).ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating Branch Protection {ex}" );
            }
        }

        private void CreateIssue(GitHubClient client, Repository repo, PayloadInfo payloadInfo)
        {
            try
            {
                NewIssue newIssue = new NewIssue(" Branch " + payloadInfo.branchName + " Protection settings Applied");
                newIssue.Body = "The Branch Protection is applied by @" + payloadInfo.ownerName + "\r\n The Branch Protection settings can't be applied using REST API, there is no REST API to Create Branch Protection Settings through REST API, Octokit, Octokit.GraphSQL either";
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
}
