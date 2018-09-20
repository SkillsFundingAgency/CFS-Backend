using CalculateFunding.Models.Results;
using System;

namespace CalculateFunding.Services.Results.ResultModels
{
    public class PublishedAllocationLineResultModel
    {
        public string AllocationLineId { get; set; }

        public string AllocationLineName { get; set; }

        public decimal? FundingAmount { get; set; }

        public AllocationLineStatus Status { get; set; }

        public DateTimeOffset? LastUpdated { get; set; }

        public string Authority { get; set; }
    }
    
}
