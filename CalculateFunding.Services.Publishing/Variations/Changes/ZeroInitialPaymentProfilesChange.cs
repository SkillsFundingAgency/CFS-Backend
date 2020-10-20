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
        private const string _changeName = "zero initial payment profiles";

        public ZeroInitialPaymentProfilesChange(ProviderVariationContext variationContext)
            : base(variationContext)
        {
        }

        protected override async Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            try
            {
                AsyncPolicy resiliencePolicy = variationsApplications.ResiliencePolicies.SpecificationsApiClient;
                ISpecificationsApiClient specificationsApiClient = variationsApplications.SpecificationsApiClient;

                ApiResponse<IEnumerable<ProfileVariationPointer>> variationPointersResponse =
                    await resiliencePolicy.ExecuteAsync(() =>
                        specificationsApiClient.GetProfileVariationPointers(VariationContext.RefreshState.SpecificationId));


                if (variationPointersResponse == null || variationPointersResponse.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    RecordErrors("Unable to obtain variation pointers");

                    return;
                }

                IEnumerable<ProfileVariationPointer> variationPointers = variationPointersResponse?.Content;

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
                RecordErrors($"Unable to {_changeName} for provider id {VariationContext.ProviderId}. {exception.Message}");
            }
        }
    }
}