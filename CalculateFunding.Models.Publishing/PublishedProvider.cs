﻿using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
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
        /// Logical ID for this published provider to identify it between datastores and consistent between versions
        /// </summary>
        [JsonProperty("publishedProviderId")]
        public string PublishedProviderId => Current.PublishedProviderId;

        /// <summary>
        /// Current version of the provider
        /// </summary>
        [JsonProperty("current")]
        public PublishedProviderVersion Current { get; set; }

        [JsonProperty("released")]
        public PublishedProviderVersion Released { get; set; }

        [JsonIgnore]
        public bool HasResults => Current?.HasResults == true;

        /// <summary>
        /// Cosmos partition to store this document in. The cosmos collection uses /content/partitionKey as partition key
        /// </summary>
        [JsonProperty("partitionKey")]
        public string PartitionKey => GeneratePartitionKey(Current.FundingStreamId, Current.FundingPeriodId, Current.ProviderId);

        public static string GeneratePartitionKey(string fundingStreamId, string fundingPeriodId, string providerId)
        {
            return $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}";
        }

        public bool HasPredecessor(string providerId)
        {
            return Current?.Predecessors?.Count(_ => _?.ToLower()?.Trim() == providerId?.ToLower()?.Trim()) >= 1;
        }

        public void AddPredecessor(string providerId)
        {
            Current.Predecessors ??= new List<string>();
            ICollection<string> currentPredecessors = Current.Predecessors;

            if (currentPredecessors.Contains(providerId))
            {
                return;
            }

            currentPredecessors.Add(providerId);
        }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Current, Released);
        }
    }
}