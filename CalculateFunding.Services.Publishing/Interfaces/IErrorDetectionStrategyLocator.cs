using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IErrorDetectionStrategyLocator
    {
        IDetectPublishedProviderErrors GetErrorDetectorByName(string errorDetectorName);

        IEnumerable<IDetectPublishedProviderErrors> GetErrorDetectorsForAllFundingConfigurations();
    }
}
