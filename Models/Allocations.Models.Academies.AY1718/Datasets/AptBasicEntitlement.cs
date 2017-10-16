using System.ComponentModel;
using Allocations.Models.Framework;

namespace Academies.AY1718.Datasets
{
    [Dataset("SBS1718", "APT Basic Entitlement")]
    public class AptBasicEntitlement : ProviderDataset
    {
        [Description("Primary Amount Per Pupil")]
        public decimal PrimaryAmountPerPupil { get; set; }
        [Description("Primary Amount")]
        public decimal PrimaryAmount { get; set; }

        [Description("Primary Notional SEN")]
        public decimal PrimaryNotionalSEN { get; set; }
    }
}