using System;
using System.Collections.Generic;
using System.Net;
using Azure.Messaging.EventHubs;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbThrottledEventsFilter : ICosmosDbThrottledEventsFilter
    {
        private const int EventHubWindow = 15;

        public IEnumerable<string> GetUniqueCosmosDBContainerNamesFromEventData(IEnumerable<EventData> events)
        {
            Guard.ArgumentNotNull(events, nameof(events));

            // This method gets called many times, but most times there isn't a message which matches, so don't allocate here unless it is required
            HashSet<string> collections = null;

            foreach (EventData eventData in events)
            {
                if (eventData.Properties.Count == 0 || eventData.EnqueuedTime < DateTime.UtcNow.AddMinutes(-EventHubWindow))
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