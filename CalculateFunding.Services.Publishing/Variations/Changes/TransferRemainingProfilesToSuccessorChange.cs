using System;
using System.Linq;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class TransferRemainingProfilesToSuccessorChange : ProfileVariationPointerChange
    {
        protected override string ChangeName => "Transfer remaining profiles";

        public TransferRemainingProfilesToSuccessorChange(ProviderVariationContext variationContext, string strategyName) 
            : base(variationContext, strategyName)
        {
        }
        
        protected override void MakeAdjustmentsFromProfileVariationPointer(ProfileVariationPointer variationPointer)
        {
            FundingLine closedFundingLine = RefreshState.FundingLines?
                .SingleOrDefault(_ => _.FundingLineCode == variationPointer.FundingLineId);

            if (closedFundingLine == null || SuccessorRefreshState == null)
            {
                throw new ArgumentOutOfRangeException(nameof(variationPointer),
                    $"Did not locate a funding line for variation pointer with fundingLineId {variationPointer.FundingLineId}");
            }

            FundingLine successorFundingLine = SuccessorRefreshState.FundingLines?
                .SingleOrDefault(_ => _.FundingLineCode == variationPointer.FundingLineId);

            if (successorFundingLine == null)
            {
                // copy the closed funding line so we don't get side effects
                successorFundingLine = closedFundingLine.DeepCopy();

                foreach (ProfilePeriod profilePeriod in successorFundingLine.DistributionPeriods.SelectMany(dp => dp.ProfilePeriods))
                {
                    profilePeriod.ProfiledValue = 0;
                }

                // add the funding line to the successor published provider
                SuccessorRefreshState.FundingLines = (SuccessorRefreshState.FundingLines ?? ArraySegment<FundingLine>.Empty).Concat(new[] { successorFundingLine });
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