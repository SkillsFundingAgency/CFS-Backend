using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IServiceBusService : IQueueService
    {
        Task CreateTopic(string topicName);

        Task DeleteTopic(string topicName);

        Task CreateSubscription(string topicName, string subscriptionName);

        Task DeleteSubscription(string topicName, string subscriptionName);
    }
}
