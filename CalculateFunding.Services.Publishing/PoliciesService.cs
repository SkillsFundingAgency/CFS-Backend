using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using System;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Publishing
{
    public class PoliciesService : IPoliciesService
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Policy _policiesApiClientPolicy;

        public PoliciesService(
            IPoliciesApiClient policiesApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PoliciesApiClient, nameof(publishingResiliencePolicies.PoliciesApiClient));

            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = publishingResiliencePolicies.PoliciesApiClient;
        }

        public async Task<FundingConfiguration> GetFundingConfiguration(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ApiResponse<FundingConfiguration> fundingConfigurationRequest = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(fundingStreamId, fundingPeriodId));

            if (fundingConfigurationRequest == null || fundingConfigurationRequest.StatusCode != System.Net.HttpStatusCode.OK || fundingConfigurationRequest.Content == null)
            {
                throw new InvalidOperationException("Unable to lookup funding configuration");
            }

            return fundingConfigurationRequest.Content;
        }

        public async Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ApiResponse<FundingPeriod> fundingPeriod =
                await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(fundingPeriodId));

            if (fundingPeriod?.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Unable to lookup funding period from policy service");
            }

            if (fundingPeriod?.Content == null)
            {
                throw new Exception("Unable to lookup funding period from policy service - content null");
            }

            return fundingPeriod.Content;
        }

        public async Task<TemplateMetadataContents> GetTemplateMetadataContents(string fundingStreamId, string templateId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(templateId, nameof(templateId));

            ApiResponse<TemplateMetadataContents> templateMetadataContentsResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, templateId));
            
            return templateMetadataContentsResponse?.Content;
        }
    }
}
