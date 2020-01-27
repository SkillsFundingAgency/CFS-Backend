using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ZeroRemainingProfilesChange : VariationChange
    {
        public ZeroRemainingProfilesChange(ProviderVariationContext variationContext)
            : base(variationContext)
        {
        }

        protected override async Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            try
            {
                Policy resiliencePolicy = variationsApplications.ResiliencePolicies.SpecificationsApiClient;
                ISpecificationsApiClient specificationsApiClient = variationsApplications.SpecificationsApiClient;

                ApiResponse<IEnumerable<ProfileVariationPointer>> variationPointersResponse =
                    await resiliencePolicy.ExecuteAsync(() =>
                        specificationsApiClient.GetProfileVariationPointers(VariationContext.RefreshState.SpecificationId));

                IEnumerable<ProfileVariationPointer> variationPointers = variationPointersResponse?.Content;

                if (variationPointers.IsNullOrEmpty())
                {
                    RecordErrors($"Unable to zero profiles for provider id {VariationContext.ProviderId}");
                    
                    return;
                }

                foreach (ProfileVariationPointer variationPointer in variationPointers)
                {
                    ZeroProfilesFromVariationPoint(variationPointer);
                }
            }
            catch (Exception exception)
            {
                RecordErrors($"Unable to zero profiles for provider id {VariationContext.ProviderId}. {exception.Message}");
            }
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