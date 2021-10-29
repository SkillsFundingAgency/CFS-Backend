using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Newtonsoft.Json;
using Polly;
using System;
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
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IPublishedProviderLoaderForFundingGroupData _publishedProviderLoader;

        public FundingGroupDataGenerator(IPublishedFundingGenerator publishedFundingGenerator,
            IPoliciesService policiesService,
            IPublishedFundingDateService publishedFundingDateService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishedProviderLoaderForFundingGroupData publishedProviderLoader)
        {
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedFundingGenerator));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(publishedFundingDateService, nameof(publishedFundingDateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishedProviderLoader, nameof(publishedProviderLoader));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));

            _publishedFundingGenerator = publishedFundingGenerator;
            _policiesService = policiesService;
            _publishedFundingDateService = publishedFundingDateService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingDataService = publishedFundingDataService;
            _publishedProviderLoader = publishedProviderLoader;
        }

        public async Task<IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)>> Generate(
            IEnumerable<OrganisationGroupResult> organisationGroupsToCreate,
            SpecificationSummary specification,
            Channel channel,
            IEnumerable<string> batchPublishedProviderIds)
        {
            Reference fundingStream = specification.FundingStreams.First();
            
            List<PublishedProvider> publishedProviders =
                await _publishedProviderLoader.GetAllPublishedProviders(organisationGroupsToCreate, specification.Id, channel.ChannelId, batchPublishedProviderIds);

            PublishedFundingInput publishedFundingInput = new PublishedFundingInput
            {
                OrganisationGroupsToSave = await GeneratePublishedFundingOrganisationGroupResultTuples(organisationGroupsToCreate, specification, fundingStream),
                FundingStream = fundingStream,
                FundingPeriod = await _policiesService.GetFundingPeriodByConfigurationId(specification.FundingPeriod.Id),
                PublishingDates = await _publishedFundingDateService.GetDatesForSpecification(specification.Id),
                TemplateMetadataContents = await ReadTemplateMetadataContents(fundingStream, specification),
                TemplateVersion = specification.TemplateIds[fundingStream.Id],
                SpecificationId = specification.Id
            };

            return GenerateOutput(organisationGroupsToCreate, publishedProviders, publishedFundingInput);
        }

        private async Task<List<(PublishedFunding, OrganisationGroupResult)>> GeneratePublishedFundingOrganisationGroupResultTuples(IEnumerable<OrganisationGroupResult> organisationGroupsToCreate, SpecificationSummary specification, Reference fundingStream)
        {
            IEnumerable<PublishedFunding> publishedFundings = await _publishingResiliencePolicy.ExecuteAsync(() =>
                           _publishedFundingDataService.GetCurrentPublishedFunding(fundingStream.Id, specification.FundingPeriod.Id));

            List<(PublishedFunding, OrganisationGroupResult)> organisationGroupsToSave = new List<(PublishedFunding, OrganisationGroupResult)>();

            foreach (OrganisationGroupResult organisationGroup in organisationGroupsToCreate)
            {
                PublishedFunding publishedFundingForOrganisationGroup = publishedFundings?
                    .Where(_ => organisationGroup.IdentifierValue == _.Current.OrganisationGroupIdentifierValue &&
                        organisationGroup.GroupTypeCode == Enum.Parse<OrganisationGroupTypeCode>(_.Current.OrganisationGroupTypeCode) &&
                        organisationGroup.GroupTypeClassification == Enum.Parse<OrganisationGroupTypeClassification>(_.Current.OrganisationGroupTypeClassification) &&
                        organisationGroup.GroupTypeIdentifier == Enum.Parse<OrganisationGroupTypeIdentifier>(_.Current.OrganisationGroupTypeIdentifier) &&
                        organisationGroup.GroupReason == Enum.Parse<OrganisationGroupingReason>(_.Current.GroupingReason.ToString()))
                    .OrderBy(_ => _.Current.Version).LastOrDefault();

                organisationGroupsToSave.Add((publishedFundingForOrganisationGroup, organisationGroup));
            }

            return organisationGroupsToSave;
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

        private IEnumerable<(PublishedFundingVersion, OrganisationGroupResult)> GenerateOutput(IEnumerable<OrganisationGroupResult> organisationGroupsToCreate, List<PublishedProvider> publishedProviders, PublishedFundingInput publishedFundingInput)
        {
            IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundings =
                                _publishedFundingGenerator.GeneratePublishedFunding(publishedFundingInput, publishedProviders).ToList();

            AggregateVariationReasons(publishedProviders, publishedFundings);

            List<(PublishedFundingVersion, OrganisationGroupResult)> result = new List<(PublishedFundingVersion, OrganisationGroupResult)>();

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

                result.Add((publishedFunding.PublishedFundingVersion, organisationGroupResult));
            }

            return result;
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
