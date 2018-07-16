using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace CalculateFunding.Models.External
{
    [Serializable]
    public class ProviderResultSummary
    {
        public ProviderResultSummary()
        {
        }

        public ProviderResultSummary(Period period, Provider provider, IEnumerable<FundingStreamResultSummary> fundingStreams)
        {
            Period = period;
            Provider = provider;
            FundingStreamResults = fundingStreams;
        }

        public Period Period { get; set; }

        public Provider Provider { get; set; }

        [XmlIgnore]
        public IEnumerable<FundingStreamResultSummary> FundingStreamResults { get; set; }

        public List<FundingStreamResultSummary> FundingStreamResultList { get => FundingStreamResults.ToList(); set => FundingStreamResults = value;}
    }
}