using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class VariationServiceStepContext : IVariationServiceStepContext
    {
        public IDetectProviderVariations Service { get; set; }
    }
}
