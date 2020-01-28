using System;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;

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

            int variationPointerIndex = orderedClosedProfilePeriods.IndexOf(_ => _.Occurrence == variationPointer.Occurrence &&
                                                                           _.Year == variationPointer.Year &&
                                                                           _.TypeValue == variationPointer.TypeValue);

            if (variationPointerIndex == -1)
            {
                throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate profile period corresponding to variation pointer for funding line id {variationPointer.FundingLineId}");
            }

            for (int profilePeriod = variationPointerIndex; profilePeriod < orderedClosedProfilePeriods.Length; profilePeriod++)
            {
                ProfilePeriod successorProfilePeriod = orderedSuccessorProfilePeriods[profilePeriod];
                
                successorProfilePeriod.ProfiledValue =  successorProfilePeriod.ProfiledValue + orderedClosedProfilePeriods[profilePeriod].ProfiledValue;
            }
        }
    }
}