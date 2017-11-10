using Allocations.Models;

namespace Allocations.Functions.Engine.Models
{
    public class BudgetSummary : ResultSummary
    {
        public Reference Budget { get; set; }
        public FundingPolicySummary[] FundingPolicies { get; set; }
    }
}