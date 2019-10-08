using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public abstract class SpecificationPublishingBase
    {
        protected readonly ISpecificationIdServiceRequestValidator Validator;
        protected readonly ISpecificationsApiClient Specifications;

        private readonly IPublishingResiliencePolicies _resiliencePolicies;

        protected SpecificationPublishingBase(ISpecificationIdServiceRequestValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(validator, nameof(validator));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsRepositoryPolicy, nameof(resiliencePolicies.SpecificationsRepositoryPolicy));

            Validator = validator;
            Specifications = specifications;
            _resiliencePolicies = resiliencePolicies;
        }

        protected Polly.Policy ResiliencePolicy => _resiliencePolicies.SpecificationsRepositoryPolicy;
    }
}