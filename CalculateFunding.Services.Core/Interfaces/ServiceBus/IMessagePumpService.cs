using CalculateFunding.Models;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IMessagePumpService
    {
        Task ReceiveAsync(string topicName, string subscriptionName, Func<string, Task> handler);

        Task ReceiveAsync(string topicName, string subscriptionName, Func<Message, Task> handler, Action<Exception> onError, ReceiveMode receiveMode = ReceiveMode.PeekLock);

        SubscriptionClient GetSubscriptionClient(string topicName, string subscriptionName, ReceiveMode receiveMode = ReceiveMode.PeekLock);
    }
}
