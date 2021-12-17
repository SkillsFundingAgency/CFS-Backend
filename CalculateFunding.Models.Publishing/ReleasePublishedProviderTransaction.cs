using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing
{
    public class ReleasePublishedProviderTransaction
    {
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        /// <summary>
        /// Published Provider Approval Status
        /// </summary>
        [JsonProperty("status")]
        public PublishedProviderStatus Status { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }

        [JsonProperty("author")]
        public Reference Author { get; set; }

        [JsonProperty("variationReasons")]
        public string[] VariationReasons { get; set; }

        [JsonProperty("majorversion")]
        public int MajorVersion { get; set; }

        [JsonProperty("minorVersion")]
        public int MinorVersion { get; set; }

        [JsonProperty("channelName")]
        public string ChannelName { get; set; }

        [JsonProperty("channelCode")]
        public string ChannelCode { get; set; }

        /// <summary>
        /// Total funding for this provider in pounds and pence
        /// </summary>
        [JsonProperty("totalFunding")]
        public decimal? TotalFunding { get; set; }
    }
}
