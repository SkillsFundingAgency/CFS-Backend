using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseApprovedProvidersService
    {
        Task<IEnumerable<string>> ReleaseProvidersInApprovedState(Reference author, string correlationId, SpecificationSummary specification);
    }
}