using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IApprovePrerequisiteChecker
    {
        Task<IEnumerable<string>> PerformPrerequisiteChecks(string specificationId);
    }
}
