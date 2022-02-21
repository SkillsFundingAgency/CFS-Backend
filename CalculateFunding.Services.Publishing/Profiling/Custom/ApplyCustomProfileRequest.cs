using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Publishing.Profiling.Custom
{
    public class ApplyCustomProfileRequest
    {
        public string SpecificationId { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }

        public string FundingLineCode { get; set; }

        public string ProviderId { get; set; }
        
        public string CustomProfileName { get; set; }

        public string PublishedProviderId => $"publishedprovider-{ProviderId}-{FundingPeriodId}-{FundingStreamId}";

        public IEnumerable<ProfilePeriod> ProfilePeriods { get; set; }

        public decimal? CarryOver { get; set; }

        [JsonIgnore]
        public bool HasCarryOver => CarryOver.HasValue;
    }
}