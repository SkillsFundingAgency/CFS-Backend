using System.Collections.Generic;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Profiling.Custom
{
    public class ApplyCustomProfileRequest
    {
        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }
        
        public string ProviderId { get; set; }
        
        public string CustomProfileName { get; set; }

        public string PublishedProviderId => $"publishedprovider-{ProviderId}-{FundingPeriodId}-{FundingStreamId}";
        
        public IEnumerable<FundingLineProfileOverrides> ProfileOverrides {get; set;}
    }
}