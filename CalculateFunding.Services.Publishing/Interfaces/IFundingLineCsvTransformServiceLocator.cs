using CalculateFunding.Services.Publishing.Reporting;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineCsvTransformServiceLocator
    {
        IFundingLineCsvTransform GetService(FundingLineCsvGeneratorJobType jobType);
    }
}