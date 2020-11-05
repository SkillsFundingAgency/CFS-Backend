using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public abstract class ProfileVariationPointerChange : VariationChange
    {
        private readonly string _changeName;
        protected ProfileVariationPointerChange(ProviderVariationContext variationContext, string changeName)
            : base(variationContext)
        {
            Guard.IsNullOrWhiteSpace(changeName, nameof(changeName));

            _changeName = changeName;
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            try
            {
                IEnumerable<ProfileVariationPointer> variationPointers = VariationContext.VariationPointers;

                if (!variationPointers.IsNullOrEmpty())
                {
                    foreach (ProfileVariationPointer variationPointer in variationPointers)
                    {
                        MakeAdjustmentsFromProfileVariationPointer(variationPointer);
                    }
                }
            }
            catch (Exception exception)
            {
                RecordErrors($"Unable to {_changeName} for provider id {VariationContext.ProviderId}. {exception.Message}");
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