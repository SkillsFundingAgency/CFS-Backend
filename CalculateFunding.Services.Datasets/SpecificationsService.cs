using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;

namespace CalculateFunding.Services.Datasets
{
    public class SpecificationsService : ISpecificationsService
    {
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IPoliciesApiClient _policiesApiClient;
        
        private readonly ILogger _logger;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly AsyncPolicy _policiesApiClientPolicy;

        public SpecificationsService(
            ISpecificationsApiClient specificationsApiClient,
            IPoliciesApiClient policiesApiClient,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies, nameof(datasetsResiliencePolicies));
            Guard.ArgumentNotNull(datasetsResiliencePolicies.SpecificationsApiClient, nameof(datasetsResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies.PoliciesApiClient, nameof(datasetsResiliencePolicies.PoliciesApiClient));

            Guard.ArgumentNotNull(logger, nameof(logger));

            _specificationsApiClient = specificationsApiClient;
            _policiesApiClient = policiesApiClient;
            _logger = logger;

            _specificationsApiClientPolicy = datasetsResiliencePolicies.SpecificationsApiClient;
            _policiesApiClientPolicy = datasetsResiliencePolicies.PoliciesApiClient;
        }

        public async Task<IActionResult> GetEligibleSpecificationsToReference(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            ApiResponse<SpecificationSummary> specificationSummaryApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationSummaryApiResponse.StatusCode.IsSuccess() && specificationSummaryApiResponse.StatusCode != HttpStatusCode.NotFound)
            {
                string errorMessage = $"Failed to fetch specification summary for specification ID: {specificationId} with StatusCode={specificationSummaryApiResponse.StatusCode}";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            List<string> allowedPublishedFundingStreamsIdsToReference = new List<string>();

            SpecificationSummary specificationSummary = specificationSummaryApiResponse.Content;

            foreach (Reference fundingStream in specificationSummary.FundingStreams)
            {
                ApiResponse<FundingConfiguration> fundingConfigurationApiResponse =
                    await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(fundingStream.Id, specificationSummary.FundingPeriod.Id));

                if (!fundingConfigurationApiResponse.StatusCode.IsSuccess() && fundingConfigurationApiResponse.StatusCode != HttpStatusCode.NotFound)
                {
                    string errorMessage = $"Failed to fetch funding config for FundingStreamID={fundingStream.Id} and FundingPeriodID={specificationSummary.FundingPeriod.Id} with StatusCode={fundingConfigurationApiResponse.StatusCode}";

                    _logger.Error(errorMessage);

                    throw new RetriableException(errorMessage);
                }

                FundingConfiguration fundingConfiguration = fundingConfigurationApiResponse.Content;

                allowedPublishedFundingStreamsIdsToReference.AddRange(fundingConfiguration.AllowedPublishedFundingStreamsIdsToReference);
            }

            ApiResponse<IEnumerable<SpecificationSummary>> specificationSelectedForFundingSummaryApiResponse =
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationsSelectedForFunding());

            if (!specificationSelectedForFundingSummaryApiResponse.StatusCode.IsSuccess() && specificationSelectedForFundingSummaryApiResponse.StatusCode != HttpStatusCode.NotFound)
            {
                string errorMessage = $"Failed to fetch GetSpecificationsSelectedForFunding with StatusCode={specificationSelectedForFundingSummaryApiResponse.StatusCode}";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            IEnumerable<SpecificationSummary> specificationSelectedForFundingSummaries = specificationSelectedForFundingSummaryApiResponse.Content;

            IEnumerable<EligibleSpecificationReference> eligibleSpecificationReferences =
                (from SpecificationSummary specificationSelectedForFundingSummary in specificationSelectedForFundingSummaries
                from Reference fundingStream in specificationSelectedForFundingSummary.FundingStreams
                where allowedPublishedFundingStreamsIdsToReference.Contains(fundingStream.Id)
                select new EligibleSpecificationReference
                {
                    FundingPeriodId = specificationSelectedForFundingSummary.FundingPeriod.Id,
                    FundingPeriodName = specificationSelectedForFundingSummary.FundingPeriod.Name,
                    FundingStreamId = fundingStream.Id,
                    FundingStreamName = fundingStream.Name,
                    SpecificationId = specificationSelectedForFundingSummary.Id,
                    SpecificationName = specificationSelectedForFundingSummary.Name
                }).ToList();
            return new OkObjectResult(eligibleSpecificationReferences);
        }
    }
}
