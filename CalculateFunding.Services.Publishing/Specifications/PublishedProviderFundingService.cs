using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
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
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository,
                nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(validator, nameof(validator));

            _resiliencePolicy = resiliencePolicies.PublishedFundingRepository;
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

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(PublishedProviderFundingService)
            };

            health.Dependencies.AddRange((await _publishedFunding.IsHealthOk()).Dependencies);

            return health;
        }
    }
}