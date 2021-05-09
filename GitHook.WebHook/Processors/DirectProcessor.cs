using GitHook.BusinessLayer;
using GitHook.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace GitHook.Webhook.Processors
{
  public class DirectProcessor : IPayloadProcessor
  {
    private readonly ILogger<DirectProcessor> _logger;
    private readonly IConfiguration _configuration;
    private readonly BranchProtection _branchProtection;

    public DirectProcessor(
      ILogger<DirectProcessor> logger,
      BranchProtection branchProtection,
      IConfiguration configuration)
    {
      this._logger = logger ?? throw new ArgumentNullException(nameof (logger));
      this._branchProtection = branchProtection ?? throw new ArgumentNullException(nameof (branchProtection));
      this._configuration = configuration ?? throw new ArgumentNullException(nameof (configuration));
    }

    public bool ProcessPayload(PayloadInfo payloadInfo)
    {
      string teamName = this._configuration["AppSettings:GitHubTeamName"];
      string GitHubAppName = this._configuration["AppSettings:GitHubAppName"];
      string token = this._configuration["AppSettings.GitHubToken"];
      this._logger.LogInformation("GitHubAppName  is " + GitHubAppName);
      this._logger.LogInformation("GitHubTeamName  is " + teamName);
      try
      {
        this._logger.LogInformation(" Calling ProtectRepo directly " + payloadInfo.repoName + "/" + payloadInfo.branchName);
        this._branchProtection.ProtectRepo(payloadInfo, GitHubAppName, token, teamName).GetAwaiter().GetResult();
      }
      catch (Exception ex)
      {
        this._logger.LogInformation(string.Format(" Calling ProtectRepo directly failed {0}", (object) ex));
        return false;
      }
      return true;
    }
  }
}
