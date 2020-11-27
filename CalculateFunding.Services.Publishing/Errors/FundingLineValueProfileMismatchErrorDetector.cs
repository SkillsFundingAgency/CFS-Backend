using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class FundingLineValueProfileMismatchErrorDetector : PublishedProviderErrorDetector
    {
        protected override void ClearErrors(PublishedProviderVersion publishedProviderVersion)
        {
            publishedProviderVersion.Errors?.RemoveAll(_ => _.Type == PublishedProviderErrorType.FundingLineValueProfileMismatch);
        }

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            ErrorCheck errorCheck = new ErrorCheck();

            PublishedProviderVersion publishedProviderVersion = publishedProvider.Current;
            
            foreach (FundingLine fundingLine in CustomPaymentFundingLinesFor(publishedProviderVersion))
            {
                decimal fundingLineValue = fundingLine.Value.GetValueOrDefault();
                decimal profiledValue = GetProfiledSum(fundingLine, publishedProviderVersion);

                if (fundingLineValue != profiledValue)
                {
                    errorCheck.AddError(new PublishedProviderError
                    {
                        Identifier = fundingLine.FundingLineCode,
                        Type = PublishedProviderErrorType.FundingLineValueProfileMismatch,
                        SummaryErrorMessage = "A funding line profile doesn't match allocation value.",
                        DetailedErrorMessage = $"Funding line profile doesn't match allocation value. " +
                        $"The allocation value is £{fundingLineValue}, but the profile value is set to £{profiledValue}",
                        FundingLine = fundingLine.FundingLineCode,
                        FundingStreamId = publishedProviderVersion.FundingStreamId
                    });
                }
            }

            return Task.FromResult(errorCheck);
        }

        private static IEnumerable<FundingLine> CustomPaymentFundingLinesFor(PublishedProviderVersion publishedProviderVersion)
        {
            if (publishedProviderVersion.ProfilePatternKeys.IsNullOrEmpty() &&
                !publishedProviderVersion.HasCustomProfiles)
            {
                return Enumerable.Empty<FundingLine>();
            }

            HashSet<string> fundingLineCodes = publishedProviderVersion
                .ProfilePatternKeys
                .Select(_ => _.FundingLineCode)
                .Union(publishedProviderVersion.CustomProfiles?.Select(_ => _.FundingLineCode) ?? ArraySegment<string>.Empty)
                .ToHashSet();

            return publishedProviderVersion.FundingLines.Where(_ => _.Type == OrganisationGroupingReason.Payment
                                                                    && fundingLineCodes.Contains(_.FundingLineCode)); 
        }

        private static decimal GetProfiledSum(FundingLine fundingLine, PublishedProviderVersion publishedProviderVersion) 
            => new YearMonthOrderedProfilePeriods(fundingLine).Sum(_ => _.ProfiledValue) 
               + publishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLine.FundingLineCode).GetValueOrDefault();
    }
}