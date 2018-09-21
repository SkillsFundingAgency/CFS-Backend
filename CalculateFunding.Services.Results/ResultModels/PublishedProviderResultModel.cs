using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Results.ResultModels
{
    public class PublishedProviderResultModel
    {
        public PublishedProviderResultModel()
        {
            FundingStreamResults = Enumerable.Empty<PublishedFundingStreamResultModel>();
        }

        public IEnumerable<PublishedFundingStreamResultModel> FundingStreamResults { get; set; }

        public string SpecificationId { get; set; }

        public string ProviderName { get; set; }

        public string ProviderId { get; set; }

        public string Ukprn { get; set; }

        public decimal FundingAmount
        {
            get
            {
                return FundingStreamResults.IsNullOrEmpty() ? 0 : FundingStreamResults.Sum(m => m.FundingAmount);
            }
        }

        public int TotalAllocationLines
        {
            get
            {
                return FundingStreamResults.IsNullOrEmpty() ? 0 : FundingStreamResults.Sum(m => m.TotalAllocationLines);
            }
        }

        public int NumberHeld
        {
            get
            {
                return FundingStreamResults.IsNullOrEmpty() ? 0 : FundingStreamResults.Sum(m => m.NumberHeld);
            }
        }

        public int NumberApproved
        {
            get
            {
                return FundingStreamResults.IsNullOrEmpty() ? 0 : FundingStreamResults.Sum(m => m.NumberApproved);
            }
        }

        public int NumberPublished
        {
            get
            {
                return FundingStreamResults.IsNullOrEmpty() ? 0 : FundingStreamResults.Sum(m => m.NumberPublished);
            }
        }

        public int NumberUpdated
        {
            get
            {
                return FundingStreamResults.IsNullOrEmpty() ? 0 : FundingStreamResults.Sum(m => m.NumberUpdated);
            }
        }

        public DateTimeOffset? LastUpdated
        {
            get
            {
                return FundingStreamResults.IsNullOrEmpty() ? null : FundingStreamResults.Max(m => m.LastUpdated);
            }
        }
    }
    
}
