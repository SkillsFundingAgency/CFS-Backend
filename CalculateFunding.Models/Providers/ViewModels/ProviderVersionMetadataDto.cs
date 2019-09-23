using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers.ViewModels
{
    public class ProviderVersionMetadataDto
    {
        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("targetDate")]
        public DateTimeOffset TargetDate { get; set; }

        [JsonProperty("fundingStream")]
        public string FundingStream { get; set; }

        [JsonProperty("versionType")]
        public ProviderVersionType VersionType { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }
    }
}
