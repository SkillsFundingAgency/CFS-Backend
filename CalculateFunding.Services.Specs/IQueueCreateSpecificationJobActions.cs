using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.Specs
{
    public interface IQueueCreateSpecificationJobActions
    {
        Task Run(SpecificationVersion specificationVersion, Reference user, string correlationId);
    }
}