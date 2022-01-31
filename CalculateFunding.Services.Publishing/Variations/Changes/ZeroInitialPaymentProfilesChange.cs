using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ZeroInitialPaymentProfilesChange : VariationChange
    {
        protected override string ChangeName => "Zero initial payment profiles";

        private readonly string _strategyName;

        public ZeroInitialPaymentProfilesChange(ProviderVariationContext variationContext, string strategyName)
            : base(variationContext, strategyName)
        {
            Guard.IsNullOrWhiteSpace(strategyName, nameof(strategyName));

            _strategyName = strategyName;
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            IEnumerable<ProfileVariationPointer> variationPointers = VariationContext.VariationPointers;

            if (variationPointers.IsNullOrEmpty())
            {
                foreach (FundingLine fl in RefreshState.FundingLines)
                {
                    AddAffectedFundingLine(fl.FundingLineCode);

                    if (fl.Value.HasValue)
                    {
                        fl.Value = 0;
                    }

                    if (fl.Type == FundingLineType.Payment)
                    {
                        if (fl.DistributionPeriods != null)
                        {
                            foreach (DistributionPeriod distributionPeriod in fl.DistributionPeriods)
                            {
                                if (distributionPeriod != null)
                                {
                                    foreach (ProfilePeriod profile in distributionPeriod.ProfilePeriods)
                                    {
                                        if (profile != null)
                                        {
                                            profile.ProfiledValue = 0;
                                        }
                                    }
                                }
                            }

                        }
                    }

                }
            }
            
            return Task.CompletedTask;
        }
    }
}