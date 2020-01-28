using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
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
            _changeName = changeName;
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
                    RecordErrors($"Unable to {_changeName} for provider id {VariationContext.ProviderId}");
                    
                    return;
                }

                foreach (ProfileVariationPointer variationPointer in variationPointers)
                {
                    MakeAdjustmentsFromProfileVariationPointer(variationPointer);
                }
            }
            catch (Exception exception)
            {
                RecordErrors($"Unable to {_changeName} for provider id {VariationContext.ProviderId}. {exception.Message}");
            }
        }

        protected abstract void MakeAdjustmentsFromProfileVariationPointer(ProfileVariationPointer variationPointer);
    }
}