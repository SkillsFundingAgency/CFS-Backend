using Allocations.Models;
using CalculateFunding.Models;

namespace Allocations.Functions.Engine.Models
{
    public class FundingPolicySummary : ResultSummary
    {
        public Reference FundingPolicy { get; set; }
    }
}