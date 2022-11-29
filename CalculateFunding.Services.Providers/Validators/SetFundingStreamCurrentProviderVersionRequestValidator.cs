using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.Validators
{
    public class SetFundingStreamCurrentProviderVersionRequestValidator : AbstractValidator<SetFundingStreamCurrentProviderVersionRequest>
    {
        public SetFundingStreamCurrentProviderVersionRequestValidator(IProviderVersionsMetadataRepository providerVersions,
            IPoliciesApiClient policiesApiClient,
            IFundingDataZoneApiClient fundingDataZoneApiClient,
            IProvidersResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(providerVersions, nameof(providerVersions));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ProviderVersionMetadataRepository, nameof(resiliencePolicies.ProviderVersionMetadataRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.FundingDataZoneApiClient, nameof(resiliencePolicies.FundingDataZoneApiClient));
            
            RuleFor(_ => _.FundingStreamId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .CustomAsync(async (fundingStreamId,
                    context,
                    cancellationToken) =>
                {
                    ApiResponse<FundingStream> response = await resiliencePolicies.PoliciesApiClient.ExecuteAsync(() =>
                        policiesApiClient.GetFundingStreamById(fundingStreamId));

                    if (response?.Content == null)
                    {
                        context.AddFailure(new ValidationFailure("FundingStreamId",
                            $"No funding stream located with Id {fundingStreamId}"));
                    }
                });
          

            RuleFor(_ => _.ProviderVersionId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty()
                .CustomAsync(async (providerVersionId,
                    context,
                    cancellationToken) =>
                {
                    ProviderVersionMetadata response = await resiliencePolicies.ProviderVersionMetadataRepository.ExecuteAsync(() =>
                        providerVersions.GetProviderVersionMetadata(providerVersionId));

                    if (response == null)
                    {
                        context.AddFailure(new ValidationFailure("ProviderVersionId", 
                            $"No provider version located with Id {providerVersionId}"));
                    }
                });

            RuleFor(_ => _.ProviderSnapshotId)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .CustomAsync(async (providerSnapshotId,
                    context,
                    cancellationToken) =>
                {
                    if (providerSnapshotId != null)
                    {
                        ApiResponse<IEnumerable<ProviderSnapshot>> providerSnapshotsResponse = await resiliencePolicies.FundingDataZoneApiClient.ExecuteAsync(() =>
                            fundingDataZoneApiClient.GetLatestProviderSnapshotsForAllFundingStreams());

                        if (providerSnapshotsResponse?.Content == null)
                        {
                            context.AddFailure(new ValidationFailure("ProviderSnapshotId",
                                $"No provider snapshots located"));
                        }

                        SetFundingStreamCurrentProviderVersionRequest fundingStreamCurrentProviderVersionRequest = context.ParentContext.InstanceToValidate as SetFundingStreamCurrentProviderVersionRequest;
                   
                        IEnumerable<ProviderSnapshot> latestProviderSnapshot = providerSnapshotsResponse?.Content.Where(x => x.FundingStreamCode == fundingStreamCurrentProviderVersionRequest.FundingStreamId);
                        if (!latestProviderSnapshot.Select(x => x.ProviderVersionId).Contains(fundingStreamCurrentProviderVersionRequest.ProviderVersionId))
                        {
                            context.AddFailure(new ValidationFailure("ProviderSnapshotId",
                                $"Unable to set current to version as it is not currently the latest snapshot provider version"));
                        }

                        if (!latestProviderSnapshot.Select(x => x?.ProviderSnapshotId).Contains(providerSnapshotId))
                        {
                            context.AddFailure(new ValidationFailure("ProviderSnapshotId",
                                $"Unable to set current to version as the version doesn't match the snap shot"));
                        }
                    }
                });
        }
    }
}