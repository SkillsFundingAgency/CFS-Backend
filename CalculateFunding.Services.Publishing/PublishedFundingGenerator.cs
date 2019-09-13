using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;
using PublishingModels = CalculateFunding.Models.Publishing;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingGenerator : IPublishedFundingGenerator
    {
        private IMapper _mapper;
        private IPublishedFundingIdGeneratorResolver _publishedFundingIdGeneratorResolver;

        public PublishedFundingGenerator(IMapper mapper, IPublishedFundingIdGeneratorResolver publishedFundingIdGeneratorResolver)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _mapper = mapper;
            _publishedFundingIdGeneratorResolver = publishedFundingIdGeneratorResolver;
        }
        /// <summary>
        /// Generate instances of the PublishedFundingVersion to save into cosmos for the Organisation Group Results
        /// </summary>
        /// <param name="organisationGroupsToSave"></param>
        /// <param name="templateMetadataContents"></param>
        /// <param name="publishedProviders"></param>
        /// <returns></returns>
        public IEnumerable<(PublishingModels.PublishedFunding, PublishingModels.PublishedFundingVersion)> GeneratePublishedFunding(IEnumerable<(PublishingModels.PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> organisationGroupsToSave, TemplateMetadataContents templateMetadataContents, IEnumerable<PublishingModels.PublishedProvider> publishedProviders, string templateVersion)
        {
            foreach (var organisationGroup in organisationGroupsToSave)
            {
                PublishingModels.PublishedProvider publishedProvider = publishedProviders.Where(_ => organisationGroup.OrganisationGroupResult.Providers.Any(provider => provider.ProviderId == _.Current.ProviderId)).SingleOrDefault();

                IEnumerable<TemplateModels.FundingLine> fundingLines = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines) ?? new FundingLine[0];

                IEnumerable<TemplateModels.Calculation> calculations = fundingLines.SelectMany(_ => _.Calculations.Flatten(calculation => calculation.Calculations)) ?? new Calculation[0];

                IEnumerable<TemplateModels.ReferenceData> refernceData = calculations.Where(_ => _.ReferenceData != null)?.SelectMany(_ => _.ReferenceData) ?? new ReferenceData[0];

                PublishingModels.PublishedFundingVersion publishedFundingVersion = new PublishingModels.PublishedFundingVersion
                {
                    FundingStreamId = publishedProvider.Current.FundingStreamId,
                    FundingPeriod = new PublishingModels.PublishedFundingPeriod
                    {
                        Type = Enum.Parse<PublishingModels.PublishedFundingPeriodType>(publishedProvider.Current.FundingPeriodId.Split('-')[0]),
                        Period = publishedProvider.Current.FundingPeriodId.Split('-')[1]
                    },
                    SpecificationId = publishedProvider.Current.SpecificationId,
                    OrganisationGroupTypeCode = organisationGroup.OrganisationGroupResult.GroupTypeCode.ToString(),
                    OrganisationGroupTypeIdentifier = organisationGroup.OrganisationGroupResult.GroupTypeIdentifier.ToString(),
                    OrganisationGroupIdentifierValue = organisationGroup.OrganisationGroupResult.IdentifierValue,
                    OrganisationGroupTypeCategory = organisationGroup.OrganisationGroupResult.GroupTypeClassification.ToString(),
                    OrganisationGroupName = organisationGroup.OrganisationGroupResult.Name,
                    OrganisationGroupSearchableName = organisationGroup.OrganisationGroupResult.SearchableName,
                    OrganisationGroupIdentifiers = _mapper.Map<IEnumerable<PublishingModels.PublishedOrganisationGroupTypeIdentifier>>(organisationGroup.OrganisationGroupResult.Identifiers),
                    FundingLines = _mapper.Map<IEnumerable<PublishingModels.FundingLine>>(fundingLines),
                    Calculations = _mapper.Map<IEnumerable<PublishingModels.FundingCalculation>>(calculations),
                    ReferenceData = _mapper.Map<IEnumerable<PublishingModels.FundingReferenceData>>(refernceData),
                    SchemaVersion = templateMetadataContents.SchemaVersion,
                    Status = PublishingModels.PublishedFundingStatus.Approved,
                    MinorVersion = publishedProvider.Current.MinorVersion,
                    MajorVersion = publishedProvider.Current.MajorVersion,
                    GroupingReason = organisationGroup.OrganisationGroupResult.GroupReason.AsMatchingEnum<PublishingModels.GroupingReason>(),
                    ProviderFundings = publishedProviders.Select(_ => _.Current.Id),
                    TemplateVersion = templateVersion
                };

                publishedFundingVersion.FundingId = _publishedFundingIdGeneratorResolver.GetService(templateMetadataContents.SchemaVersion).GetFundingId(publishedFundingVersion);

                yield return (organisationGroup.PublishedFunding, publishedFundingVersion);
            }
        }
    }
}
