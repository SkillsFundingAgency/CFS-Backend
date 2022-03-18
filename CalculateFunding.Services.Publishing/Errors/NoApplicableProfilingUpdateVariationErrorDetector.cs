using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Errors
{
    public class NoApplicableProfilingUpdateVariationErrorDetector : PublishedProviderErrorDetector
    {
        public NoApplicableProfilingUpdateVariationErrorDetector()
            : base(PublishedProviderErrorType.NoApplicableProfilingUpdateVariation)
        {
        }

        public override bool IsPreVariationCheck => false;

        public override bool IsAssignProfilePatternCheck => false;

        public override bool IsPostVariationCheck => true;

        public override bool IsForAllFundingConfigurations => true;

        public override int RunningOrder => 1;

        public override string Name => nameof(NoApplicableProfilingUpdateVariationErrorDetector);

        protected override Task<ErrorCheck> HasErrors(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            ErrorCheck errorCheck = new ErrorCheck();

            if (!publishedProvidersContext.VariationContexts.ContainsKey(publishedProvider.Current.ProviderId))
            {
                return Task.FromResult(errorCheck);
            }

            ProviderVariationContext providerVariationContext = publishedProvidersContext.VariationContexts[publishedProvider.Current.ProviderId];

            // only add no applicable variation if the provider doesn't have custom profiles because if it has
            // custom profiles then it will always be updated also we only require a variation strategy if the
            // provider has previously been released
            if (publishedProvider.Released != null &&
                providerVariationContext != null &&
                providerVariationContext.VariationPointers.AnyWithNullCheck() &&
                !providerVariationContext.ApplicableVariations.AnyWithNullCheck(_ => _ == "DistributionProfile"))
            {
                publishedProvider.Current.FundingLines = publishedProvider.Current.FundingLines.Select(_ =>
                {
                    // persist changes if the current funding line has been changed through variation strategy
                    // or the funding line is custom profiled
                    // or there is no variation pointer set for the current funding line
                    // or the current value is null
                    if ((providerVariationContext.AllAffectedFundingLineCodes != null &&
                        providerVariationContext.AllAffectedFundingLineCodes.Contains(_.FundingLineCode)) ||
                        providerVariationContext.CurrentState.FundingLineHasCustomProfile(_.FundingLineCode) ||
                        !providerVariationContext.VariationPointers.AnyWithNullCheck(vp => vp.FundingLineId == _.FundingLineCode) ||
                        !_.Value.HasValue)
                    {
                        return _;
                    }
                    else
                    {
                        string fundingLineCode = _.FundingLineCode;

                        FundingLine currentFundingLine = providerVariationContext
                                                    .PreRefreshState
                                                    .FundingLines
                                                    .First(fl =>
                                                        fl.FundingLineCode == _.FundingLineCode);

                        string errorMessage = $"Post Profiling and Variations - No applicable variation strategy executed for profiling update from £{GetValueOrExcluded(currentFundingLine.Value)} to £{GetValueOrExcluded(_.Value)} against funding line {fundingLineCode}.";

                        // only add the error if the funding line values don't match
                        if (_.Value != currentFundingLine.Value)
                        {
                            errorCheck.AddError(new PublishedProviderError
                            {
                                Type = PublishedProviderErrorType.NoApplicableProfilingUpdateVariation,
                                DetailedErrorMessage = errorMessage,
                                SummaryErrorMessage = errorMessage,
                                FundingStreamId = publishedProvider.Current.FundingStreamId,
                                FundingLineCode = fundingLineCode,
                                Identifier = fundingLineCode
                            });
                        }

                        // if a funding line is not changed through re-profiling then we need to make sure we don't override the existing profiling
                        return new FundingLine
                        {
                            FundingLineCode = _.FundingLineCode,
                            Name = _.Name,
                            TemplateLineId = _.TemplateLineId,
                            DistributionPeriods = currentFundingLine.DistributionPeriods,
                            Type = _.Type,
                            Value = currentFundingLine.Value
                        };
                    }
                }).ToList();
            }

            return Task.FromResult(errorCheck);
        }

        private static string GetValueOrExcluded(decimal? currentFundingLineValue)
        {
            return currentFundingLineValue == null ? "Excluded" : currentFundingLineValue.GetValueOrDefault().ToString();
        }
    }
}
