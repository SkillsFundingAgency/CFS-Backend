using System;
using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbThrottledEventsFilter : ICosmosDbThrottledEventsFilter
    {
        public IEnumerable<string> GetUniqueCosmosDbCollectionNamesFromEventData(IEnumerable<EventData> events)
        {
            Guard.ArgumentNotNull(events, nameof(events));

            HashSet<string> collections = new HashSet<string>();

            foreach (EventData eventData in events)
            {
                if(eventData.Properties.ContainsKey("statusCode") &&
                    eventData.Properties["statusCode"] != null && 
                    Convert.ToInt32(eventData.Properties["statusCode"]) == 429)
                {
                    if (eventData.Properties.ContainsKey("collection"))
                    {
                        collections.Add(eventData.Properties["collection"].ToString());
                    }
                }

            }

            return collections;
        }
    }
}
