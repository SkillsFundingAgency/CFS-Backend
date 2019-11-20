using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderStatusService : IPublishedProviderStatusService
    {
        private readonly ISpecificationIdServiceRequestValidator _validator;
        private readonly ISpecificationService _specificationService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly Policy _publishedFundingRepositoryResilience;
        private readonly Policy _specificationsRepositoryPolicy;

        public PublishedProviderStatusService(
            ISpecificationIdServiceRequestValidator validator,
            ISpecificationService specificationService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(validator, nameof(validator));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies.SpecificationsRepositoryPolicy, nameof(publishingResiliencePolicies.SpecificationsRepositoryPolicy));

            _validator = validator;
            _specificationService = specificationService;
            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingRepositoryResilience = publishingResiliencePolicies.PublishedFundingRepository;
            _specificationsRepositoryPolicy = publishingResiliencePolicies.SpecificationsRepositoryPolicy;
        }

        public async Task<IActionResult> GetProviderStatusCounts(string specificationId)
        {
            ValidationResult validationResults = _validator.Validate(specificationId);

            if (!validationResults.IsValid) return validationResults.AsBadRequest();

            SpecificationSummary specificationSummary =
                await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationService.GetSpecificationSummaryById(specificationId));

            IEnumerable<PublishedProviderFundingStreamStatus> publishedProviderFundingStreamStatuses =
                await _publishedFundingRepositoryResilience.ExecuteAsync(() => _publishedFundingRepository.GetPublishedProviderStatusCounts(specificationId));
            
            List<ProviderFundingStreamStatusResponse> response = new List<ProviderFundingStreamStatusResponse>();

            foreach (IGrouping<string, PublishedProviderFundingStreamStatus> publishedProviderFundingStreamGroup in publishedProviderFundingStreamStatuses.GroupBy(x => x.FundingStreamId))
            {
                if (!specificationSummary.FundingStreams.Select(x => x.Id).Contains(publishedProviderFundingStreamGroup.Key))
                {
                    continue;
                }

                response.Add(new ProviderFundingStreamStatusResponse
                {
                    FundingStreamId = publishedProviderFundingStreamGroup.Key,
                    ProviderApprovedCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Approved"),
                    ProviderDraftCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Draft"),
                    ProviderReleasedCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Released"),
                    ProviderUpdatedCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Updated"),
                    TotalFunding = publishedProviderFundingStreamGroup.Sum(x=>x.TotalFunding)
                });
            }

            return new OkObjectResult(response);
        }

        private static int GetCountValueOrDefault(IGrouping<string, PublishedProviderFundingStreamStatus> publishedProviderFundingStreamGroup, string statusName)
        {
            PublishedProviderFundingStreamStatus publishedProviderFundingStreamStatus = publishedProviderFundingStreamGroup.SingleOrDefault(x => x.Status == statusName);

            return publishedProviderFundingStreamStatus == null ? default : publishedProviderFundingStreamStatus.Count;
        }
    }
}
