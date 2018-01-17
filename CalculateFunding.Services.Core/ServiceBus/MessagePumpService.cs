using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class MessagePumpService : IMessagePumpService
    {
        private readonly string _connectionString;

        public MessagePumpService(ServiceBusSettings settings)
        {
            _connectionString = settings.ServiceBusConnectionString;
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
    }
}