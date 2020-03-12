using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineCsvBatchProcessorServiceLocator
    {
        IFundingLineCsvBatchProcessor GetService(FundingLineCsvGeneratorJobType jobType);
    }
}