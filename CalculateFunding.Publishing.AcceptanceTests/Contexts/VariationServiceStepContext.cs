using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class VariationServiceStepContext : IVariationServiceStepContext
    {
        private readonly ISpecificationsApiClient _specificationsApiClient;

        public VariationServiceStepContext(IDetectProviderVariations variationsDetection,
            IApplyProviderVariations variationsApplication,
            ISpecificationsApiClient specificationsApiClient)
        {
            _specificationsApiClient = specificationsApiClient;
            Guard.ArgumentNotNull(variationsApplication, nameof(variationsApplication));
            Guard.ArgumentNotNull(variationsDetection, nameof(variationsDetection));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));

            VariationsDetection = variationsDetection;
            VariationsApplication = variationsApplication;
        }

        public IDetectProviderVariations VariationsDetection { get; set; }

        public IApplyProviderVariations VariationsApplication { get; set; }

        public SpecificationsInMemoryClient SpecificationsInMemoryClient =>
            (SpecificationsInMemoryClient) _specificationsApiClient;
    }
}
