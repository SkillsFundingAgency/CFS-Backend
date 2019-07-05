using System;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{
    abstract public class ProviderVersionMetadata
    {
        public ProviderVersionMetadata()
        {
            Created = DateTimeOffset.Now;
        }

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("versionType")]
        public string ProviderVersionTypeString { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("version")]
        public int Version { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("targetDate")]
        public DateTimeOffset TargetDate { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        [JsonProperty("fundingStream")]
        public string FundingStream { get; set; }

        [JsonIgnore]
        public ProviderVersionType VersionType
        {
            get
            {
                if (Enum.TryParse<ProviderVersionType>(ProviderVersionTypeString, true, out var result))
                {
                    return result;
                }

                return ProviderVersionType.Missing;
            }
            set
            {
                ProviderVersionTypeString = value.ToString();
            }
        }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; private set; }
    }
}
