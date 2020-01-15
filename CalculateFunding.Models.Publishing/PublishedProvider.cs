﻿using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// The funding for a single provider for a funding period for a funding stream
    /// </summary>
    public class PublishedProvider : IIdentifiable
    {
        /// <summary>
        /// Comos document id
        /// </summary>
        [JsonProperty("id")]
        public string Id =>
            $"publishedprovider-{Current.ProviderId}-{Current.FundingPeriodId}-{Current.FundingStreamId}";

        /// <summary>
        /// Current version of the provider
        /// </summary>
        [JsonProperty("current")]
        public PublishedProviderVersion Current { get; set; }

        [JsonProperty("released")]
        public PublishedProviderVersion Released { get; set; }

        /// <summary>
        /// Cosmos partition to store this document in. The cosmos collection uses /content/partitionKey as partition key
        /// </summary>
        [JsonProperty("partitionKey")]
        public string ParitionKey => GeneratePartitionKey(Current.FundingStreamId, Current.FundingPeriodId, Current.ProviderId);

        public static string GeneratePartitionKey(string fundingStreamId, string fundingPeriodId, string providerId)
        {
            return $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";
        }
    }
}