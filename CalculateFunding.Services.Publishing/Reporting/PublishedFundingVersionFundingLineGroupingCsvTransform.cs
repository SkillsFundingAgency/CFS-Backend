using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class PublishedFundingVersionFundingLineGroupingCsvTransform : FundingLineGroupingCsvTransformBase
    {
        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType) => jobType == FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues;

        protected override PublishedFundingVersion GetPublishedFundingVersion(IEnumerable<dynamic> documents,
            int resultCount,
            FundingLineCsvGeneratorJobType jobType)  
            => documents.ElementAt(resultCount);
    }
}