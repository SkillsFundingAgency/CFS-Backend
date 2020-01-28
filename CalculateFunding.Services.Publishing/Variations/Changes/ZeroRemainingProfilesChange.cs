using System;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ZeroRemainingProfilesChange : ProfileVariationPointerChange
    {
        public ZeroRemainingProfilesChange(ProviderVariationContext variationContext)
            : base(variationContext , "zero profiles")
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

            int variationPointerIndex = orderedProfilePeriods.IndexOf(_ => _.Occurrence == variationPointer.Occurrence &&
                                                                           _.Year == variationPointer.Year &&
                                                                           _.TypeValue == variationPointer.TypeValue);

            if (variationPointerIndex == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate profile period corresponding to variation pointer for funding line id {variationPointer.FundingLineId}");
            }

            for (int profilePeriod = variationPointerIndex; profilePeriod < orderedProfilePeriods.Length; profilePeriod++)
            {
                orderedProfilePeriods[profilePeriod].ProfiledValue = 0;
            }
        }
    }
}