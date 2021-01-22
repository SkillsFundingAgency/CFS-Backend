using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    //TODO; Put this under test 
    //TODO; change this name to something meaningful (probably PublishedProviderFundingLineProfileValuesCsvTransform)
    public class PublishedProviderDeliveryProfileFundingLineCsvTransform : FundingLineCsvTransformBase
    {
        private string _fundingLineCode;

        public override string FundingLineCode { set => _fundingLineCode = value; }

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
            FundingLine fundingLine = publishedProviderVersion.FundingLines.SingleOrDefault(_ => _.FundingLineCode == _fundingLineCode);

            if (fundingLine == null)
            {
                throw new InvalidOperationException("Expected to transform a funding line but none located on published provider version");
            }

            row["Total Funding"] = fundingLine.Value;
            
            foreach (ProfilePeriod profilePeriod in new YearMonthOrderedProfilePeriods(fundingLine))
            {
                row[$"{profilePeriod.Year} {profilePeriod.TypeValue} {profilePeriod.Occurrence}"] = profilePeriod.ProfiledValue;
            }
        }
    }
}
