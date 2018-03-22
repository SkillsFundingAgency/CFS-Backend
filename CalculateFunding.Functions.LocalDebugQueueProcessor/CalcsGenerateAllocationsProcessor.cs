using System.Threading.Tasks;
using CalculateFunding.Functions.CalcEngine.EventHub;
using CalculateFunding.Functions.Calcs.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    internal class CalcsGenerateAllocationsProcessor : BaseEventProcessor
    {
        protected override async Task OnMessages(EventData[] messages)
        {
            await OnCalcsGenerateAllocationResults.Run(messages);
        }
    }
}