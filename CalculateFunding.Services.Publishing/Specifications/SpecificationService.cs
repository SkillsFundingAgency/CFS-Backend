using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using ApiProfileVariationPointer = CalculateFunding.Common.ApiClient.Specifications.Models.ProfileVariationPointer;
using CalculateFunding.Common.Models.Versioning;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class SpecificationService : ISpecificationService
    {
        private readonly ISpecificationsApiClient _specifications;
        private readonly AsyncPolicy _resiliencePolicy;

        public SpecificationService(ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsRepositoryPolicy,
                nameof(resiliencePolicies.SpecificationsRepositoryPolicy));

            _specifications = specifications;
            _resiliencePolicy = resiliencePolicies.SpecificationsRepositoryPolicy;
        }

        public async Task<ApiSpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<ApiSpecificationSummary> specificationSummaryResponse =
                await _resiliencePolicy.ExecuteAsync(() => _specifications.GetSpecificationSummaryById(specificationId));

            return specificationSummaryResponse?.Content;
        }

        public async Task<IEnumerable<ApiSpecificationSummary>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ApiResponse<IEnumerable<ApiSpecificationSummary>> specificationSummaryResponse =
                await _resiliencePolicy.ExecuteAsync(() => _specifications.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId));

            if (specificationSummaryResponse.StatusCode.IsSuccess())
            {
                return specificationSummaryResponse.Content;
            }

            return null;
        }

        public async Task SelectSpecificationForFunding(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            HttpStatusCode specificationForFundingResponse =
                await _resiliencePolicy.ExecuteAsync(() => _specifications.SelectSpecificationForFunding(specificationId));

            if (!specificationForFundingResponse.IsSuccess())
            {
                throw new Exception($"Failed to select specification with id '{specificationId}' for funding.");
            }
        }

        public async Task<IEnumerable<ApiProfileVariationPointer>> GetProfileVariationPointers(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<IEnumerable<ApiProfileVariationPointer>> profileVariationPointerResponse =
                await _resiliencePolicy.ExecuteAsync(() => _specifications.GetProfileVariationPointers(specificationId));

            if (profileVariationPointerResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!profileVariationPointerResponse.StatusCode.IsSuccess())
            {
                throw new Exception($"Failed to select get profile variation prointer with specification id '{specificationId}'");
            }

            return profileVariationPointerResponse.Content;
        }

        public async Task<PublishStatusResponseModel> EditSpecificationStatus(
            string specificationId, 
            Common.Models.Versioning.PublishStatus publishStatus)
        {
            ApiResponse<PublishStatusResponseModel> response =
                await _resiliencePolicy.ExecuteAsync(() => 
                _specifications.UpdateSpecificationStatus(
                    specificationId, 
                    new Common.Models.Versioning.PublishStatusRequestModel { PublishStatus = publishStatus }));

            return response?.Content;
        }
    }
}