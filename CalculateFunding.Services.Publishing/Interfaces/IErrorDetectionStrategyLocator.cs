namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IErrorDetectionStrategyLocator
    {
        IDetectPublishedProviderErrors GetDetector(string errorDetectorName);
    }
}
