using System;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.Threading;
using Serilog;

namespace CalculateFunding.Services.Core.Threading
{
    public class ProducerConsumerFactory : IProducerConsumerFactory
    {
        public IProducerConsumer CreateProducerConsumer<TItem>(Func<CancellationToken, dynamic, Task<(bool Complete, TItem Item)>> producer, 
            Func<CancellationToken, dynamic, TItem, Task> consumer, 
            int channelBounds, 
            int consumerPoolSize, 
            ILogger logger)
        {
            return new ProducerConsumer<TItem>(producer,
                consumer,
                channelBounds,
                consumerPoolSize,
                logger);
        }
    }
}