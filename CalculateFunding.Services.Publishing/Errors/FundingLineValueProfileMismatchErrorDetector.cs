using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class FundingLineValueProfileMismatchErrorDetector : PublishedProviderErrorDetector
    {
        private IFundingLineRoundingSettings _fundingLineRoundingSettings;

        public FundingLineValueProfileMismatchErrorDetector(IFundingLineRoundingSettings fundingLineRoundingSettings) 
            : base(PublishedProviderErrorType.FundingLineValueProfileMismatch)
        {
            Guard.ArgumentNotNull(fundingLineRoundingSettings, nameof(fundingLineRoundingSettings));

            _fundingLineRoundingSettings = fundingLineRoundingSettings;
        }

        public override bool IsPreVariationCheck => true;

        public override bool IsAssignProfilePatternCheck => true;
        
        public override string Name => nameof(FundingLineValueProfileMismatchErrorDetector);

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            // this error checker checks all published providers in scope regardless of
            // whether it has been updated during refresh this is to make sure that any custom
            // profiling which has happened before a fundingline value change are flagged as errors
            ErrorCheck errorCheck = new ErrorCheck();

            PublishedProviderVersion publishedProviderVersion = publishedProvider.Current;
            
            foreach (FundingLine fundingLine in publishedProvider.Current.CustomPaymentFundingLines)
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
                        DetailedErrorMessage = "Funding line profile doesn't match allocation value. " +
                        $"The allocation value is £{fundingLineValue}, but the profile value is set to £{profiledValue}",
                        FundingLineCode = fundingLine.FundingLineCode,
                        FundingStreamId = publishedProviderVersion.FundingStreamId
                    });
                }
            }

            return Task.FromResult(errorCheck);
        }

        private decimal GetProfiledSum(FundingLine fundingLine, PublishedProviderVersion publishedProviderVersion) 
            => Math.Round(new YearMonthOrderedProfilePeriods(fundingLine).Sum(_ => _.ProfiledValue)
               + publishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLine.FundingLineCode).GetValueOrDefault(), _fundingLineRoundingSettings.DecimalPlaces, MidpointRounding.AwayFromZero);
    }
}