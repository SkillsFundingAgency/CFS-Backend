using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class FundingStreamResultSummary
    {
        public FundingStreamResultSummary()
        {
            Allocations = new List<AllocationResult>();
        }

        public FundingStreamResultSummary(FundingStream fundingStream, decimal fundingStreamTotalAmount,
            List<AllocationResult> allocations)
        {
            FundingStreamTotalAmount = fundingStreamTotalAmount;
            Allocations = allocations;
        }

	    public SpecificationInformationModel Specification { get; set; }

        public AllocationFundingStreamModel FundingStream { get; set; }

        public decimal FundingStreamTotalAmount { get; set; }

        public List<AllocationResult> Allocations { get; set; }
    }
}