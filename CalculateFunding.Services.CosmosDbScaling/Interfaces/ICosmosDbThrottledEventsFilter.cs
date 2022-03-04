using Azure.Messaging.EventHubs;
using System.Collections.Generic;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbThrottledEventsFilter
    {
        IEnumerable<string> GetUniqueCosmosDBContainerNamesFromEventData(IEnumerable<EventData> events);
    }
}
