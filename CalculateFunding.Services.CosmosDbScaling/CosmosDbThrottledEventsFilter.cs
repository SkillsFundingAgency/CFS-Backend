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
        public IEnumerable<string> GetUniqueCosmosDBContainerNamesFromEventData(IEnumerable<EventData> events)
        {
            Guard.ArgumentNotNull(events, nameof(events));

            // This method gets called many times, but most times there isn't a message which matches, so don't allocate here unless it is required
            HashSet<string> collections = null;

            foreach (EventData eventData in events)
            {
                if (eventData.Properties.Count == 0)
                {
                    continue;
                }

                if (eventData.Properties.TryGetValue("statusCode", out object statusCode)
                    && Convert.ToInt32(statusCode) == (int)HttpStatusCode.TooManyRequests)
                {
                    if (collections == null)
                    {
                        collections = new HashSet<string>();
                    }

                    collections.Add(eventData.Properties["collection"].ToString());
                }
            }

            return collections;
        }
    }
}