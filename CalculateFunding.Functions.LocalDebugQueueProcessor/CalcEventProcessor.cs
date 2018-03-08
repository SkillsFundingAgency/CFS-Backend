using System.Threading.Tasks;
using CalculateFunding.Functions.Results.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    internal class CalcEventProcessor : BaseEventProcessor
    {
        protected override async Task OnMessages(EventData[] messages)
        {
            await OnProviderDataEvent.Run(messages);
        }
    }
}