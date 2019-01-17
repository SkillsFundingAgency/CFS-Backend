using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "urn:TBC")]
    [XmlRoot(Namespace = "urn:TBC", IsNullable = false)]
    public class LocalAuthorityResultsSummary
    {
        public LocalAuthorityResultsSummary()
        {
            LocalAuthorities = new Collection<LocalAuthorityResultSummary>();
        }

        public string FundingPeriod { get; set; }

        public Collection<LocalAuthorityResultSummary> LocalAuthorities { get; set; }

        public decimal TotalAllocation { get; set; }
    }
}
