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

        public SubscriptionClient GetSubscriptionClient(string topicName, string subscriptionName, ReceiveMode receiveMode = ReceiveMode.PeekLock)
        {
            return new SubscriptionClient(_connectionString, topicName, subscriptionName, receiveMode, new RetryExponential(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), 10));
        }

        public async Task ReceiveAsync(string topicName, string subscriptionName, Func<string, Task> handler)
        {
            SubscriptionClient client = GetSubscriptionClient(topicName, subscriptionName);
   
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

       public Task ReceiveAsync(string topicName, string subscriptionName, Func<Message, Task> handler, Action<Exception> onError, ReceiveMode receiveMode = ReceiveMode.PeekLock)
       {
            return Task.Run(() =>
            {
                SubscriptionClient client = GetSubscriptionClient(topicName, subscriptionName, receiveMode);

                var options = new MessageHandlerOptions(e =>
                {
                    onError(e.Exception);
                   
                    return Task.CompletedTask;
                })
                {
                    AutoComplete = true,
                    MaxAutoRenewDuration = TimeSpan.FromMinutes(30),
                    MaxConcurrentCalls = 1
                };

                client.RegisterMessageHandler(
                    async (message, token) =>
                    {
                        await handler(message);
                        //await client.CompleteAsync(message.SystemProperties.LockToken);
                    },
                    options);
            });           
        }
    }
}