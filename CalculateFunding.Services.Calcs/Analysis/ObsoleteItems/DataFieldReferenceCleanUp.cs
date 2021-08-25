using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;

namespace CalculateFunding.Services.Calcs.Analysis.ObsoleteItems
{
    public class DataFieldReferenceCleanUp : ObsoleteItemsReferenceCleanUp
    {
        public DataFieldReferenceCleanUp(ICalculationsRepository calculations,
            ICalcsResiliencePolicies resilience) : base(calculations, resilience, ObsoleteItemType.DatasetField)
        {
        }
    }
}