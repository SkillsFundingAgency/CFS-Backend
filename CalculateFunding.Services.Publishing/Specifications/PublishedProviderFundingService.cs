using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderFundingService : IPublishedProviderFundingService
    {
        private readonly AsyncPolicy _resiliencePolicy;
        private readonly IPublishedFundingDataService _publishedFunding;
        private readonly ISpecificationService _specificationService;
        private readonly ISpecificationIdServiceRequestValidator _validator;

        public PublishedProviderFundingService(IPublishingResiliencePolicies resiliencePolicies,
            IPublishedFundingDataService publishedFunding,
            ISpecificationService specificationService,
            ISpecificationIdServiceRequestValidator validator)
        {
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository,
                nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(validator, nameof(validator));

            _resiliencePolicy = resiliencePolicies.PublishedFundingRepository;
            _publishedFunding = publishedFunding;
            _specificationService = specificationService;
            _validator = validator;
        }

        public async Task<IActionResult> GetLatestPublishedProvidersForSpecificationId(string specificationId)
        {
            ValidationResult validationResults = _validator.Validate(specificationId);

            if (!validationResults.IsValid) return validationResults.AsBadRequest();

            SpecificationSummary specificationSummary = await _specificationService.GetSpecificationSummaryById(specificationId);

            List<PublishedProviderVersion> results = new List<PublishedProviderVersion>();

            foreach (var fundingStream in specificationSummary.FundingStreams)
            {
                IEnumerable<PublishedProvider> publishedProviders = await _resiliencePolicy.ExecuteAsync(() =>
                _publishedFunding.GetCurrentPublishedProviders(fundingStream.Id, specificationSummary.FundingPeriod.Id));

                if (publishedProviders.AnyWithNullCheck())
                {
                    results.AddRange(publishedProviders.Select(_ => _.Current));
                }
            }

            return new OkObjectResult(results);
        }
    }
}