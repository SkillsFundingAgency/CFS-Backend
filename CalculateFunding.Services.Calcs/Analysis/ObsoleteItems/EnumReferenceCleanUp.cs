using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public class EnumReferenceCleanUp : ObsoleteItemsReferenceCleanUp
    {
        public EnumReferenceCleanUp(ICalculationsRepository calculations,
            ICalcsResiliencePolicies resilience) : base(calculations, resilience, ObsoleteItemType.EnumValue)
        {
        }
    }
}