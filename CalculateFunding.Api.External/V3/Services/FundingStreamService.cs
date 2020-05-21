using AutoMapper;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Utility;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            ApiResponse<IEnumerable<Common.ApiClient.Policies.Models.FundingStream>> fundingStreamsApiResponse 
                = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreams());

            IEnumerable<Models.FundingStream> fundingStreams = 
                _mapper.Map<IEnumerable<Models.FundingStream>>(fundingStreamsApiResponse.Content);

            return new OkObjectResult(fundingStreams);
        }
    }
}
