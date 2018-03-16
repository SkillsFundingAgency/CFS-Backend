using System.Threading.Tasks;
using CalculateFunding.Functions.Calcs.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    internal class CalcsAddRelationshipToBuildProjectProcessor : BaseEventProcessor
    {
        protected override async Task OnMessages(EventData[] messages)
        {
            await CalcsAddRelationshipToBuildProject.Run(messages);
        }
    }
}