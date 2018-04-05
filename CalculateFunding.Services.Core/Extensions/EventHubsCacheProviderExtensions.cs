using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class EventHubsCacheProviderExtensions
    {
        public static Task<bool> HasMessageBeenProcessed(this ICacheProvider cacheProvider, string eventHubName, EventData eventData)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.IsNullOrWhiteSpace(eventHubName, nameof(eventHubName));
            Guard.ArgumentNotNull(eventData, nameof(eventData));

            return cacheProvider.KeyExists<string>($"eh:{eventHubName}:{eventData.SystemProperties.PartitionKey}:{eventData.SystemProperties.SequenceNumber}");
        }

        public static Task MarkMessageAsProcessed(this ICacheProvider cacheProvider, string eventHubName, EventData eventData)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.IsNullOrWhiteSpace(eventHubName, nameof(eventHubName));
            Guard.ArgumentNotNull(eventData, nameof(eventData));
            DateTimeOffset expiry = new DateTimeOffset(eventData.SystemProperties.EnqueuedTimeUtc.AddDays(1));

            return cacheProvider.SetAsync($"eh:{eventHubName}:{eventData.SystemProperties.PartitionKey}:{eventData.SystemProperties.SequenceNumber}", "y", expiry);
        }
    }
}
