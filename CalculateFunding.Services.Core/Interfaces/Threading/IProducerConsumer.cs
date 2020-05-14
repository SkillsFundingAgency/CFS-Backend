using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Interfaces.Threading
{
    public interface IProducerConsumer
    {
        Task Run(object context);
        
        int ConsumerPoolSize { get; }
        
        int ChannelBounds { get; }
        
        CancellationToken CancellationToken { get; }
    }
}