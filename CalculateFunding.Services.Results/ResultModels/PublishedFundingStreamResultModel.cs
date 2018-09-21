using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Results.ResultModels
{
    public class PublishedFundingStreamResultModel
    {
        public PublishedFundingStreamResultModel()
        {
            AllocationLineResults = Enumerable.Empty<PublishedAllocationLineResultModel>();
        }

        public IEnumerable<PublishedAllocationLineResultModel> AllocationLineResults { get; set; }

        public string FundingStreamName { get; set; }

        public string FundingStreamId { get; set; }

        public decimal FundingAmount
        {
            get
            {
                return AllocationLineResults.IsNullOrEmpty() ? 0 : AllocationLineResults.Sum(m => m.FundingAmount.Value);
            }
        }

        public DateTimeOffset? LastUpdated
        {
            get
            {
                return AllocationLineResults.IsNullOrEmpty() ? null : AllocationLineResults.Max(m => m.LastUpdated);
            }
        }

        public int NumberHeld
        {
            get
            {
                return AllocationLineResults.IsNullOrEmpty() ? 0 : AllocationLineResults.Count(m => m.Status == AllocationLineStatus.Held);
            }
        }

        public int NumberApproved
        {
            get
            {
                return AllocationLineResults.IsNullOrEmpty() ? 0 : AllocationLineResults.Count(m => m.Status == AllocationLineStatus.Approved);
            }
        }

        public int NumberPublished
        {
            get
            {
                return AllocationLineResults.IsNullOrEmpty() ? 0 : AllocationLineResults.Count(m => m.Status == AllocationLineStatus.Published);
            }
        }

        public int NumberUpdated
        {
            get
            {
                return AllocationLineResults.IsNullOrEmpty() ? 0 : AllocationLineResults.Count(m => m.Status == AllocationLineStatus.Updated);
            }
        }

        public int TotalAllocationLines
        {
            get
            {
                return AllocationLineResults.IsNullOrEmpty() ? 0 : AllocationLineResults.Count();
            }
        }
    }
    
}
