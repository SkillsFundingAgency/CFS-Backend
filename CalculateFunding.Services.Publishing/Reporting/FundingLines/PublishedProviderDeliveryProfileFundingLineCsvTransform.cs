using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Publishing.Profiling;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    //TODO; Put this under test 
    //TODO; change this name to something meaningful (probably PublishedProviderFundingLineProfileValuesCsvTransform)
    public class PublishedProviderDeliveryProfileFundingLineCsvTransform : FundingLineCsvTransformBase
    {
        public override bool IsForJobType(FundingLineCsvGeneratorJobType jobType)
        {
            return jobType == FundingLineCsvGeneratorJobType.CurrentProfileValues;
        }

        protected override PublishedProviderVersion GetPublishedProviderVersion(IEnumerable<dynamic> documents, int resultCount, FundingLineCsvGeneratorJobType jobType)
        {
            return documents.ElementAt(resultCount).Current;
        }

        protected override void TransformFundingLine(
            IDictionary<string, object> row,
            PublishedProviderVersion publishedProviderVersion,
            IEnumerable<ProfilePeriodPattern> profilePeriodPatterns = null,
            IEnumerable<string> distinctFundingLineNames = null)
        {
            FundingLine fundingLine = publishedProviderVersion.FundingLines.SingleOrDefault();

            if (fundingLine == null)
            {
                throw new InvalidOperationException("Expected to transform a funding line but none located on published provider version");
            }

            row["Total Funding"] = fundingLine.Value;

            IDictionary<string, ProfilePeriod> profilePeriods = new YearMonthOrderedProfilePeriods(fundingLine).ToDictionary(_ => $"{_.Year} {_.TypeValue} {_.Occurrence}");

            foreach (ProfilePeriodPattern profilePeriodPattern in new YearMonthOrderedProfilePeriodPatterns(profilePeriodPatterns))
            {
                string key = $"{profilePeriodPattern.PeriodYear} {profilePeriodPattern.Period} {profilePeriodPattern.Occurrence}";
                row[key] = profilePeriods.ContainsKey(key) ? 
                    (decimal?)profilePeriods[key].ProfiledValue : 
                    null;
            }

            row["Amount Carried Forward"] = publishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLine.FundingLineCode);
        }


    }
}
