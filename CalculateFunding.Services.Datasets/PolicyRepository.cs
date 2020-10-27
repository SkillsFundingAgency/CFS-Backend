using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Datasets.Interfaces;
using Polly;
using System.Collections.Generic;
using System.Threading.Tasks;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly AsyncPolicy _policiesApiClientPolicy;

        public PolicyRepository(
            IPoliciesApiClient policiesApiClient,
            IDatasetsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));

            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
        }

        public async Task<IEnumerable<PoliciesApiModels.FundingStream>> GetFundingStreams()
        {
            ApiResponse<IEnumerable<PoliciesApiModels.FundingStream>> fundingStreamsApiResponse
                    = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreams());

            return fundingStreamsApiResponse.Content;
        }

        public async Task<PoliciesApiModels.FundingPeriod> GetFundingPeriod(string fundingPeriodId)
        {
            ApiResponse<PoliciesApiModels.FundingPeriod> apiResponse
                    = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(fundingPeriodId));

            return apiResponse.Content;
        }

        public async Task<PoliciesApiModels.FundingStream> GetFundingStream(string fundingStreamId)
        {
            ApiResponse<PoliciesApiModels.FundingStream> apiResponse
                    = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreamById(fundingStreamId));

            return apiResponse.Content;
        }

        public async Task<PoliciesApiModels.TemplateMetadataDistinctCalculationsContents> GetDistinctTemplateMetadataCalculationsContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            ApiResponse<PoliciesApiModels.TemplateMetadataDistinctCalculationsContents> apiResponse
                    = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetDistinctTemplateMetadataCalculationsContents(fundingStreamId, fundingPeriodId, templateVersion));

            return apiResponse.Content;
        }
    }
}
