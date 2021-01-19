using System.Collections.Generic;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class ProfilingBatch
    {
        public string FundingPeriodId { get; set; }
        
        public string FundingStreamId { get; set; }
        
        public string ProfilePatternKey { get; set; }
        
        public string ProviderType { get; set; }
        
        public string ProviderSubType { get; set; }
        
        public string FundingLineCode { get; set; }
        
        public decimal FundingValue { get; set; }
        
        public IEnumerable<PublishedProviderVersion> PublishedProviders { get; set; }
        
        public IEnumerable<FundingLine> FundingLines { get; set; }

        public string Key => $"{FundingPeriodId}-{FundingStreamId}-{ProfilePatternKey ?? "?"}-{ProviderType ?? "?"}-{ProviderSubType ?? "?"}-{FundingLineCode}-{FundingValue:N4}";
    }
}