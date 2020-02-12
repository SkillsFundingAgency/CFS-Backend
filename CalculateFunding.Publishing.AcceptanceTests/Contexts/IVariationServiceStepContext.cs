using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IVariationServiceStepContext
    {
        IDetectProviderVariations VariationsDetection { get; set; }

        IApplyProviderVariations VariationsApplication { get; set; }

        SpecificationsInMemoryClient SpecificationsInMemoryClient { get; }
    }
}

