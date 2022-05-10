namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderStateSummaryCsvTransformServiceLocator
    {
        IPublishedProviderStateSummaryCsvTransform GetService(string jobDefinitionName);
    }
}