using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models.Versioning;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ISpecificationService
    {
        Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId);

        Task<IEnumerable<SpecificationSummary>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId);

        Task SelectSpecificationForFunding(string specificationId);

        Task ClearForceOnNextRefresh(string specificationId);

        Task<IEnumerable<ProfileVariationPointer>> GetProfileVariationPointers(string specificationId);

        Task<PublishStatusResponseModel> EditSpecificationStatus(string specificationId, PublishStatus publishStatus);
    }
}