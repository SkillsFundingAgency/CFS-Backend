using System;
using Allocations.Models.Datasets;
using Allocations.Models.Framework;
using Newtonsoft.Json;

namespace AY1718.CSharp.Datasets
{
    [Dataset("SBS1718", "APT Provider Information")]
    public class AptProviderInformation : ProviderSourceDataset
    {
        [JsonProperty("UPin")]
        public string UPIN { get; set; }
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }
        [JsonProperty("dateOpened")]
        public DateTimeOffset DateOpened { get; set; }
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }

    }
}
