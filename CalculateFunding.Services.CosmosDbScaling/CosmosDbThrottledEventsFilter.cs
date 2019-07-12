using System;
using System.Collections.Generic;
using System.Net;
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
                if (eventData.Properties["statusCode"] != null
                    && Convert.ToInt32(eventData.Properties["statusCode"]) == (int)HttpStatusCode.TooManyRequests)
                {
                    collections.Add(eventData.Properties["collection"].ToString());
                }
            }

            return collections;
        }
    }
}
