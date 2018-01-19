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

        SubscriptionClient _client;

        public MessagePumpService(ServiceBusSettings settings)
        {
            _connectionString = settings.ServiceBusConnectionString;
        }

        public async Task CloseAsync()
        {
            if(!_client.IsClosedOrClosing)
                await _client.CloseAsync();
        }

        public SubscriptionClient GetSubscriptionClient(string topicName, string subscriptionName)
        {
            return new SubscriptionClient(_connectionString, topicName, subscriptionName);
        }

        public async Task ReceiveAsync(string topicName, string subscriptionName, Func<string, Task> handler)
        {
            _client = GetSubscriptionClient(topicName, subscriptionName);

            _client.RegisterMessageHandler(
                async (message, token) =>
                {
                    await handler(Encoding.UTF8.GetString(message.Body));
                    
                    await _client.CompleteAsync(message.SystemProperties.LockToken);
                }, 
                async args =>
                {
                
                });

            Thread.Sleep(60 * 1000);

            await _client.CloseAsync();
        }

       public Task ReceiveAsync(string topicName, string subscriptionName, Func<Message, Task> handler, Action<Exception> onError)
       {
            return Task.Run(() =>
            {
                _client = GetSubscriptionClient(topicName, subscriptionName);
                var options = new MessageHandlerOptions(e =>
                {
                    onError(e.Exception);
                    return Task.CompletedTask;
                })
                {
                    AutoComplete = false,
                    MaxAutoRenewDuration = TimeSpan.FromMinutes(1)
                };

                _client.RegisterMessageHandler(
                    async (message, token) =>
                    {
                        await handler(message);

                        await _client.CompleteAsync(message.SystemProperties.LockToken);
                    },
                    options);

                Thread.Sleep(10 * 1000);

            });           
        }
    }
}