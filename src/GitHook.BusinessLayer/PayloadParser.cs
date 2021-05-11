using GitHook.Models;
using Newtonsoft.Json;
using System;

namespace GitHook.BusinessLayer
{
    public class PayloadParser
    {

        public PayloadInfo TexttoPayloadInfo(string payloadJson)
        {
            GitHubPayload githubPayload= JsonConvert.DeserializeObject<GitHubPayload>(payloadJson);

            return PayloadtoPayloadInfo(githubPayload);

        }
        public PayloadInfo PayloadtoPayloadInfo(GitHubPayload payload)
        {
            return new PayloadInfo()
            {
                orgName = payload.organization.login,
                orgId = payload.organization.id,
                repoName = payload.repository.name,
                repoId = payload.repository.id,
                branchName = payload.repository.default_branch,
                openIssuesCount = payload.repository.open_issues,
                CreatedAt = payload.repository.created_at,
                Created = payload.action == "created",
                ownerName = payload.sender.login
            };
        }
    }
}
