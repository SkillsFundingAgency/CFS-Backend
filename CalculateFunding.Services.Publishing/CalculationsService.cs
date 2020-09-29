using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class CalculationsService : ICalculationsService
    {
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly AsyncPolicy _calcsApiClientPolicy;
        private readonly ILogger _logger;

        public CalculationsService(
            ICalculationsApiClient calculationsApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.CalculationsApiClient, nameof(publishingResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _calculationsApiClient = calculationsApiClient;
            _calcsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _logger = logger;
        }

        public async Task<bool> HaveAllTemplateCalculationsBeenApproved(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<BooleanResponseModel> apiResponse = await _calcsApiClientPolicy.ExecuteAsync(
                () => _calculationsApiClient.CheckHasAllApprovedTemplateCalculationsForSpecificationId(specificationId));

            if (!apiResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Failed to check spoecification with id '{specificationId}' " +
                    $"for all approved template calculations with status code '{apiResponse.StatusCode}'";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            return apiResponse.Content.Value;
        }

        public async Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            ApiResponse<TemplateMapping> apiResponse = await _calcsApiClientPolicy.ExecuteAsync(
                () => _calculationsApiClient.GetTemplateMapping(specificationId, fundingStreamId));

            if (!apiResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Failed to retrieve template mapping for specification id '{specificationId}' and  funding stream id '{fundingStreamId}'" +
                    $" with status code '{apiResponse.StatusCode}'";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            return apiResponse.Content;
        }

        public async Task<IEnumerable<CalculationMetadata>> GetCalculationMetadataForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<IEnumerable<CalculationMetadata>> apiResponse = await _calcsApiClientPolicy.ExecuteAsync(
                () => _calculationsApiClient.GetCalculationMetadataForSpecification(specificationId));

            if (!apiResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Failed to retrieve calculation metadata for specification id '{specificationId}'" +
                    $" with status code '{apiResponse.StatusCode}'";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            return apiResponse.Content;
        }
    }
}
