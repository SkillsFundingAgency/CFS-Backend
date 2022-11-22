using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting.ChannelLevelPublishedGroup
{
    public interface IChannelLevelPublishedGroupsCsvBatchProcessor
    {
        bool IsForJobType(FundingLineCsvGeneratorJobType jobType);

        Task<bool> GenerateCsv(FundingLineCsvGeneratorJobType jobType,
            string specificationId,
            string fundingPeriodId,
            string temporaryFilePath,
            IChannelLevelPublishedGroupCsvTransform fundingLineCsvTransform,
            string fundingLineName,
            string fundingStreamId,
            string fundingLineCode);
    }
}