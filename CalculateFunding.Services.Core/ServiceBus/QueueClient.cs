using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.ServiceBus
{
    public class QueueClient : IQueueClient
    {
        private static readonly ConcurrentDictionary<string, CloudQueueClient> _queueClients = new ConcurrentDictionary<string, CloudQueueClient>();
        private CloudStorageAccount _storageAccount;

        public QueueClient(string connectionString)
        {
            _storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        public CloudQueueClient GetQueueClient(string entityPath)
        {
            return _queueClients.GetOrAdd(entityPath, (key) =>
            {
                return _storageAccount.CreateCloudQueueClient();
            });
        }

        public async Task CreateQueue(string entityPath)
        {
            CloudQueue queue = GetQueueClient(entityPath).GetQueueReference(entityPath);

            await queue.CreateIfNotExistsAsync();
        }

        public async Task<bool> Exists(string entityPath)
        {
            CloudQueue queue = GetQueueClient(entityPath).GetQueueReference(entityPath);

            return await queue.ExistsAsync();
        }

        public async Task DeleteQueue(string entityPath)
        {
            CloudQueue queue = GetQueueClient(entityPath).GetQueueReference(entityPath);

            await queue.DeleteAsync();
        }

        public async Task<CloudQueueMessage> GetMessage(string entityPath)
        {
            CloudQueue queue = GetQueueClient(entityPath).GetQueueReference(entityPath);

            return await queue.GetMessageAsync();
        }

        public async Task DeleteMessage(string entityPath, CloudQueueMessage message)
        {
            CloudQueue queue = GetQueueClient(entityPath).GetQueueReference(entityPath);

            await queue.DeleteMessageAsync(message);
        }

        public async Task AddMessage(string entityPath, CloudQueueMessage message)
        {
            CloudQueue queue = GetQueueClient(entityPath).GetQueueReference(entityPath);

            await queue.CreateIfNotExistsAsync();

            await queue.AddMessageAsync(message);
        }

        public void TimedOut()
        {

        }
    }
}
