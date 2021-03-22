using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderVersionFundingLineProfileValuesCsvTransform : PublishedProviderDeliveryProfileFundingLineCsvTransform
    {
        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.HistoryProfileValues;
        }

        protected override PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount, FundingLineCsvGeneratorJobType jobType)
        {
            return documents.ElementAt(resultCount);
        }
    }
}