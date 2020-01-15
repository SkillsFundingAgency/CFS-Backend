namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IVariationStrategyServiceLocator
    {
        IVariationStrategy GetService(string variationStrategyName);
    }
}
