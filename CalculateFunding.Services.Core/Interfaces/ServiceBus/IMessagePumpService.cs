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
        SubscriptionClient GetSubscriptionClient(string topicName, string subscriptionName);

        Task ReceiveAsync(string topicName, string subscriptionName, Func<string, Task> handler);

        Task ReceiveAsync(string topicName, string subscriptionName, Func<Message, Task> handler);
    }
}
