using System.ComponentModel;
using Allocations.Models.Datasets;
using Allocations.Models.Framework;
using Newtonsoft.Json;

namespace AY1718.CSharp.Datasets
{
    [Dataset("SBS1718", "APT Basic Entitlement")]
    public class AptBasicEntitlement : ProviderSourceDataset
    {
        [Description("Primary Amount Per Pupil")]
        [JsonProperty("primaryAmountPerPupil")]
        public decimal PrimaryAmountPerPupil { get; set; }

        /// <summary>
        /// This is the primary amount
        /// </summary>
        [Description("Primary Amount")]
        [JsonProperty("primaryAmount")]
        public decimal PrimaryAmount { get; set; }

        [Description("Primary Notional SEN")]
        [JsonProperty("primaryNotionalSEN")]
        public decimal PrimaryNotionalSEN { get; set; }
    }
}