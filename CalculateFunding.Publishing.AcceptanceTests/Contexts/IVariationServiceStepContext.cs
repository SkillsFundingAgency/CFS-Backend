using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IVariationServiceStepContext
    {
        IDetectProviderVariations Service { get; set; }
    }
}
