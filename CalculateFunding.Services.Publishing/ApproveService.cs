using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing
{
    public class ApproveService : IApproveService
    {
        private readonly IJobTracker _jobTracker;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly Policy _publishingResiliencePolicy;
        private readonly ISpecificationService _specificationService;

        public ApproveService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService, 
            IJobTracker jobTracker)
        {
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _jobTracker = jobTracker;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
        }

        public async Task<ApiSpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            return await _specificationService.GetSpecificationSummaryById(specificationId);
        }

        public async Task ApproveResults(Message message)
        {
            //Ignore this for now in the pr, its just place holder stuff for the next stories
            //We will be getting the job if from the message and the spec id
            //We will be adding telemtry
            //Updating cache with percentage comeplete
            //and whatever else

            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = message.GetUserProperty<string>("jobId");
            
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            if (!await _jobTracker.TryStartTrackingJob(jobId, "ApproveResults"))
            {
                return;
            }
            
            Reference author = message.GetUserDetails();

            string specificationId = message.GetUserProperty<string>("specification-id");

            IEnumerable<PublishedProvider> publishedProviders =
                await _publishingResiliencePolicy.ExecuteAsync(() => _publishedFundingRepository.GetPublishedProvidersForApproval(specificationId));

            if (publishedProviders.IsNullOrEmpty())
                throw new RetriableException($"Null or empty published providers returned for specification id : '{specificationId}' when setting status to approved.");

            await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProviders, author, PublishedProviderStatus.Approved, jobId);

            await _jobTracker.CompleteTrackingJob(jobId);
        }
    }
}