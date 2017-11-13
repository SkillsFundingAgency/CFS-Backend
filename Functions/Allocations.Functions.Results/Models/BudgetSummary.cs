using Allocations.Models;

namespace Allocations.Functions.Results.Models
{
    public class BudgetSummary : ResultSummary
    {
        public Reference Budget { get; set; }
        public FundingPolicy[] FundingPolicies { get; set; }
    }
}