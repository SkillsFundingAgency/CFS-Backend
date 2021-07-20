using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CalculateFunding.Runners.CalcEngine
{
    public abstract class ServiceBusQueueWorker<TMessage> : BackgroundService
    {
        protected ILogger<ServiceBusQueueWorker<TMessage>> Logger { get; }

        protected IConfiguration Configuration { get; }

        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        protected ServiceBusQueueWorker(IConfiguration configuration, IHostApplicationLifetime hostApplicationLifetime, ILogger<ServiceBusQueueWorker<TMessage>> logger)
        {
            Configuration = configuration;
            Logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string queueName = Configuration.GetValue<string>("ServiceBusSettings:QueueName");
            ServiceBusSessionProcessor messageProcessor = CreateServiceBusSessionProcessor(queueName);
            messageProcessor.ProcessMessageAsync += HandleMessageAsync;
            messageProcessor.ProcessErrorAsync += HandleReceivedExceptionAsync;

            ManagementClient managementClient = new ManagementClient(Configuration.GetValue<string>("ServiceBusSettings:ConnectionString"));
            QueueRuntimeInfo runtimeInfo = await managementClient.GetQueueRuntimeInfoAsync(queueName);
            long messageCount = runtimeInfo.MessageCountDetails.ActiveMessageCount;

            Logger.LogInformation($"Starting message pump on queue {queueName} in namespace {messageProcessor.FullyQualifiedNamespace}");
            await messageProcessor.StartProcessingAsync(stoppingToken);
            Logger.LogInformation("Message pump started");

            while (!stoppingToken.IsCancellationRequested && messageCount > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Configuration.GetValue<double>("ServiceBusSettings:MessageCountPollDelayInSeconds")));
                runtimeInfo = await managementClient.GetQueueRuntimeInfoAsync(queueName);
                messageCount = runtimeInfo.MessageCountDetails.ActiveMessageCount;
            }

            Logger.LogInformation("Closing message pump");
            await messageProcessor.CloseAsync(cancellationToken: stoppingToken);
            Logger.LogInformation("Message pump closed : {Time}", DateTimeOffset.UtcNow);

            Logger.LogInformation($"Stopping service bus listener for queue {queueName}.");
            _hostApplicationLifetime.StopApplication();
        }

        private ServiceBusSessionProcessor CreateServiceBusSessionProcessor(string queueName)
        {
            ServiceBusClient serviceBusClient = AuthenticateToAzureServiceBus();
            ServiceBusSessionProcessor messageProcessor = serviceBusClient.CreateSessionProcessor(queueName);
            return messageProcessor;
        }

        private ServiceBusClient AuthenticateToAzureServiceBus()
        {
            string connectionString = Configuration.GetValue<string>("ServiceBusSettings:ConnectionString");
            return new ServiceBusClient(connectionString);
        }

        private async Task HandleMessageAsync(ProcessSessionMessageEventArgs processMessageEventArgs)
        {
            try
            {
                string rawMessageBody = Encoding.UTF8.GetString(processMessageEventArgs.Message.Body.ToBytes().ToArray());
                Logger.LogInformation("Received message {MessageId} with body {MessageBody}",
                    processMessageEventArgs.Message.MessageId, rawMessageBody);

                TMessage message = JsonConvert.DeserializeObject<TMessage>(rawMessageBody);
                if (message != null)
                {
                    await ProcessMessage(message, processMessageEventArgs.Message.MessageId,
                        processMessageEventArgs.Message.ApplicationProperties,
                        processMessageEventArgs.CancellationToken);
                }
                else
                {
                    Logger.LogError(
                        "Unable to deserialize to message contract {ContractName} for message {MessageBody}",
                        typeof(TMessage), rawMessageBody);
                }

                Logger.LogInformation("Message {MessageId} processed", processMessageEventArgs.Message.MessageId);

                await processMessageEventArgs.CompleteMessageAsync(processMessageEventArgs.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unable to handle message");
                await processMessageEventArgs.CompleteMessageAsync(processMessageEventArgs.Message);
            }
        }

        private Task HandleReceivedExceptionAsync(ProcessErrorEventArgs exceptionEvent)
        {
            Logger.LogError(exceptionEvent.Exception, "Unable to process message");
            return Task.CompletedTask;
        }

        protected abstract Task ProcessMessage(TMessage order, string messageId, IReadOnlyDictionary<string, object> userProperties, CancellationToken cancellationToken);
    }
}