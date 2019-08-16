using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICalculationPrerequisiteCheckerService
    {
        Task<IEnumerable<string>> VerifyCalculationPrerequisites(SpecificationSummary specification);
    }
}
