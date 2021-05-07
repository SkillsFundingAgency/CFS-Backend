using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Api.Profiling.IntegrationTests.ReProfiling
{
    public class ProfilePatternTemplateParameters
    {
        public string Id => $"{FundingPeriodId}-{FundingStream}-{FundingLineId}";
       
        public string FundingPeriodId { get; set; }
       
        public string FundingStream{ get; set; }
       
        public string FundingLineId{ get; set; }
       
        public string ReProfilingEnabled{ get; set; }
       
        public string IncreasedAmountStrategyKey{ get; set; }
       
        public string DecreasedAmountStrategyKey{ get; set; }
       
        public string SameAmountStrategyKey{ get; set; }
       
        public IEnumerable<ProfilePeriodPattern> ProfilePattern { get; set; }
        
        public string DisplayName { get; set; }
        
        public string FundingStreamPeriodStartDate { get; set; }
        
        public string FundingStreamPeriodEndDate { get; set; }
    }
}