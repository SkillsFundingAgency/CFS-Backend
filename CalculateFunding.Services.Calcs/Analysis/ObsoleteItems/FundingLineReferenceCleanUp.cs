using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public class FundingLineReferenceCleanUp : ObsoleteItemsReferenceCleanUp
    {
        public FundingLineReferenceCleanUp(ICalculationsRepository calculations,
            ICalcsResiliencePolicies resilience) : base(calculations, resilience, ObsoleteItemType.FundingLine)
        {
        }
    }
}