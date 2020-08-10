using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class PublishedFundingFundingLineGroupingCsvTransform : FundingLineGroupingCsvTransformBase
    {
        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType) => jobType == FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues;

        protected override PublishedFundingVersion GetPublishedFundingVersion(IEnumerable<dynamic> documents,
            int resultCount) 
            => documents.ElementAt(resultCount).Current;
    }
}