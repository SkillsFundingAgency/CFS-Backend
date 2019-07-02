using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.CosmosDbScaling.Interfaces
{
    public interface ICosmosDbThrottledEventsFilter
    {
        IEnumerable<string> GetUniqueCosmosDbCollectionNamesFromEventData(IEnumerable<EventData> events);
    }
}
