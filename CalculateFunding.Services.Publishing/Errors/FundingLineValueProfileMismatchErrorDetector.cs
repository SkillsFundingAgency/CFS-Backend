using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class FundingLineValueProfileMismatchErrorDetector : PublishedProviderErrorDetector
    {
        public FundingLineValueProfileMismatchErrorDetector() 
            : base(PublishedProviderErrorType.FundingLineValueProfileMismatch)
        {
        }

        public override bool IsPreVariationCheck => true;

        public override bool IsAssignProfilePatternCheck => true;
        
        public override string Name => nameof(FundingLineValueProfileMismatchErrorDetector);

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
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

        private static decimal GetProfiledSum(FundingLine fundingLine, PublishedProviderVersion publishedProviderVersion) 
            => new YearMonthOrderedProfilePeriods(fundingLine).Sum(_ => _.ProfiledValue) 
               + publishedProviderVersion.GetCarryOverTotalForFundingLine(fundingLine.FundingLineCode).GetValueOrDefault();
    }
}