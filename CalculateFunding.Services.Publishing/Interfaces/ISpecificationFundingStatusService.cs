using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ISpecificationFundingStatusService
    {
        Task<SpecificationFundingStatus> CheckChooseForFundingStatus(string specificationId);

        Task<SpecificationFundingStatus> CheckChooseForFundingStatus(SpecificationSummary specificationSummary);
    }
}
