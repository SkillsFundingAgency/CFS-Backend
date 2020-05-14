using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace CalculateFunding.Services.Core.Interfaces.Threading
{
    public interface IProducerConsumerFactory
    {
        IProducerConsumer CreateProducerConsumer<TItem>(Func<CancellationToken, dynamic, Task<(bool Complete, TItem Item)>> producer,
            Func<CancellationToken, dynamic, TItem, Task> consumer,
            int channelBounds,
            int consumerPoolSize,
            ILogger logger,
            CancellationToken cancellationToken = default);
    }
}