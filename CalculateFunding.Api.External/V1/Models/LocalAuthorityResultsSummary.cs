using System;
using System.Xml.Serialization;

namespace CalculateFunding.Api.External.V1.Models
{
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "urn:TBC")]
    [XmlRoot(Namespace = "urn:TBC", IsNullable = false)]
    public class LocalAuthorityResultsSummary
    {
        public LocalAuthorityResultsSummary()
        {
            LocalAuthorities = new LocalAuthorityResultSummary[0];
        }

        public string FundingPeriod { get; set; }

        public LocalAuthorityResultSummary[] LocalAuthorities { get; set; }

        public decimal TotalAllocation { get; set; }
    }
}
