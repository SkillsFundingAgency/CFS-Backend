using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class FundingLineValueProfileMismatchErrorDetector : PublishedProviderErrorDetector
    {
        protected override Task<ErrorCheck> HasErrors(PublishedProviderVersion publishedProviderVersion)
        {
            ErrorCheck errorCheck = new ErrorCheck();

            foreach (FundingLine fundingLine in PaymentFundingLinesFor(publishedProviderVersion))
            {
                decimal fundingLineValue = fundingLine.Value.GetValueOrDefault();
                decimal profiledValue = GetProfiledSum(fundingLine);

                if (fundingLineValue != profiledValue)
                {
                    errorCheck.AddError(new PublishedProviderError
                    {
                        FundingLineCode = fundingLine.FundingLineCode,
                        Type = PublishedProviderErrorType.FundingLineValueProfileMismatch,
                        Description = $"Expected total funding line to be {fundingLineValue} but custom profiles total {profiledValue}"
                    });
                }
            }

            return Task.FromResult(errorCheck);
        }

        private static IEnumerable<FundingLine> PaymentFundingLinesFor(PublishedProviderVersion publishedProviderVersion)
        {
            if (publishedProviderVersion.ProfilePatternKeys == null ||
                !publishedProviderVersion.ProfilePatternKeys.Any())
            {
                return Enumerable.Empty<FundingLine>();
            }

            HashSet<string> fundingLineCodes = publishedProviderVersion
                .ProfilePatternKeys
                .Select(_ => _.FundingLineCode)
                .ToHashSet();

            return publishedProviderVersion.FundingLines.Where(_ => _.Type == OrganisationGroupingReason.Payment
                                                                    && fundingLineCodes.Contains(_.FundingLineCode)); 
        }

        private static decimal GetProfiledSum(FundingLine fundingLine)
        {
            return new YearMonthOrderedProfilePeriods(fundingLine).Sum(_ => _.ProfiledValue);
        }
    }
}