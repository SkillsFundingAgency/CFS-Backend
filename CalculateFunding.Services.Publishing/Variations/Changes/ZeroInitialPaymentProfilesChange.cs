using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ZeroInitialPaymentProfilesChange : VariationChange
    {
        private const string ChangeName = "zero initial payment profiles";

        public ZeroInitialPaymentProfilesChange(ProviderVariationContext variationContext)
            : base(variationContext)
        {
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            try
            {
                IEnumerable<ProfileVariationPointer> variationPointers = VariationContext.VariationPointers;

                if (variationPointers.IsNullOrEmpty())
                {
                    foreach (FundingLine fl in RefreshState.FundingLines)
                    {
                        if (fl.Value.HasValue)
                        {
                            fl.Value = 0;
                        }

                        if (fl.Type == OrganisationGroupingReason.Payment)
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
            }
            catch (Exception exception)
            {
                RecordErrors($"Unable to {ChangeName} for provider id {VariationContext.ProviderId}. {exception.Message}");
            }
            
            return Task.CompletedTask;
        }
    }
}