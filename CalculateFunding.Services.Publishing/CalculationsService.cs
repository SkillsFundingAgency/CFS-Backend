using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
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
        private readonly Policy _calcsApiClientPolicy;
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
    }
}
