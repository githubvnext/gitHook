using GitHook.Models;

namespace GitHook.Webhook.Processors
{
  public interface IPayloadProcessor
  {
    bool ProcessPayload(PayloadInfo payloadInfo);
  }
}
