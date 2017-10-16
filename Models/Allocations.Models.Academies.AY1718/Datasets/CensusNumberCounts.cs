using System;
using System.ComponentModel;
using Allocations.Models.Framework;

namespace Academies.AY1718.Datasets
{
    [Dataset("SBS1718", "Census Number Counts")]
    public class CensusNumberCounts : ProviderDataset
    {
        [Description("NOR Primary")]
        public int NORPrimary { get; set; }

    }
}