using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class ProviderProfilingRequestData
    {
        public PublishedProviderVersion PublishedProvider { get; set; }
        
        public IEnumerable<FundingLine> FundingLinesToProfile { get; set; }
        
        public IDictionary<string, string> ProfilePatternKeys { get; set; }
        
        public string ProviderType { get; set; }
        
        public string ProviderSubType { get; set; }

        public string GetProfilePatternKey(FundingLine fundingLine)
        {
            if (ProfilePatternKeys.IsNullOrEmpty())
            {
                return null;
            }
            
            return ProfilePatternKeys.TryGetValue(fundingLine.FundingLineCode, out string key) ? key : null;
        }
    }
}