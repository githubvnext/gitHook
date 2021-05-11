using GitHook.BusinessLayer;
using GitHook.Models;
using GitHook.Webhook.Processors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GitHook.WebHook.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GitHookController : Controller
    {
        private const string Sha1Prefix = "sha1=";
        private readonly ILogger<GitHookController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IPayloadProcessor _payloadProcessor;
        private readonly PayloadParser _payloadParser;
        public GitHookController(
          ILogger<GitHookController> logger,
          IConfiguration configuration,
          IPayloadProcessor payloadProcessor,
          PayloadParser payloadParser)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _payloadProcessor = payloadProcessor ?? throw new ArgumentNullException(nameof(payloadProcessor));
            _payloadParser = payloadParser ?? throw new ArgumentNullException(nameof(payloadParser));
        }

        [HttpPost("")]
        public async Task<IActionResult> Receive()
        {
            
             
            Request.Headers.TryGetValue("X-GitHub-Event", out StringValues eventName);
            Request.Headers.TryGetValue("X-GitHub-Delivery", out StringValues delivery);
            Request.Headers.TryGetValue("User-Agent", out StringValues userAgent);
            _logger.LogInformation($"Delivery ID is {delivery}");
            if (!eventName.ToString().Equals("repository", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(string.Format("Escaping this call as it is not for repository creation ! The Event Name is {0}", (object)eventName));
                return Ok();
            }
            using (StreamReader reader = new StreamReader(Request.Body))
            {
                string payloadJson = await reader.ReadToEndAsync();
                _logger.LogDebug(payloadJson);
                 
                Request.Headers.TryGetValue("X-Hub-Signature", out StringValues signature);
                if (IsGithubPushAllowed(payloadJson, eventName, signature, userAgent))
                {
                    _logger.LogInformation("GitHub Push HMAC is OK");
                    PayloadInfo payloadInfo = _payloadParser.TexttoPayloadInfo(payloadJson);
                    if (payloadInfo.Created)
                    {
                        _logger.LogInformation("Calling to Protect Repo " + payloadInfo.repoName);
                        if (_payloadProcessor.ProcessPayload(payloadInfo))
                        {
                            _logger.LogInformation("Calling to Protect Repo completed " + payloadInfo.repoName);
                            return Ok();
                        }
                        _logger.LogError("Queueing Payload failed");
                        return StatusCode(500);
                    }
                    _logger.LogInformation("NOT Calling to Protect Repo  " + payloadInfo.repoName);
                    return Ok();
                }
                _logger.LogInformation(string.Format("HMAC Signature not OK or userAgent is not Authorized {0} !", (object)signature));
                return Unauthorized();
            }
        }

        private bool IsGithubPushAllowed(
          string payload,
          string eventName,
          string signatureWithPrefix,
          string userAgent)
        {
            _logger.LogInformation("signatureWithPrefix is " + signatureWithPrefix);
            _logger.LogInformation("eventname is " + eventName);
            _logger.LogInformation("payload is " + payload);
            _logger.LogInformation("userAgent is " + userAgent);
            if (string.IsNullOrWhiteSpace(payload))
                throw new ArgumentNullException(nameof(payload));
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));
            if (string.IsNullOrWhiteSpace(signatureWithPrefix))
                throw new ArgumentNullException(nameof(signatureWithPrefix));
            if (string.IsNullOrWhiteSpace(userAgent))
                throw new ArgumentNullException(nameof(userAgent));
            if (!userAgent.StartsWith("GitHub-Hookshot/", StringComparison.InvariantCultureIgnoreCase)                  || 
                ! signatureWithPrefix.StartsWith(Sha1Prefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("The Hook call is not made from valid Github source");

                //The Hook call is not made from valid Github source
                return false;
            }
                
            string signatureOnly = signatureWithPrefix.Substring(Sha1Prefix.Length);
            byte[] GitHubSecretBytes = Encoding.ASCII.GetBytes(_configuration["AppSettings:GitHubSecret"]);
            byte[] payloadBytes = Encoding.ASCII.GetBytes(payload);
            using (HMACSHA1 hmacshA1 = new HMACSHA1(GitHubSecretBytes))
            {
                if (ToHexString(hmacshA1.ComputeHash(payloadBytes)).Equals(signatureOnly))
                    return true;
            }
            return false;
        }

        public static string ToHexString(byte[] bytes)
        {
            StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
            foreach (byte num in bytes)
                stringBuilder.AppendFormat("{0:x2}", (object)num);
            return stringBuilder.ToString();
        }
    }
}


