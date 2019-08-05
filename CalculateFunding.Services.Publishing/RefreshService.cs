using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshService : IRefreshService
    {
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly Policy _publishingResiliencePolicy;

        public RefreshService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService, 
            IProviderService providerService)
        {
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _providerService = providerService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
        }

        public async Task<IEnumerable<Provider>> GetProvidersByProviderVersionId(string providerVersionId)
        {
            return await _providerService.GetProvidersByProviderVersionsId(providerVersionId);
        }

        public async Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            return await _specificationService.GetSpecificationSummaryById(specificationId);
        }

        public async Task RefreshResults(Message message)
        {
            //Ignore this for now in the pr, its just place holder stuff for the next stories
            //We will be getting the job if from the message and the spec id
            //We will be adding telemtry
            //Updating cache with percentage comeplete
            //and whatever else
            
            Guard.ArgumentNotNull(message, nameof(message));

            Reference author = message.GetUserDetails();

            var specificationId = "";

            IEnumerable<PublishedProvider> publishedProviders = await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingRepository.GetLatestPublishedProvidersBySpecification(specificationId));

            if (publishedProviders.IsNullOrEmpty())
                throw new RetriableException(
                    $"Null or empty publsihed providers returned for specification id : '{specificationId}' when setting status to updated");

            await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Updated);
        }
    }
}