using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs
{
    public interface IQueueEditSpecificationJobActions
    {
        Task Run(
            SpecificationVersion specificationVersion,
            SpecificationVersion previousSpecificationVersion,
            SpecificationEditModel editModel,
            Reference user, 
            string correlationId, 
            bool triggerProviderSnapshotDataLoadJob,
            bool triggerCalculationEngineRunJob);
    }
}
