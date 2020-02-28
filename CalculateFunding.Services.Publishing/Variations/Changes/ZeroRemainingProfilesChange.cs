using System;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ZeroRemainingProfilesChange : ProfileVariationPointerChange
    {
        public ZeroRemainingProfilesChange(ProviderVariationContext variationContext)
            : base(variationContext , "zero remaining profiles")
        {
        }

        protected override void MakeAdjustmentsFromProfileVariationPointer(ProfileVariationPointer variationPointer)
        {
            ZeroProfilesFromVariationPoint(variationPointer);
        }

        private void ZeroProfilesFromVariationPoint(ProfileVariationPointer variationPointer)
        {
            FundingLine fundingLine = RefreshState.FundingLines?
                .SingleOrDefault(_ => _.FundingLineCode == variationPointer.FundingLineId);

            if (fundingLine == null)
            {
                throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate a funding line for variation pointer with fundingLineId {variationPointer.FundingLineId}");
            }

            ProfilePeriod[] orderedProfilePeriods = new YearMonthOrderedProfilePeriods(fundingLine)
                .ToArray();
            
            int variationPointerIndex = GetProfilePeriodIndexForVariationPoint(variationPointer, orderedProfilePeriods);

            for (int profilePeriod = variationPointerIndex; profilePeriod < orderedProfilePeriods.Length; profilePeriod++)
            {
                orderedProfilePeriods[profilePeriod].ProfiledValue = 0;
            }
        }
    }
}