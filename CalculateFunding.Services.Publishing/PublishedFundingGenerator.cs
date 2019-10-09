using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using PublishingModels = CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingGenerator : IPublishedFundingGenerator
    {
        private readonly IMapper _mapper;
        private readonly IPublishedFundingIdGeneratorResolver _publishedFundingIdGeneratorResolver;

        public PublishedFundingGenerator(IMapper mapper,
            IPublishedFundingIdGeneratorResolver publishedFundingIdGeneratorResolver)
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
        public IEnumerable<(PublishingModels.PublishedFunding, PublishingModels.PublishedFundingVersion)> GeneratePublishedFunding(GeneratePublishedFundingInput generatePublishedFundingInput)
        {
            Guard.ArgumentNotNull(generatePublishedFundingInput, nameof(generatePublishedFundingInput));
            Guard.ArgumentNotNull(generatePublishedFundingInput.FundingPeriod, nameof(generatePublishedFundingInput.FundingPeriod));
            Guard.ArgumentNotNull(generatePublishedFundingInput.FundingStream, nameof(generatePublishedFundingInput.FundingStream));
            Guard.ArgumentNotNull(generatePublishedFundingInput.OrganisationGroupsToSave, nameof(generatePublishedFundingInput.OrganisationGroupsToSave));
            Guard.ArgumentNotNull(generatePublishedFundingInput.PublishedProviders, nameof(generatePublishedFundingInput.PublishedProviders));
            Guard.ArgumentNotNull(generatePublishedFundingInput.PublishingDates, nameof(generatePublishedFundingInput.PublishingDates));
            Guard.ArgumentNotNull(generatePublishedFundingInput.TemplateMetadataContents, nameof(generatePublishedFundingInput.TemplateMetadataContents));
            Guard.IsNullOrWhiteSpace(generatePublishedFundingInput.TemplateVersion, nameof(generatePublishedFundingInput.TemplateVersion));
            Guard.IsNullOrWhiteSpace(generatePublishedFundingInput.SpecificationId, nameof(generatePublishedFundingInput.SpecificationId));

            IEnumerable<(PublishingModels.PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> organisationGroupsToSave = generatePublishedFundingInput.OrganisationGroupsToSave;

            TemplateMetadataContents templateMetadataContents = generatePublishedFundingInput.TemplateMetadataContents;
            IEnumerable<PublishingModels.PublishedProvider> publishedProviders = generatePublishedFundingInput.PublishedProviders;
            string templateVersion = generatePublishedFundingInput.TemplateVersion;
            FundingPeriod fundingPeriod = generatePublishedFundingInput.FundingPeriod;

            FundingValueAggregator fundingValueAggregator = new FundingValueAggregator();

            foreach (var organisationGroup in organisationGroupsToSave)
            {
                // TODO: extract interface
                IEnumerable<string> providerIds = organisationGroup.OrganisationGroupResult.Providers.Select(p => p.ProviderId);
                IEnumerable<string> publishedProvidersIds = publishedProviders.Select(p => p.Current.ProviderId);

                List<PublishedProvider> publishedProvidersForOrganisationGroup = new List<PublishedProvider>(publishedProviders.Where(p => providerIds.Contains(p.Current.ProviderId)));
                List<PublishedProviderVersion> publishedProviderVersionsForOrganisationGroup = new List<PublishedProviderVersion>(publishedProvidersForOrganisationGroup.Select(p => p.Current));

                IEnumerable<string> missingProviders = providerIds.Except(publishedProvidersIds);

                if (missingProviders.AnyWithNullCheck())
                {
                    string providerIdsString = string.Join(", ", missingProviders);
                    throw new Exception($"Missing PublishedProvider result for organisation group '{organisationGroup.OrganisationGroupResult.GroupReason}' '{organisationGroup.OrganisationGroupResult.GroupTypeCode}' '{organisationGroup.OrganisationGroupResult.GroupTypeIdentifier}' '{organisationGroup.OrganisationGroupResult.IdentifierValue}'. Provider IDs={providerIdsString}");
                }

                IEnumerable<AggregateFundingLine> fundingLineAggregates = fundingValueAggregator.GetTotals(templateMetadataContents, publishedProviderVersionsForOrganisationGroup);

                IEnumerable<Common.TemplateMetadata.Models.FundingLine> fundingLineDefinitons = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines) ?? Enumerable.Empty<Common.TemplateMetadata.Models.FundingLine>();

                List<PublishingModels.FundingLine> fundingLines = GenerateFundingLines(fundingLineAggregates, fundingLineDefinitons);
                List<PublishingModels.FundingCalculation> calculations = GenerateCalculations(fundingLineAggregates.SelectMany(c => c.Calculations));

                // IEnumerable<Common.TemplateMetadata.Models.Calculation> calculationDefinitions = fundingLineDefinitons.SelectMany(_ => _.Calculations.Flatten(calculation => calculation.Calculations)) ?? new Calculation[0];
                //IEnumerable<TemplateModels.ReferenceData> refernceData = calculations.Where(_ => _.ReferenceData != null)?.SelectMany(_ => _.ReferenceData) ?? new ReferenceData[0];

                decimal totalFunding = publishedProviderVersionsForOrganisationGroup.Sum(c => c.TotalFunding);

                PublishingModels.PublishedFundingVersion publishedFundingVersion = new PublishingModels.PublishedFundingVersion
                {
                    FundingStreamId = generatePublishedFundingInput.FundingStream.Id,
                    FundingStreamName = generatePublishedFundingInput.FundingStream.Name,
                    TotalFunding = totalFunding,
                    FundingPeriod = new PublishingModels.PublishedFundingPeriod
                    {
                        Type = Enum.Parse<PublishingModels.PublishedFundingPeriodType>(fundingPeriod.Type.ToString()),
                        Period = fundingPeriod.Period,
                        EndDate = fundingPeriod.EndDate,
                        StartDate = fundingPeriod.StartDate,
                        Name = fundingPeriod.Name,
                    },
                    SpecificationId = generatePublishedFundingInput.SpecificationId,
                    OrganisationGroupTypeCode = organisationGroup.OrganisationGroupResult.GroupTypeCode.ToString(),
                    OrganisationGroupTypeIdentifier = organisationGroup.OrganisationGroupResult.GroupTypeIdentifier.ToString(),
                    OrganisationGroupIdentifierValue = organisationGroup.OrganisationGroupResult.IdentifierValue,
                    OrganisationGroupTypeClassification = organisationGroup.OrganisationGroupResult.GroupTypeClassification.ToString(),
                    OrganisationGroupName = organisationGroup.OrganisationGroupResult.Name,
                    OrganisationGroupSearchableName = organisationGroup.OrganisationGroupResult.SearchableName,
                    OrganisationGroupIdentifiers = _mapper.Map<IEnumerable<PublishingModels.PublishedOrganisationGroupTypeIdentifier>>(organisationGroup.OrganisationGroupResult.Identifiers),
                    FundingLines = fundingLines,
                    Calculations = calculations,
                    SchemaVersion = templateMetadataContents.SchemaVersion,
                    Status = PublishingModels.PublishedFundingStatus.Approved,
                    GroupingReason = organisationGroup.OrganisationGroupResult.GroupReason.AsMatchingEnum<PublishingModels.GroupingReason>(),
                    ProviderFundings = publishedProviderVersionsForOrganisationGroup.Select(_ => _.FundingId),
                    TemplateVersion = templateVersion,
                    StatusChangedDate = generatePublishedFundingInput.PublishingDates.StatusChangedDate,
                    EarliestPaymentAvailableDate = generatePublishedFundingInput.PublishingDates.EarliestPaymentAvailableDate,
                    ExternalPublicationDate = generatePublishedFundingInput.PublishingDates.ExternalPublicationDate,
                };

                publishedFundingVersion.FundingId = _publishedFundingIdGeneratorResolver.GetService(templateMetadataContents.SchemaVersion).GetFundingId(publishedFundingVersion);

                PublishedFunding publishedFundingResult = organisationGroup.PublishedFunding;

                if (publishedFundingResult == null)
                {
                    publishedFundingResult = new PublishingModels.PublishedFunding()
                    {
                        Current = publishedFundingVersion,
                    };
                }

                yield return (publishedFundingResult, publishedFundingVersion);
            }
        }

        private List<FundingCalculation> GenerateCalculations(IEnumerable<AggregateFundingCalculation> aggregateCalculations)
        {
            List<PublishingModels.FundingCalculation> calculations = new List<PublishingModels.FundingCalculation>();
            foreach (AggregateFundingCalculation aggregateFundingCalculation in aggregateCalculations)
            {
                calculations.Add(new FundingCalculation()
                {
                    TemplateCalculationId = aggregateFundingCalculation.TemplateCalculationId,
                    Value = aggregateFundingCalculation.Value,
                });

                if (aggregateFundingCalculation.Calculations.AnyWithNullCheck())
                {
                    calculations.AddRange(GenerateCalculations(aggregateFundingCalculation.Calculations));
                }
            }

            return calculations;
        }

        private List<PublishingModels.FundingLine> GenerateFundingLines(IEnumerable<AggregateFundingLine> fundingLineAggregates, IEnumerable<Common.TemplateMetadata.Models.FundingLine> fundingLineDefinitons)
        {
            List<PublishingModels.FundingLine> fundingLines = new List<PublishingModels.FundingLine>();

            foreach (AggregateFundingLine aggregateFundingLine in fundingLineAggregates)
            {
                Common.TemplateMetadata.Models.FundingLine fundingLineDefinition = fundingLineDefinitons.FirstOrDefault(c => c.TemplateLineId == aggregateFundingLine.TemplateLineId);
                if (fundingLineDefinition == null)
                {
                    throw new InvalidOperationException($"Unable to find funding line with TemplateLineId '{aggregateFundingLine.TemplateLineId}'");
                }

                PublishingModels.FundingLine fundingline = new PublishingModels.FundingLine()
                {
                    FundingLineCode = fundingLineDefinition.FundingLineCode,
                    Name = fundingLineDefinition.Name,
                    TemplateLineId = fundingLineDefinition.TemplateLineId,
                    Type = fundingLineDefinition.Type.AsMatchingEnum<PublishingModels.OrganisationGroupingReason>(),
                    Value = aggregateFundingLine.Value ?? 0,
                };

                fundingLines.Add(fundingline);

                fundingline.DistributionPeriods = aggregateFundingLine.DistributionPeriods.ToList();

                if (aggregateFundingLine.FundingLines.AnyWithNullCheck())
                {
                    fundingLines.AddRange(GenerateFundingLines(aggregateFundingLine.FundingLines, fundingLineDefinitons));
                }
            }

            return fundingLines;
        }
    }
}