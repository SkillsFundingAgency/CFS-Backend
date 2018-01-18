using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class MessagePumpService : IMessagePumpService
    {
        private readonly string _connectionString;
        private readonly ICorrelationIdProvider _correlationIdProvider;

        public MessagePumpService(ServiceBusSettings settings, ICorrelationIdProvider correlationIdProvider)
        {
            _connectionString = settings.ServiceBusConnectionString;
            _correlationIdProvider = correlationIdProvider;
        }

        public SubscriptionClient GetSubscriptionClient(string topicName, string subscriptionName)
        {
            return new SubscriptionClient(_connectionString, topicName, subscriptionName);
        }

        public async Task ReceiveAsync(string topicName, string subscriptionName, Func<string, Task> handler)
        {
            var client = GetSubscriptionClient(topicName, subscriptionName);

            client.RegisterMessageHandler(
                async (message, token) =>
                {
                    await handler(Encoding.UTF8.GetString(message.Body));
                    await client.CompleteAsync(message.SystemProperties.LockToken);
                }, 
                async args =>
                {
                
                });

            Thread.Sleep(60 * 1000);

            await client.CloseAsync();
        }

        public async Task ReceiveAsync(string topicName, string subscriptionName, Func<Message, Task> handler)
        {
            var client = GetSubscriptionClient(topicName, subscriptionName);

            client.RegisterMessageHandler(
                async (message, token) =>
                {
                    if (message.UserProperties.ContainsKey("sfa-correlationId"))
                    {
                        var correlationId = message.UserProperties["sfa-correlationId"].ToString();
                        _correlationIdProvider.SetCorrelationId(correlationId);
                    }

                    await handler(message);
                    await client.CompleteAsync(message.SystemProperties.LockToken);
                },
                async args =>
                {

                });

            Thread.Sleep(60 * 1000);

            await client.CloseAsync();
        }
    }
}