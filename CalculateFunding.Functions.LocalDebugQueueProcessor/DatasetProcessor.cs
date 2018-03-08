using System.Threading.Tasks;
using CalculateFunding.Functions.Datasets.EventHub;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Functions.LocalDebugQueueProcessor
{
    public class DatasetProcessor : BaseEventProcessor
    {
        protected override async Task OnMessages(EventData[] messages)
        {
            await OnDatasetEvent.Run(messages);
        }
    }
}