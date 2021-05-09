using System;

namespace GitHook.Models
{
    public class PayloadInfo
    {
        public string ownerName { get; set; }

        public string orgName { get; set; }

        public string repoName { get; set; }

        public string branchName { get; set; }

        public long orgId { get; set; }

        public long repoId { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool Created { get; set; }

        public int openIssuesCount { get; set; }
    }
}
