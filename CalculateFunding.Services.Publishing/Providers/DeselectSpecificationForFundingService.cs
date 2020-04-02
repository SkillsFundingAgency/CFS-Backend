using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeselectSpecificationForFundingService : IDeselectSpecificationForFundingService
    {
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specsApiPolicy;
        private readonly ILogger _logger;

        public DeselectSpecificationForFundingService(ISpecificationsApiClient specificationsApiClient, 
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, "resiliencePolicies.SpecificationsApiClient");
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _specsApiPolicy = resiliencePolicies.SpecificationsApiClient;
            _specificationsApiClient = specificationsApiClient;
            _logger = logger;
        }

        public async Task DeselectSpecificationForFunding(string fundingStreamId, string fundingPeriodId)
        {
            _logger.Information($"Deselecting specification for funding for {fundingStreamId} {fundingPeriodId}");

            ApiResponse<IEnumerable<SpecificationSummary>> response = await _specsApiPolicy.ExecuteAsync(() => 
                _specificationsApiClient.GetSpecificationsSelectedForFundingByPeriod(fundingPeriodId));

            SpecificationSummary specificationSummary = response?.Content?
                .FirstOrDefault(_ => _.FundingStreams?.Any(fs => fs.Id == fundingStreamId) == true);

            if (specificationSummary == null)
            {
                throw new ArgumentOutOfRangeException(nameof(fundingStreamId), $"Did not locate a specification selected for funding for {fundingStreamId} {fundingPeriodId}");
            }

            HttpStatusCode statusCode = await _specsApiPolicy.ExecuteAsync(() =>
                _specificationsApiClient.DeselectSpecificationForFunding(specificationSummary.Id));

            if (!statusCode.IsSuccess())
            {
                throw new InvalidOperationException($"Unable to deselect specification for funding for {fundingStreamId} {fundingPeriodId}");
            }
        }    
    }
}