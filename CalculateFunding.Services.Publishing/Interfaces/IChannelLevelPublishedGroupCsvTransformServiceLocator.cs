using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IChannelLevelPublishedGroupCsvTransformServiceLocator
    {
        IChannelLevelPublishedGroupCsvTransform GetService(FundingLineCsvGeneratorJobType jobType);
    }
}