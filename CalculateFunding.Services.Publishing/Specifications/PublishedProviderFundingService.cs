using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderFundingService : IPublishedProviderFundingService
    {
        private readonly Policy _resiliencePolicy;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly ISpecificationIdServiceRequestValidator _validator;

        public PublishedProviderFundingService(IPublishingResiliencePolicies resiliencePolicies,
            IPublishedFundingRepository publishedFunding,
            ISpecificationIdServiceRequestValidator validator)
        {
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepositoryPolicy,
                nameof(resiliencePolicies.PublishedFundingRepositoryPolicy));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(validator, nameof(validator));

            _resiliencePolicy = resiliencePolicies.PublishedFundingRepositoryPolicy;
            _publishedFunding = publishedFunding;
            _validator = validator;
        }

        public async Task<IActionResult> GetLatestPublishedProvidersForSpecificationId(string specificationId)
        {
            ValidationResult validationResults = _validator.Validate(specificationId);

            if (!validationResults.IsValid) return validationResults.AsBadRequest();

            var publishedProviders = await _resiliencePolicy.ExecuteAsync(() =>
                _publishedFunding.GetLatestPublishedProvidersBySpecification(specificationId));

            return new OkObjectResult(publishedProviders.Select(_ => _.Current).ToArray());
        }
    }
}