using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class PoliciesService : IPoliciesService
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly AsyncPolicy _policiesApiClientPolicy;

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

        public async Task<FundingPeriod> GetFundingPeriodByConfigurationId(string fundingPeriodId)
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

        public async Task<TemplateMetadataContents> GetTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(templateId, nameof(templateId));

            ApiResponse<TemplateMetadataContents> templateMetadataContentsResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateId));

            return templateMetadataContentsResponse?.Content;
        }

        public async Task<FundingDate> GetFundingDate(
            string fundingStreamId,
            string fundingPeriodId,
            string fundingLineId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingLineId, nameof(fundingLineId));

            ApiResponse<FundingDate> templateMetadataContentsResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingDate(
                    fundingStreamId,
                    fundingPeriodId,
                    fundingLineId));

            return templateMetadataContentsResponse?.Content;
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            ApiResponse<IEnumerable<FundingStream>> fundingStreamsResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreams());

            return fundingStreamsResponse?.Content;
        }

        public async Task<TemplateMetadataDistinctFundingLinesContents> GetDistinctTemplateMetadataFundingLinesContents(
            string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));

            ApiResponse<TemplateMetadataDistinctFundingLinesContents> distinctTemplateMetadataFundingLinesContentsResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() =>
                _policiesApiClient.GetDistinctTemplateMetadataFundingLinesContents(
                    fundingStreamId, fundingPeriodId, templateVersion));


            return distinctTemplateMetadataFundingLinesContentsResponse?.Content;
        }

        public async Task<IEnumerable<PublishedFundingTemplate>> GetFundingTemplates(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ApiResponse<IEnumerable<PublishedFundingTemplate>> distinctTemplateMetadataFundingLinesContentsResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() =>
                _policiesApiClient.GetFundingTemplates(
                    fundingStreamId, fundingPeriodId));


            return distinctTemplateMetadataFundingLinesContentsResponse?.Content;
        }

        public async Task<IEnumerable<string>> GetDistinctFundingLineNames(string fundingStreamId, string fundingPeriodId)
        {
            List<string> fundingLines = new List<string>();

            IEnumerable<PublishedFundingTemplate> publishedFundingTemplates
                = await GetFundingTemplates(fundingStreamId, fundingPeriodId);

            IEnumerable<string> templateVersionStrings = publishedFundingTemplates.Select(_ => _.TemplateVersion);

            List<Task<TemplateMetadataDistinctFundingLinesContents>> requests = new List<Task<TemplateMetadataDistinctFundingLinesContents>>();
            foreach (string templateVersionString in templateVersionStrings)
            {
                requests.Add(GetDistinctTemplateMetadataFundingLinesContents(fundingStreamId, fundingPeriodId, templateVersionString));
            }

            await TaskHelper.WhenAllAndThrow(requests.ToArray());

            foreach (Task<TemplateMetadataDistinctFundingLinesContents> request in requests)
            {
                TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents = request.Result;

                fundingLines.AddRange(templateMetadataDistinctFundingLinesContents.FundingLines.Select(_ => _.Name));
            }

            return fundingLines.Distinct();
        }

        public async Task<TemplateMetadataFundingLineCashCalculationsContents> GetCashCalcsForFundingLines(string fundingStreamId, string fundingPeriodId, string templateId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(templateId, nameof(templateId));

            ApiResponse<TemplateMetadataFundingLineCashCalculationsContents> distinctTemplateMetadataContents =
                    await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetCashCalcsForFundingLines(fundingStreamId, fundingPeriodId, templateId));

            return distinctTemplateMetadataContents?.Content;
        }

        public async Task<TemplateMetadataDistinctCalculationsContents> GetDistinctTemplateMetadataCalculationsContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));

            ApiResponse<TemplateMetadataDistinctCalculationsContents> apiResponse
                    = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetDistinctTemplateMetadataCalculationsContents(fundingStreamId, fundingPeriodId, templateVersion));

            return apiResponse.Content;
        }
    }
}
