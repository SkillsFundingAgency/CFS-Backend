using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public abstract class SpecificationPublishingBase
    {
        protected readonly IPublishSpecificationValidator Validator;
        protected readonly ISpecificationsApiClient Specifications;

        private readonly IPublishingResiliencePolicies _resiliencePolicies;

        protected SpecificationPublishingBase(IPublishSpecificationValidator validator,
            ISpecificationsApiClient specifications,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(validator, nameof(validator));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            Validator = validator;
            Specifications = specifications;
            _resiliencePolicies = resiliencePolicies;
        }

        protected Policy ResiliencePolicy => _resiliencePolicies.SpecificationsRepositoryPolicy;
    }
}