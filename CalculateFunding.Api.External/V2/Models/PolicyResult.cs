using System;
using System.Collections.ObjectModel;

namespace CalculateFunding.Api.External.V2.Models
{
    [Serializable]
    public class PolicyResult
    {
        public PolicyResult()
        {
            Calculations = new Collection<CalculationResult>();
            SubPolicyResults = new Collection<PolicyResult>();
        }

        public PolicyResult(Policy policy, decimal totalAmount, Collection<CalculationResult> calculationResults)
        {
            Policy = policy;
            TotalAmount = totalAmount;
            Calculations = calculationResults;
        }

        public Policy Policy { get; set; }

        public decimal TotalAmount { get; set; }

        public Collection<CalculationResult> Calculations { get; set; }

        public Collection<PolicyResult> SubPolicyResults { get; set; }
    }
}