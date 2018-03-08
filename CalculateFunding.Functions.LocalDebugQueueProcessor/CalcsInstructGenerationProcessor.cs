using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    internal class CalcsInstructGenerationProcessor : BaseEventProcessor
    {
        protected override async Task OnMessages(EventData[] messages)
        {
            await OnCalcsInstructAllocationResults.Run(messages);
        }
    }
}