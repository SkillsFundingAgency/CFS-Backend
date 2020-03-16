using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderFundingLineCsvTransform : FundingLineCsvTransformBase
    {
        protected override PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount)
        {
            return documents.ElementAt(resultCount).Current;
        }

        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.CurrentState ||
                   jobType == FundingLineCsvGeneratorJobType.Released ||
                   jobType == FundingLineCsvGeneratorJobType.CurrentProfileValues;
        }
    }
}