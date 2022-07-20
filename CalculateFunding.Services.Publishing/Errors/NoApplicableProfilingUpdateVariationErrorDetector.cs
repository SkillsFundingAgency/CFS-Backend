using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
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

        public override bool IsForAllFundingConfigurations => false;

        public override int RunningOrder => 1;

        public override string Name => nameof(NoApplicableProfilingUpdateVariationErrorDetector);

        private const string DISTRIBUTION_PROFILE_STRATEGY = "DistributionProfile";

        private const string CLOSURE_WITH_SUCCESSOR_STRATEGY = "ClosureWithSuccessor";

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
                !providerVariationContext.ApplicableVariations.AnyWithNullCheck(_ => _ == DISTRIBUTION_PROFILE_STRATEGY))
            {
                // closure with successor affects the successor funding lines so we need to add the affected
                // funding lines to the successor from the predecessor context
                IEnumerable<string> predecessors = providerVariationContext.PublishedProvider?.Current?.Predecessors;

                if (predecessors.AnyWithNullCheck())
                {
                    foreach (string predecessor in predecessors)
                    {
                        if (publishedProvidersContext.VariationContexts.ContainsKey(predecessor))
                        {
                            ProviderVariationContext predecessorContext = publishedProvidersContext.VariationContexts[predecessor];

                            if (predecessorContext != null)
                            {
                                IEnumerable<string> affectedClosureWithSuccessorLines = predecessorContext.AffectedFundingLineCodes(CLOSURE_WITH_SUCCESSOR_STRATEGY);
                                if (affectedClosureWithSuccessorLines.AnyWithNullCheck())
                                {
                                    foreach (string affectedFundingLineCode in affectedClosureWithSuccessorLines)
                                    {
                                        providerVariationContext.AddAffectedFundingLineCode(CLOSURE_WITH_SUCCESSOR_STRATEGY, affectedFundingLineCode);
                                    }
                                }
                            }
                        }
                    }
                }

                publishedProvider.Current.FundingLines = publishedProvider.Current.FundingLines?.Select(_ =>
                {
                    // persist changes if the current funding line has been changed through variation strategy
                    // or the funding line is custom profiled
                    // or there is no variation pointer set for the current funding line
                    // or the current value is null
                    // or the current value is zero and the released value is zero
                    // or the refresh value equals the released value but not the prerefresh value
                    //   - (the value has changed from released value but then changed back in subsequent refresh)
                    if ((providerVariationContext.AllAffectedFundingLineCodes != null &&
                        providerVariationContext.AllAffectedFundingLineCodes.Contains(_.FundingLineCode)) ||
                        providerVariationContext.CurrentState.FundingLineHasCustomProfile(_.FundingLineCode) ||
                        !providerVariationContext.VariationPointers.AnyWithNullCheck(vp => vp.FundingLineId == _.FundingLineCode) ||
                        !_.Value.HasValue ||
                        (providerVariationContext.ReleasedState.FundingLines?.FirstOrDefault(rf => rf.FundingLineCode == _.FundingLineCode)?.Value == 0 && _.Value == 0) ||
                        ((providerVariationContext.ReleasedState.FundingLines?.FirstOrDefault(rf => rf.FundingLineCode == _.FundingLineCode)?.Value == _.Value)
                                && (providerVariationContext.PreRefreshState.FundingLines?.FirstOrDefault(cf => cf.FundingLineCode == _.FundingLineCode)?.Value != _.Value)))
                    {
                        return _;
                    }
                    else
                    {
                        string fundingLineCode = _.FundingLineCode;

                        FundingLine currentFundingLine = providerVariationContext
                                                    .PreRefreshState
                                                    .FundingLines?
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
