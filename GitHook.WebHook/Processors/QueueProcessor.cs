using Azure;
using Azure.Storage.Queues;
using GitHook.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;

namespace GitHook.Webhook.Processors
{
    public class QueueProcessor : IPayloadProcessor
    {
        private readonly ILogger<QueueProcessor> _logger;
        private readonly IConfiguration _configuration;

        public QueueProcessor(ILogger<QueueProcessor> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public bool ProcessPayload(PayloadInfo payloadInfo)
        {
            try
            {
                string payloadInfoJson = JsonConvert.SerializeObject((object)payloadInfo);
                _logger.LogInformation("Retrieving Queue Connection properties");


                string connectionString = _configuration["AppSettings:QueueConnection"];
                string queueName = _configuration["AppSettings:QueueName"];
                _logger.LogDebug(" queueConnectionString is " + connectionString);
                _logger.LogDebug(" queueName is " + queueName);
                
                
                _logger.LogInformation("Creating Queue Connection");
                QueueServiceClient queueServiceClient = new QueueServiceClient(connectionString, new QueueClientOptions()
                {
                    MessageEncoding = QueueMessageEncoding.Base64
                });
                _logger.LogInformation("Created Queue Connection");
                
                
                string base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadInfoJson));
                
                
                _logger.LogInformation("Retrieve the Storage Queue Client");
                QueueClient queueClient = queueServiceClient.GetQueueClient(queueName);
                _logger.LogInformation("Create the queue if it doesn't exist");

                Response response = queueClient.CreateIfNotExists();
                
                if (response != null)
                    _logger.LogInformation($"Created queue {queueName}! Response is {response.Status}");
                else
                    _logger.LogInformation("The queue " + queueName + " already exists");
                
                
                _logger.LogInformation("Send the base64 encoded Json Payload " + payloadInfoJson);
                _logger.LogInformation("the Message is queued " + queueClient.SendMessage(base64String).Value.MessageId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Eeception occurred while queueing {ex}");
                return false;
            }
        }
    }
}
