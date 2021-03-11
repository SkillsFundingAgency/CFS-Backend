using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class ProfilingConsistencyCheckErrorDetector : PublishedProviderErrorDetector
    {
        public ProfilingConsistencyCheckErrorDetector() : base(PublishedProviderErrorType.ProfilingConsistencyCheckFailure)
        {
        }

        public override bool IsPreVariationCheck => false;

        public override bool IsAssignProfilePatternCheck => false;

        public override bool IsPostVariationCheck => true;

        public override bool IsForAllFundingConfigurations => true;
        
        public override string Name => nameof(ProfilingConsistencyCheckErrorDetector);

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider,
            PublishedProvidersContext publishedProvidersContext)
        {
            ErrorCheck errorCheck = new ErrorCheck();

            PublishedProviderVersion publishedProviderVersion = publishedProvider.Current;
            
            ProcessPaymentFundingLinesWithoutValues(publishedProviderVersion, errorCheck);
            ProcessPaymentFundingLinesWithValues(publishedProviderVersion, errorCheck);
            
            return Task.FromResult(errorCheck);
        }

        private void ProcessPaymentFundingLinesWithValues(PublishedProviderVersion publishedProviderVersion,
            ErrorCheck errorCheck)
        {
            foreach (FundingLine fundingLine in publishedProviderVersion.PaymentFundingLinesWithValues.Except(publishedProviderVersion.CustomPaymentFundingLines))
            {
                CheckFundingLineProfilingConsistency(publishedProviderVersion, fundingLine, errorCheck);
            }    
        }

        private void CheckFundingLineProfilingConsistency(PublishedProviderVersion publishedProviderVersion,
            FundingLine fundingLine,
            ErrorCheck errorCheck)
        {
            decimal distributionPeriodsTotal = (fundingLine.DistributionPeriods?.Sum(_ => _.Value))
                .GetValueOrDefault();

            decimal totalExpectedFunding = fundingLine.Value.GetValueOrDefault();

            if (distributionPeriodsTotal != totalExpectedFunding)
            {
                string fundingLineCode = fundingLine.FundingLineCode;
                string errorMessage = $"Post Profiling and Variations - The payment funding line {fundingLineCode} has a total expected" + 
                                      $" funding of {totalExpectedFunding} but the distribution periods total for the funding line is {distributionPeriodsTotal}";
                
                errorCheck.AddError(new PublishedProviderError
                {
                    Type = PublishedProviderErrorType.ProfilingConsistencyCheckFailure,
                    DetailedErrorMessage = errorMessage,
                    SummaryErrorMessage = errorMessage,
                    FundingStreamId = publishedProviderVersion.FundingStreamId,
                    FundingLineCode = fundingLineCode,
                    Identifier = fundingLineCode
                });
            }
            
            CheckFundingLineDistributionPeriodTotals(publishedProviderVersion, fundingLine, errorCheck);
        }

        private void CheckFundingLineDistributionPeriodTotals(PublishedProviderVersion publishedProviderVersion,
            FundingLine fundingLine,
            ErrorCheck errorCheck)
        {
            foreach (DistributionPeriod distributionPeriod in fundingLine.DistributionPeriods ?? ArraySegment<DistributionPeriod>.Empty)
            {
                decimal totalExpectedFunding = distributionPeriod.Value;
                decimal profilePeriodsTotal = (distributionPeriod.ProfilePeriods?
                    .Sum(_ => _.ProfiledValue))
                    .GetValueOrDefault();

                if (totalExpectedFunding != profilePeriodsTotal)
                {
                    string fundingLineCode = fundingLine.FundingLineCode;
                    string errorMessage = $"Post Profiling and Variations - The payment funding line {fundingLineCode} distribution period {distributionPeriod.DistributionPeriodId} " + 
                                          $"has a total expected funding of {totalExpectedFunding} but the total profiled for the distribution period is {profilePeriodsTotal}";
                
                    errorCheck.AddError(new PublishedProviderError
                    {
                        Type = PublishedProviderErrorType.ProfilingConsistencyCheckFailure,
                        DetailedErrorMessage = errorMessage,
                        SummaryErrorMessage = errorMessage,
                        FundingStreamId = publishedProviderVersion.FundingStreamId,
                        FundingLineCode = fundingLineCode,
                        Identifier = fundingLineCode
                    });       
                }
            }    
        }

        private void ProcessPaymentFundingLinesWithoutValues(PublishedProviderVersion publishedProviderVersion,
            ErrorCheck errorCheck)
        {
            foreach (FundingLine fundingLine in publishedProviderVersion.PaymentFundingLinesWithoutValues.Except(publishedProviderVersion.CustomPaymentFundingLines))
            {
                int distributionPeriodCount = (fundingLine.DistributionPeriods?.Count()).GetValueOrDefault();
                
                if (distributionPeriodCount == 0)
                {
                    continue;
                }

                string fundingLineCode = fundingLine.FundingLineCode;
                string errorMessage = $"Post Profiling and Variations - The payment funding line {fundingLineCode} has a null total" + 
                                      $" but contains {distributionPeriodCount} distributions periods";
                
                errorCheck.AddError(new PublishedProviderError
                {
                    Type = PublishedProviderErrorType.ProfilingConsistencyCheckFailure,
                    DetailedErrorMessage = errorMessage,
                    SummaryErrorMessage = errorMessage,
                    FundingStreamId = publishedProviderVersion.FundingStreamId,
                    FundingLineCode = fundingLineCode,
                    Identifier = fundingLineCode
                });
            }
        }
    }
}