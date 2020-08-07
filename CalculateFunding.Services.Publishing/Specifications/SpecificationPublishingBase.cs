using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public abstract class SpecificationPublishingBase
    {
        protected readonly ISpecificationIdServiceRequestValidator SpecificationIdValidator;
        protected readonly IPublishedProviderIdsServiceRequestValidator PublishedProviderIdsValidator;
        protected readonly ISpecificationsApiClient Specifications;
        protected readonly IFundingConfigurationService _fundingConfigurationService;
        private readonly IPublishingResiliencePolicies _resiliencePolicies;

        protected SpecificationPublishingBase(
            ISpecificationIdServiceRequestValidator specificationIdValidator,
            IPublishedProviderIdsServiceRequestValidator publishedProviderIdsValidator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies,
            IFundingConfigurationService fundingConfigurationService)
        {
            Guard.ArgumentNotNull(specificationIdValidator, nameof(specificationIdValidator));
            Guard.ArgumentNotNull(publishedProviderIdsValidator, nameof(publishedProviderIdsValidator));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsRepositoryPolicy, nameof(resiliencePolicies.SpecificationsRepositoryPolicy));
            Guard.ArgumentNotNull(fundingConfigurationService, nameof(fundingConfigurationService));

            SpecificationIdValidator = specificationIdValidator;
            PublishedProviderIdsValidator = publishedProviderIdsValidator;
            Specifications = specifications;
            _resiliencePolicies = resiliencePolicies;
            _fundingConfigurationService = fundingConfigurationService;
        }

        protected Polly.AsyncPolicy ResiliencePolicy => _resiliencePolicies.SpecificationsRepositoryPolicy;
    }
}