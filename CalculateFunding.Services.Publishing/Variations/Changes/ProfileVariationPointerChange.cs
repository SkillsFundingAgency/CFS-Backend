using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public abstract class ProfileVariationPointerChange : VariationChange
    {
        private readonly string _strategyName;

        protected ProfileVariationPointerChange(ProviderVariationContext variationContext, string strategyName)
            : base(variationContext, strategyName)
        {
            Guard.IsNullOrWhiteSpace(strategyName, nameof(strategyName));

            _strategyName = strategyName;
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            IEnumerable<ProfileVariationPointer> variationPointers = VariationContext.VariationPointers;

            if (!variationPointers.IsNullOrEmpty())
            {
                foreach (ProfileVariationPointer variationPointer in variationPointers)
                {
                    AddAffectedFundingLine(variationPointer.FundingLineId);
                    MakeAdjustmentsFromProfileVariationPointer(variationPointer);
                }
            }
            
            return Task.CompletedTask;
        }

        protected abstract void MakeAdjustmentsFromProfileVariationPointer(ProfileVariationPointer variationPointer);

        protected int GetProfilePeriodIndexForVariationPoint(ProfileVariationPointer variationPointer, ProfilePeriod[] profilePeriods)
        {
            int variationPointerIndex = profilePeriods.IndexOf(_ => _.Occurrence == variationPointer.Occurrence &&
                                                                    _.Year == variationPointer.Year &&
                                                                    _.TypeValue == variationPointer.TypeValue);

            if (variationPointerIndex == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate profile period corresponding to variation pointer for funding line id {variationPointer.FundingLineId}");
            }

            return variationPointerIndex;
        }
    }
}