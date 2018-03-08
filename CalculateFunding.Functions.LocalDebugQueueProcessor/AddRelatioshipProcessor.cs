using System.Threading.Tasks;
using CalculateFunding.Functions.Specs.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    internal class AddRelatioshipProcessor : BaseEventProcessor
    {
        protected override async Task OnMessages(EventData[] messages)
        {
            await OnAddRelatioshipEvent.Run(messages);
        }
    }
}