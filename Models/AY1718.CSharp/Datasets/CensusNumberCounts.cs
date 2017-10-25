using System.ComponentModel;
using Allocations.Models.Datasets;
using Allocations.Models.Framework;
using Newtonsoft.Json;

namespace AY1718.CSharp.Datasets
{
    [Dataset("SBS1718", "Census Number Counts")]
    public class CensusNumberCounts : ProviderSourceDataset
    {
        [Description("NOR Primary")]
        [JsonProperty("norPrimary")]
        public int NORPrimary { get; set; }

    }
}