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
            Policies = new List<PolicyResult>();
        }

        public FundingStreamResultSummary(FundingStream fundingStream, decimal totalAmount,
            List<AllocationResult> allocations, List<PolicyResult> policies)
        {
            TotalAmount = totalAmount;
            Allocations = allocations;
            Policies = policies;
        }

        public AllocationFundingStreamModel FundingStream { get; set; }

        public decimal TotalAmount { get; set; }

        public List<AllocationResult> Allocations { get; set; }

        public List<PolicyResult> Policies { get; set; }
    }
}