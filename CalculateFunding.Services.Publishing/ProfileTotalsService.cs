using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class ProfileTotalsService : IProfileTotalsService
    {
        private readonly Policy _resilience;
        private readonly IPublishedFundingRepository _publishedFunding;

        public ProfileTotalsService(IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            
            _resilience = resiliencePolicies.PublishedFundingRepository;
            _publishedFunding = publishedFunding;
        }

        public async Task<IActionResult> GetPaymentProfileTotalsForFundingStreamForProvider(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            PublishedProviderVersion latestPublishedProviderVersion = await _resilience.ExecuteAsync(() =>
                _publishedFunding.GetLatestPublishedProviderVersion(fundingStreamId, fundingPeriodId, providerId));

            if (latestPublishedProviderVersion == null)
            {
                return new NotFoundResult();
            }

            ProfileTotal[] profileTotals = new PaymentFundingLineProfileTotals(latestPublishedProviderVersion)
                .ToArray();

            return new OkObjectResult(profileTotals);
        }
    }
}