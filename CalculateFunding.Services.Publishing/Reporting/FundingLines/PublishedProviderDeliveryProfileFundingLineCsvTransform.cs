using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedProviderDeliveryProfileFundingLineCsvTransform : FundingLineCsvTransformBase
    {
        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.CurrentProfileValues;
        }

        protected override PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount)
        {
            return documents.ElementAt(resultCount).Current;
        }

        protected override void TransformFundingLine(IDictionary<string, object> row, PublishedProviderVersion publishedProviderVersion)
        {
            FundingLine fundingLine = publishedProviderVersion.FundingLines.FirstOrDefault();
            row["Total Funding"] = fundingLine.Value;
            
            foreach (ProfilePeriod profilePeriod in new YearMonthOrderedProfilePeriods(fundingLine))
            {
                row[$"{profilePeriod.Year} {profilePeriod.TypeValue} {profilePeriod.Occurrence}"] = profilePeriod.ProfiledValue;
            }
        }
    }
}
