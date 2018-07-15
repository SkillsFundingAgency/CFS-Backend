using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    [Serializable]
    public class FundingStreamResultSummary
    {
        public FundingStreamResultSummary()
        {
        }

        public FundingStreamResultSummary(FundingStream fundingStream, decimal totalAmount,
            List<AllocationResult> allocations, List<PolicyResult> policies)
        {
            FundingStream = fundingStream;
            TotalAmount = totalAmount;
            Allocations = allocations;
            Policies = policies;
        }

        public FundingStream FundingStream { get; set; }

        public decimal TotalAmount { get; set; }

        public List<AllocationResult> Allocations { get; set; }

        public List<PolicyResult> Policies { get; set; }
    }
}