using System.Collections.Generic;

namespace CalculateFunding.Models.External
{
    public class PolicyResult
    {
        public PolicyResult()
        {
        }

        public PolicyResult(Policy policy, double totalAmount, IEnumerable<CalculationResult> calculationResults)
        {
            Policy = policy;
            TotalAmount = totalAmount;
            Calculations = calculationResults;
        }

        public Policy Policy { get; set; }

        public double TotalAmount { get; set; }

        public IEnumerable<CalculationResult> Calculations { get; set; }
    }
}