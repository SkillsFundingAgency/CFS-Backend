namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderCsvTransformServiceLocator
    {
        IPublishedProviderCsvTransform GetService(string jobDefinitionName);
    }
}