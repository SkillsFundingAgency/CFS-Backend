using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderUpdateDateService : IPublishedProviderUpdateDateService
    {
        private readonly AsyncPolicy _publishedFundingResilience;
        private readonly IPublishedFundingRepository _publishedFundingRepository;

        public PublishedProviderUpdateDateService(IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, "resiliencePolicies.PublishedFundingRepository");
            
            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IActionResult> GetLatestPublishedDate(string fundingStreamId,
            string fundingPeriodId)
        {
            DateTime? latestPublishedDate = await _publishedFundingResilience.ExecuteAsync(() 
                => _publishedFundingRepository.GetLatestPublishedDate(fundingStreamId, fundingPeriodId));
            
            return new OkObjectResult(latestPublishedDate);
        }
    }
}