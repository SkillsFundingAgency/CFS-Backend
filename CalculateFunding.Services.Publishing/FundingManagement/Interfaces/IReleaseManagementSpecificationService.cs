using CalculateFunding.Common.ApiClient.Specifications.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseManagementSpecificationService
    {
        Task EnsureReleaseManagementSpecification(SpecificationSummary specification);
    }
}
