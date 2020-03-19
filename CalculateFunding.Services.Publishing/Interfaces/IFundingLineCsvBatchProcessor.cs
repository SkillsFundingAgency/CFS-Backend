using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingLineCsvBatchProcessor
    {
        bool IsForJobType(FundingLineCsvGeneratorJobType jobType);
        
        Task<bool> GenerateCsv(FundingLineCsvGeneratorJobType jobType,
            string specificationId,
            string temporaryFilePath,
            IFundingLineCsvTransform fundingLineCsvTransform,
            string fundingLineCode,
            string fundingStreamId);
    }
}