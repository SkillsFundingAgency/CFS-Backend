using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    /// <summary>
    /// Used in FundingGroupDataGenerator
    /// </summary>
    public class PublishedProviderLoaderForFundingGroupData : IPublishedProviderLoaderForFundingGroupData
    {
        private readonly IPublishedProvidersLoadContext _publishProvidersLoadContext;
        private readonly IReleaseManagementRepository _repo;

        public PublishedProviderLoaderForFundingGroupData(IPublishedProvidersLoadContext publishProvidersLoadContext,
            IReleaseManagementRepository releaseManagementRepository)
        {
            Guard.ArgumentNotNull(publishProvidersLoadContext, nameof(publishProvidersLoadContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            _publishProvidersLoadContext = publishProvidersLoadContext;
            _repo = releaseManagementRepository;
        }

        /// <summary>
        /// Gets all PublishedProviders from PublishedProvidersLoadContext for provided <paramref name="batchPublishedProviderIds"/>
        ///  including those that are in the <paramref name="organisationGroupResults" /> enumerable.
        /// </summary>
        /// <param name="organisationGroupResults">The OrganisationGroupResults that are being released</param>
        /// <param name="specificationId">Specification id</param>
        /// <param name="channelId">Channel id</param>
        /// <param name="batchPublishedProviderIds">The batch of published provider ids in the release</param>
        /// <returns></returns>
        public async Task<List<PublishedProvider>> GetAllPublishedProviders(
            IEnumerable<OrganisationGroupResult> organisationGroupResults,
            string specificationId,
            int channelId,
            IEnumerable<string> batchPublishedProviderIds)
        {
            IEnumerable<ProviderVersionInChannel> providerVersionsInChannel =
                await _repo.GetLatestPublishedProviderVersions(specificationId, new List<int> { channelId });

            List<PublishedProvider> publishedProviders = new List<PublishedProvider>();

            IEnumerable<PublishedProvider> publishedProvidersInBatch = await _publishProvidersLoadContext.GetOrLoadProviders(batchPublishedProviderIds);

            publishedProviders.AddRange(publishedProvidersInBatch);

            IEnumerable<string> organisationGroupProviderIds = organisationGroupResults.SelectMany(s => s.Providers).Select(s => s.ProviderId).Distinct();
            IEnumerable<string> providerIdsNotInBatch = organisationGroupProviderIds.Except(batchPublishedProviderIds);

            foreach (string providerId in providerIdsNotInBatch)
            {
                ProviderVersionInChannel providerVersion = providerVersionsInChannel.SingleOrDefault(s => s.ProviderId == providerId);

                if (providerVersion == null)
                {
                    throw new NonRetriableException($"Provider version not found for providerId {providerId}");
                }

                PublishedProvider publishedProvider = await _publishProvidersLoadContext.GetOrLoadProvider(providerId, providerVersion.MajorVersion);
                publishedProviders.Add(publishedProvider);
            }

            return publishedProviders;
        }
    }
}
