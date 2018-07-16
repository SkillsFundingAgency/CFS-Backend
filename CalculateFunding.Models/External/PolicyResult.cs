using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    [Serializable]
    public class PolicyResult
    {
        public PolicyResult()
        {
        }

        public PolicyResult(Policy policy, decimal totalAmount, List<CalculationResult> calculationResults)
        {
            Policy = policy;
            TotalAmount = totalAmount;
            Calculations = calculationResults;
        }

        public Policy Policy { get; set; }

        public decimal TotalAmount { get; set; }

        public IEnumerable<CalculationResult> Calculations { get; set; }

        public IEnumerable<PolicyResult> SubPolicyResults { get; set; }
    }
}