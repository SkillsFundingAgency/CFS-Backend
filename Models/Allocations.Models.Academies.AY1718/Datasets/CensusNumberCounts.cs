using System.ComponentModel;
using Allocations.Models.Framework;

namespace AY1718.CSharp.Datasets
{
    [Dataset("SBS1718", "Census Number Counts")]
    public class CensusNumberCounts : ProviderDataset
    {
        [Description("NOR Primary")]
        public int NORPrimary { get; set; }

    }
}