using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AzureManagement = Microsoft.Azure.ServiceBus.Management;
using AzureServiceBus = Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IManagementClient
    {
        ITopicClient GetTopicClient(string topicName);

        AzureServiceBus.IQueueClient GetQueueClient(string queueName);

        Task CreateSubscription(string topicName, string subscriptionName);

        Task DeleteSubscription(string topicName, string subscriptionName);

        Task<AzureManagement.QueueDescription> CreateQueue(string queuePath);

        Task DeleteQueue(string queuePath);

        Task<AzureManagement.TopicDescription> CreateTopic(string topicName);

        Task DeleteTopic(string topicName);
    }
}
