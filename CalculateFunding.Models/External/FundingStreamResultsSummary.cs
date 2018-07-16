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
            IEnumerable<AllocationResult> allocations, IEnumerable<PolicyResult> policies)
        {
            FundingStream = fundingStream;
            TotalAmount = totalAmount;
            Allocations = allocations;
            Policies = policies;
        }

        public FundingStream FundingStream { get; set; }

        public decimal TotalAmount { get; set; }

        public IEnumerable<AllocationResult> Allocations { get; set; }

        public IEnumerable<PolicyResult> Policies { get; set; }
    }
}