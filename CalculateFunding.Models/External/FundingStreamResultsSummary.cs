using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    public class FundingStreamResultsSummary
    {
        public FundingStreamResultsSummary(FundingStream fundingStream, double totalAmount,
            IEnumerable<AllocationResult> allocations, IEnumerable<PolicyResult> policies)
        {
            FundingStream = fundingStream;
            TotalAmount = totalAmount;
            Allocations = allocations;
            Policies = policies;
        }

        public FundingStream FundingStream { get; set; }

        public double TotalAmount { get; set; }

        public IEnumerable<AllocationResult> Allocations { get; set; }

        public IEnumerable<PolicyResult> Policies { get; set; }
    }
}