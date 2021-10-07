using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    /// <summary>
    /// Generates variation reasons for providers based on the previously released latest major version in a channel.
    /// </summary>
    public class GenerateVariationReasonsForChannelService : IGenerateVariationReasonsForChannelService
    {
        private readonly IDetectProviderVariations _detectProviderVariations;
        private readonly IPublishedProvidersLoadContext _publishedProvidersLoadContext;
        private readonly IReleaseManagementRepository _repo;
        private readonly IProviderService _providerService;
        private readonly ISpecificationService _specificationService;

        private static readonly VariationReason[] _initialReleaseToChannelVariationReasons = new[] { VariationReason.FundingUpdated, VariationReason.ProfilingUpdated };
        private readonly IDictionary<string, PublishedProviderSnapShots> _emptySnapshots = new Dictionary<string, PublishedProviderSnapShots>();

        public GenerateVariationReasonsForChannelService(IDetectProviderVariations detectProviderVariations,
                IPublishedProvidersLoadContext publishedProvidersLoadContext,
                IReleaseManagementRepository releaseManagementRepository,
                IProviderService providerService,
                ISpecificationService specificationsService
                )
        {
            Guard.ArgumentNotNull(detectProviderVariations, nameof(detectProviderVariations));
            Guard.ArgumentNotNull(publishedProvidersLoadContext, nameof(publishedProvidersLoadContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(specificationsService, nameof(specificationsService));

            _detectProviderVariations = detectProviderVariations;
            _publishedProvidersLoadContext = publishedProvidersLoadContext;
            _repo = releaseManagementRepository;
            _providerService = providerService;
            _specificationService = specificationsService;
        }

        public async Task<IDictionary<string, IEnumerable<VariationReason>>> GenerateVariationReasonsForProviders(
            IEnumerable<string> batchProviderIds,
            Channel channel,
            SpecificationSummary specification,
            FundingConfiguration fundingConfiguration,
           IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResults)
        {
            Guard.ArgumentNotNull(batchProviderIds, nameof(batchProviderIds));
            Guard.ArgumentNotNull(channel, nameof(channel));
            Guard.ArgumentNotNull(specification, nameof(specification));
            Guard.ArgumentNotNull(fundingConfiguration, nameof(fundingConfiguration));
            Guard.ArgumentNotNull(organisationGroupResults, nameof(organisationGroupResults));

            Task<IEnumerable<PublishedProvider>> currentProviderStatesRequest = _publishedProvidersLoadContext.GetOrLoadProviders(batchProviderIds);
            Task<IEnumerable<ProviderVersionInChannel>> latestPublishedVersionInChannelRequest = _repo.GetLatestPublishedProviderVersions(specification.Id, new[] { channel.ChannelId });
            Task<IDictionary<string, Provider>> providersRequest = _providerService.GetScopedProvidersForSpecification(specification.Id, specification.ProviderVersionId);
            Task<IEnumerable<ProfileVariationPointer>> variationPointersRequest = _specificationService.GetProfileVariationPointers(specification.Id);

            await TaskHelper.WhenAllAndThrow(currentProviderStatesRequest,
                                             latestPublishedVersionInChannelRequest,
                                             providersRequest,
                                             variationPointersRequest);

            Dictionary<string, PublishedProvider> providersBeingReleased = currentProviderStatesRequest.Result.ToDictionary(_ => _.Current.ProviderId);
            Dictionary<string, ProviderVersionInChannel> latestPublishedVersionInChannel = latestPublishedVersionInChannelRequest.Result.ToDictionary(_ => _.ProviderId);
            IDictionary<string, Provider> providers = providersRequest.Result;
            IEnumerable<ProfileVariationPointer> variationPointers = variationPointersRequest.Result;

            Dictionary<string, IEnumerable<VariationReason>> variationReasonsForProviders = new Dictionary<string, IEnumerable<VariationReason>>();

            foreach (KeyValuePair<string, PublishedProvider> currentProviderItem in providersBeingReleased)
            {
                PublishedProvider currentState = currentProviderItem.Value;

                ProviderVersionInChannel providerVersionInChannel = latestPublishedVersionInChannel.GetValueOrDefault(currentProviderItem.Key);

                IEnumerable<VariationReason> providerVariationReasons;

                if (ProviderHasPreviouslyBeenReleasedToThisChannel(providerVersionInChannel))
                {
                    if (providerVersionInChannel.MajorVersion != currentState.Released.MajorVersion)
                    {
                        PublishedProvider previousReleasedMajorVersion =
                            await _publishedProvidersLoadContext.GetOrLoadProvider(currentProviderItem.Key, providerVersionInChannel.MajorVersion);

                        ProviderVariationContext variationContext = await _detectProviderVariations.CreateRequiredVariationChanges(
                                previousReleasedMajorVersion,
                                currentState.Current.TotalFunding,
                                providers[currentProviderItem.Key],
                                fundingConfiguration.ReleaseManagementVariations,
                                _emptySnapshots,
                                providersBeingReleased,
                                variationPointers,
                                specification.ProviderVersionId,
                                organisationGroupResults
                                );

                        providerVariationReasons = ProviderHasGeneratedVariationReasons(variationContext)
                            ? new HashSet<VariationReason>(variationContext.VariationReasons)
                            : (IEnumerable<VariationReason>)System.Array.Empty<VariationReason>();
                    }
                    else
                    {
                        providerVariationReasons = Array.Empty<VariationReason>();
                    }
                }
                else
                {
                    providerVariationReasons = _initialReleaseToChannelVariationReasons;
                }

                variationReasonsForProviders.Add(currentProviderItem.Key, providerVariationReasons);
            }

            return variationReasonsForProviders;
        }

        private static bool ProviderHasGeneratedVariationReasons(ProviderVariationContext variationContext)
        {
            return variationContext.VariationReasons.AnyWithNullCheck();
        }

        private static bool ProviderHasPreviouslyBeenReleasedToThisChannel(ProviderVersionInChannel providerVersionInChannel)
        {
            return providerVersionInChannel != null;
        }
    }
}
