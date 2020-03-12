using CalculateFunding.Services.Publishing.Reporting;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineCsvTransformServiceLocator
    {
        IFundingLineCsvTransform GetService(FundingLineCsvGeneratorJobType jobType);
    }
}