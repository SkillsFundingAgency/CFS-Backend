using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    /// <summary>
    /// A version of a template. Used to track changes while a template is built.
    /// </summary>
    public class Template : IIdentifiable
    {
        /// <summary>
        /// Cosmos document id
        /// </summary>
        [JsonProperty("id")]
        public string Id => Guid.NewGuid().ToString();

        /// <summary>
        /// Current version of the provider
        /// </summary>
        [JsonProperty("current")]
        public TemplateVersion Current { get; set; }

        [JsonProperty("released")]
        public TemplateVersion Released { get; set; }

        /// <summary>
        /// Cosmos partition to store this document in. The cosmos collection uses /content/partitionKey as partition key
        /// </summary>
        [JsonProperty("partitionKey")]
        public string PartitionKey => GeneratePartitionKey(Current.FundingStreamId, Current.SchemaVersion);

        public static string GeneratePartitionKey(string schemaVersion, string fundingStreamId)
        {
            return $"template-{fundingStreamId}-{schemaVersion}";
        }

        public bool HasPredecessor(string templateBuildId)
        {
            return Current?.Predecessors?.Count(_ => _?.ToLower().Trim() == templateBuildId?.ToLower().Trim()) >= 1;
        }

        public void AddPredecessor(string templateBuildId)
        {
            Current.Predecessors ??= new List<string>();
            Current.Predecessors.Add(templateBuildId);
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