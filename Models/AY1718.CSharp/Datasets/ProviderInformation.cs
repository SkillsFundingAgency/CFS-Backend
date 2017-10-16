using System;
using Allocations.Models.Framework;

namespace AY1718.CSharp.Datasets
{
    [Dataset("SBS1718", "APT Provider Information")]
    public class AptProviderInformation : ProviderDataset
    {
        public string UPIN { get; set; }
        public string ProviderName { get; set; }
        public DateTimeOffset DateOpened { get; set; }
        public string LocalAuthority { get; set; }

    }
}
