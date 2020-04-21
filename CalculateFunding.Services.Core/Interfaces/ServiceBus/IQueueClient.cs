using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.ServiceBus
{
    public interface IQueueClient
    {
        Task CreateQueue(string entityPath);
        Task DeleteQueue(string entityPath);
        Task<CloudQueueMessage> GetMessage(string entityPath);
        Task DeleteMessage(string entityPath, CloudQueueMessage message);
        Task AddMessage(string entityPath, CloudQueueMessage message);
        Task<bool> Exists(string entityPath);
        void TimedOut();
    }
}
