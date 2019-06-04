using Newtonsoft.Json;
using System;

namespace CalculateFunding.Models.Providers
{
    public class ProviderVersionMetadata
    {
        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("versionType")]
        public string ProviderVersionTypeString;

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonIgnore]

        public ProviderVersionType VersionType {
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
        public DateTimeOffset Created { get; set; }
    }
}
