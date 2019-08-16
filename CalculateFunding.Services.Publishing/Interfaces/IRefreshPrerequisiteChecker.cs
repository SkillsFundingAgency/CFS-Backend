using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IRefreshPrerequisiteChecker
    {
        /// <summary>
        /// Performs prerequisite checks to make sure a specification can be chosen or refreshed for funding
        /// </summary>
        /// <param name="specification">Specification</param>
        /// <returns>Null or empty enumerable when all checks pass. One or more string messages with validation errors</returns>
        Task<IEnumerable<string>> PerformPrerequisiteChecks(SpecificationSummary specification);
    }
}
