using Allocations.Models;

namespace Allocations.Functions.Engine
{
    public class TestSummary
    {
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Ignored { get; set; }
    }
    public abstract class ResultSummary
    {

        public decimal TotalAmount { get; set; }
        public TestSummary TestSummary { get; set; }    
    }

    public class BudgetSummary : ResultSummary
    {
        public Reference Budget { get; set; }
        public FundingPolicySummary[] FundingPolicies { get; set; }
    }

    public class FundingPolicySummary : ResultSummary
    {
        public Reference FundingPolicy { get; set; }
    }
}
