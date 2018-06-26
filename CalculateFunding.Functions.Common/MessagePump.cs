using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Functions.Common
{
    public class MessagePump 
    {
        private readonly string _connectionString;

        public MessagePump(string connectionString)
        {
            _connectionString = connectionString;
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