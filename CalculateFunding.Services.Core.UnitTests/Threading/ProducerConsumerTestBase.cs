using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Threading
{
    public abstract class ProducerConsumerTestBase
    {
        protected ConcurrentBag<string> ProcessedItems;
        protected ConcurrentQueue<string> ItemsToProduce;

        [TestInitialize]
        public void ProducerConsumerTestBaseSetUp()
        {
            ProcessedItems = new ConcurrentBag<string>();
            ItemsToProduce = new ConcurrentQueue<string>();
        }
        
        protected Task<(bool, string)> Producer(CancellationToken cancellationToken, dynamic context)
        {
            if (ItemsToProduce.TryDequeue(out string item))
            {
                return Task.FromResult((false, item));
            }

            return Task.FromResult((true, (string) null));
        }

        protected Task Consumer(CancellationToken cancellationToken, dynamic context, string item)
        {
            ProcessedItems.Add(item);
            
            return Task.CompletedTask;
        }
        
        protected int NewRandomNumber() => new RandomNumberBetween(1, 6);
    }
}