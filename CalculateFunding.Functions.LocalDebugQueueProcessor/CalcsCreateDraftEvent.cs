using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    public class CalcsCreateDraftEvent : BaseEventProcessor
    {
        protected override async Task OnMessages(EventData[] messages)
        {
            await OnCalcsCreateDraftEvent.Run(messages);
        }
    }
}