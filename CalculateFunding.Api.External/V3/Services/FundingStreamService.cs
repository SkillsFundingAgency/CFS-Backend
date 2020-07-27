using AutoMapper;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PolicyApiClientModel = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Api.External.V3.Services
{
    public class FundingStreamService : IFundingStreamService
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IMapper _mapper;
        private readonly AsyncPolicy _policiesApiClientPolicy;

        public FundingStreamService(
            IPoliciesApiClient policiesApiClient,
            IExternalApiResiliencePolicies resiliencePolicies,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClientPolicy, nameof(resiliencePolicies.PoliciesApiClientPolicy));

            _policiesApiClient = policiesApiClient;
            _mapper = mapper;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClientPolicy;
        }

        public async Task<IActionResult> GetFundingStreams()
        {
            ApiResponse<IEnumerable<PolicyApiClientModel.FundingStream>> fundingStreamsApiResponse 
                = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreams());

            IEnumerable<Models.FundingStream> fundingStreams = 
                _mapper.Map<IEnumerable<Models.FundingStream>>(fundingStreamsApiResponse.Content);

            return new OkObjectResult(fundingStreams);
        }

        public async Task<IActionResult> GetFundingPeriods(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            ApiResponse<IEnumerable<PolicyApiClientModel.FundingConfig.FundingConfiguration>> fundingConfigurationApiResponse
                = await _policiesApiClientPolicy.ExecuteAsync(() => 
                    _policiesApiClient.GetFundingConfigurationsByFundingStreamId(fundingStreamId));

            IEnumerable<PolicyApiClientModel.FundingConfig.FundingConfiguration> apiFundingConfigurations
                = fundingConfigurationApiResponse.Content;

            if(apiFundingConfigurations == null)
            {
                return new NotFoundObjectResult(
                    $"Funding configuration not found for funding stream: {fundingStreamId}");
            }

            ApiResponse<IEnumerable<PolicyApiClientModel.FundingPeriod>> fundingPeriodsApiResponse
                = await _policiesApiClientPolicy.ExecuteAsync(() => 
                    _policiesApiClient.GetFundingPeriods());

            IEnumerable<PolicyApiClientModel.FundingPeriod> apiFundingPeriods = fundingPeriodsApiResponse.Content;

            if(apiFundingPeriods == null)
            {
                return new OkObjectResult(Enumerable.Empty<IEnumerable<Models.FundingPeriod>>());
            }

            IEnumerable<PolicyApiClientModel.FundingPeriod> apiMatchingFundingPeriods = apiFundingPeriods
                .Where(fp => apiFundingConfigurations.Any(fc => fc.FundingStreamId == fundingStreamId && fc.FundingPeriodId == fp.Id));

            IEnumerable<Models.FundingPeriod> fundingPeriods =
                _mapper.Map<IEnumerable<Models.FundingPeriod>>(apiMatchingFundingPeriods);

            fundingPeriods
                .ForEach(fp => fp.DefaultTemplateVersion =
                    apiFundingConfigurations
                        .SingleOrDefault(fc => fc.FundingStreamId == fundingStreamId && fc.FundingPeriodId == fp.Id)
                        .DefaultTemplateVersion);

            return new OkObjectResult(fundingPeriods);
        }

        public async Task<IActionResult> GetFundingTemplateSourceFile(
            string fundingStreamId, string fundingPeriodId, int majorVersion, int minorVersion)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            ApiResponse<string> fundingTemplateSourceFileApiResponse
                = await _policiesApiClientPolicy.ExecuteAsync(() =>
                    _policiesApiClient.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, $"{majorVersion}.{minorVersion}"));
            
            return new ContentResult
            {
                Content = fundingTemplateSourceFileApiResponse.Content,
                ContentType = "application/json",
                StatusCode = (int)fundingTemplateSourceFileApiResponse.StatusCode
            };
        }

        public async Task<IActionResult> GetPublishedFundingTemplates(string fundingStreamId, string fundingPeriodId)
        {
            ApiResponse<IEnumerable<PolicyApiClientModel.PublishedFundingTemplate>> publishFundingTemplatesResponse
                = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingTemplates(fundingStreamId,fundingPeriodId));

            if(!publishFundingTemplatesResponse.StatusCode.IsSuccess() && publishFundingTemplatesResponse.StatusCode != HttpStatusCode.NotFound)
            {
                return new InternalServerErrorResult($"Unable to retrieve published templates. StatusCode: {publishFundingTemplatesResponse.StatusCode}");
            }

            if(publishFundingTemplatesResponse.StatusCode == HttpStatusCode.NotFound || publishFundingTemplatesResponse.Content?.Any() == false)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult (_mapper.Map<IEnumerable<Models.PublishedFundingTemplate>>(publishFundingTemplatesResponse.Content));
        }
    }
}
