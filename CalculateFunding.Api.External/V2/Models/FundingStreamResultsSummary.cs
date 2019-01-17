using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class FundingStreamResultSummary
    {
        public FundingStreamResultSummary()
        {
            Allocations = new Collection<AllocationResult>();
        }

        public FundingStreamResultSummary(FundingStream fundingStream, decimal fundingStreamTotalAmount,
            Collection<AllocationResult> allocations)
        {
            FundingStreamTotalAmount = fundingStreamTotalAmount;
            Allocations = allocations;
        }

	    public SpecificationInformationModel Specification { get; set; }

        public AllocationFundingStreamModel FundingStream { get; set; }

        public decimal FundingStreamTotalAmount { get; set; }

        public Collection<AllocationResult> Allocations { get; set; }
    }
}