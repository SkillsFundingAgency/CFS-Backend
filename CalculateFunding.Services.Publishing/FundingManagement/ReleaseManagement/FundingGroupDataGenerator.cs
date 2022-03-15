using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    /// <summary>
    /// Generates the funding group data for release
    /// </summary>
    public class FundingGroupDataGenerator : IFundingGroupDataGenerator
    {
        private readonly IPublishedFundingGenerator _publishedFundingGenerator;
        private readonly IPoliciesService _policiesService;
        private readonly IPublishedFundingDateService _publishedFundingDateService;
        private readonly IReleaseManagementRepository _repo;
        private readonly IPublishedProviderLoaderForFundingGroupData _publishedProviderLoader;
        private readonly IPublishedFundingIdGeneratorResolver _publishedFundingIdGeneratorResolver;

        public FundingGroupDataGenerator(IPublishedFundingGenerator publishedFundingGenerator,
            IPoliciesService policiesService,
            IPublishedFundingDateService publishedFundingDateService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IReleaseManagementRepository releaseManagementRepository,
            IPublishedProviderLoaderForFundingGroupData publishedProviderLoader,
            IPublishedFundingIdGeneratorResolver publishedFundingIdGeneratorResolver)
        {
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedFundingGenerator));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(publishedFundingDateService, nameof(publishedFundingDateService));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(publishedProviderLoader, nameof(publishedProviderLoader));
            Guard.ArgumentNotNull(publishedFundingIdGeneratorResolver, nameof(publishedFundingIdGeneratorResolver));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));

            _publishedFundingGenerator = publishedFundingGenerator;
            _policiesService = policiesService;
            _publishedFundingDateService = publishedFundingDateService;
            _repo = releaseManagementRepository;
            _publishedProviderLoader = publishedProviderLoader;
            _publishedFundingIdGeneratorResolver = publishedFundingIdGeneratorResolver;
        }

        public async Task<IEnumerable<GeneratedPublishedFunding>> Generate(
            IEnumerable<OrganisationGroupResult> organisationGroupsToCreate,
            SpecificationSummary specification,
            Channel channel,
            IEnumerable<string> batchPublishedProviderIds,
            Reference author,
            string jobId,
            string correlationId)
        {
            Reference fundingStream = specification.FundingStreams.First();

            List<PublishedProvider> publishedProviders =
                await _publishedProviderLoader.GetAllPublishedProviders(organisationGroupsToCreate, specification.Id, channel.ChannelId, batchPublishedProviderIds);

            PublishedFundingInput publishedFundingInput = new PublishedFundingInput
            {
                OrganisationGroupsToSave = organisationGroupsToCreate.Select(_ => ((PublishedFunding)null, _)),
                FundingStream = fundingStream,
                FundingPeriod = await _policiesService.GetFundingPeriodByConfigurationId(specification.FundingPeriod.Id),
                PublishingDates = _publishedFundingDateService.GetDatesForSpecification(),
                TemplateMetadataContents = await ReadTemplateMetadataContents(fundingStream, specification),
                TemplateVersion = specification.TemplateIds[fundingStream.Id],
                SpecificationId = specification.Id,
            };

            IEnumerable<LatestFundingGroupVersion> latestFundingGroupsForChannel = await _repo.GetLatestFundingGroupMajorVersionsBySpecificationId(specification.Id, channel.ChannelId);


            return GenerateOutput(organisationGroupsToCreate,
                                  publishedProviders,
                                  publishedFundingInput,
                                  latestFundingGroupsForChannel,
                                  author,
                                  jobId,
                                  correlationId);
        }

        private async Task<TemplateMetadataContents> ReadTemplateMetadataContents(Reference fundingStream, SpecificationSummary specification)
        {
            TemplateMetadataContents templateMetadataContents =
                await _policiesService.GetTemplateMetadataContents(fundingStream.Id, specification.FundingPeriod.Id, specification.TemplateIds[fundingStream.Id]);

            if (templateMetadataContents == null)
            {
                throw new NonRetriableException($"Unable to get template metadata contents for funding stream. '{fundingStream.Id}'");
            }

            return templateMetadataContents;
        }

        private IEnumerable<GeneratedPublishedFunding> GenerateOutput(IEnumerable<OrganisationGroupResult> organisationGroupsToCreate, List<PublishedProvider> publishedProviders, PublishedFundingInput publishedFundingInput, IEnumerable<LatestFundingGroupVersion> latestFundingGroupsForChannel, Reference author, string jobId, string correlationId)
        {
            IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundings =
                                _publishedFundingGenerator.GeneratePublishedFunding(publishedFundingInput, publishedProviders, author, jobId, correlationId).ToList();

            AggregateVariationReasons(publishedProviders, publishedFundings);

            List<GeneratedPublishedFunding> result = new List<GeneratedPublishedFunding>();

            Dictionary<string, LatestFundingGroupVersion> latestFundingVersions =
                latestFundingGroupsForChannel
                .ToDictionary(_ => $"{_.FundingStreamCode}_{_.FundingPeriodCode}_{_.GroupingReasonCode}_{_.OrganisationGroupTypeCode}_{_.OrganisationGroupIdentifierValue}");


            foreach ((PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion) publishedFunding in publishedFundings)
            {
                OrganisationGroupResult organisationGroupResult = organisationGroupsToCreate.SingleOrDefault(
                    fg => fg.GroupReason.ToString() == publishedFunding.PublishedFundingVersion.GroupingReason.ToString()
                    && fg.GroupTypeCode.ToString() == publishedFunding.PublishedFundingVersion.OrganisationGroupTypeCode
                    && fg.GroupTypeIdentifier.ToString() == publishedFunding.PublishedFundingVersion.OrganisationGroupTypeIdentifier);

                if (organisationGroupResult == null)
                {
                    throw new NonRetriableException($"Organisation group result not found for ${JsonConvert.SerializeObject(publishedFunding.PublishedFundingVersion)}");
                }

                publishedFunding.PublishedFundingVersion.MajorVersion = GetMajorVersionForRelease(publishedFunding.PublishedFundingVersion, latestFundingVersions);
                publishedFunding.PublishedFundingVersion.FundingId = _publishedFundingIdGeneratorResolver
                    .GetService(publishedFunding.PublishedFundingVersion.SchemaVersion)
                    .GetFundingId(publishedFunding.PublishedFundingVersion);


                result.Add(new GeneratedPublishedFunding()
                {
                    PublishedFunding = publishedFunding.PublishedFunding,
                    PublishedFundingVersion = publishedFunding.PublishedFundingVersion,
                    OrganisationGroupResult = organisationGroupResult,
                });
            }

            return result;
        }

        private int GetMajorVersionForRelease(PublishedFundingVersion publishedFundingVersion, Dictionary<string, LatestFundingGroupVersion> latestFundingVersions)
        {
            if (latestFundingVersions.TryGetValue($"{publishedFundingVersion.FundingStreamId}_{publishedFundingVersion.FundingPeriod.Id}_{publishedFundingVersion.GroupingReason}_{publishedFundingVersion.OrganisationGroupTypeCode}_{publishedFundingVersion.OrganisationGroupIdentifierValue}", out LatestFundingGroupVersion latestFunding))
            {
                // Existing version, so increase the major version by 1
                return latestFunding.MajorVersion + 1;
            }
            else
            {
                // Initial version for this group in this channel
                return 1;
            }
        }


        private void AggregateVariationReasons(List<PublishedProvider> publishedProviders, IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundingToSave)
        {
            foreach ((PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion) publishedFundingItem in publishedFundingToSave)
            {
                foreach (PublishedProvider publishedProvider in publishedProviders)
                {
                    publishedFundingItem.PublishedFundingVersion.AddVariationReasons(publishedProvider.Current.VariationReasons);
                }
            }
        }
    }
}
