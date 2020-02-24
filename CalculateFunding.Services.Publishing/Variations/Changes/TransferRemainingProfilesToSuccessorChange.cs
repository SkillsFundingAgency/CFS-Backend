using System;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class TransferRemainingProfilesToSuccessorChange : ProfileVariationPointerChange
    {
        public TransferRemainingProfilesToSuccessorChange(ProviderVariationContext variationContext) 
            : base(variationContext, "transfer remaining profiles")
        {
        }
        
        protected override void MakeAdjustmentsFromProfileVariationPointer(ProfileVariationPointer variationPointer)
        {
            FundingLine closedFundingLine = RefreshState.FundingLines?
                .SingleOrDefault(_ => _.FundingLineCode == variationPointer.FundingLineId);
            FundingLine successorFundingLine = SuccessorRefreshState?.FundingLines?
                .SingleOrDefault(_ => _.FundingLineCode == variationPointer.FundingLineId);

            if (closedFundingLine == null || successorFundingLine == null)
            {
                throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate a funding line for variation pointer with fundingLineId {variationPointer.FundingLineId}");
            }

            ProfilePeriod[] orderedClosedProfilePeriods = new YearMonthOrderedProfilePeriods(closedFundingLine)
                .ToArray();
            ProfilePeriod[] orderedSuccessorProfilePeriods = new YearMonthOrderedProfilePeriods(successorFundingLine)
                .ToArray();

            int variationPointerIndex = GetProfilePeriodIndexForVariationPoint(variationPointer, orderedClosedProfilePeriods);
            
            for (int profilePeriod = variationPointerIndex; profilePeriod < orderedClosedProfilePeriods.Length; profilePeriod++)
            {
                ProfilePeriod successorProfilePeriod = orderedSuccessorProfilePeriods[profilePeriod];
                
                successorProfilePeriod.ProfiledValue =  successorProfilePeriod.ProfiledValue + orderedClosedProfilePeriods[profilePeriod].ProfiledValue;
            }
        }
    }
}