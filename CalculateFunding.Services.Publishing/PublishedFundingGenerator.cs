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
using CalculateFunding.Services.Publishing.Models;
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
        /// <param name="publishedFundingInput"></param>
        /// <param name="publishedProviders"></param>
        /// <returns></returns>
        public IEnumerable<(PublishedFunding, PublishedFundingVersion)> GeneratePublishedFunding(PublishedFundingInput publishedFundingInput,
            IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(publishedFundingInput, nameof(publishedFundingInput));
            Guard.ArgumentNotNull(publishedFundingInput.FundingPeriod, nameof(publishedFundingInput.FundingPeriod));
            Guard.ArgumentNotNull(publishedFundingInput.FundingStream, nameof(publishedFundingInput.FundingStream));
            Guard.ArgumentNotNull(publishedFundingInput.OrganisationGroupsToSave, nameof(publishedFundingInput.OrganisationGroupsToSave));
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));
            Guard.ArgumentNotNull(publishedFundingInput.PublishingDates, nameof(publishedFundingInput.PublishingDates));
            Guard.ArgumentNotNull(publishedFundingInput.TemplateMetadataContents, nameof(publishedFundingInput.TemplateMetadataContents));
            Guard.IsNullOrWhiteSpace(publishedFundingInput.TemplateVersion, nameof(publishedFundingInput.TemplateVersion));
            Guard.IsNullOrWhiteSpace(publishedFundingInput.SpecificationId, nameof(publishedFundingInput.SpecificationId));

            IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> organisationGroupsToSave = publishedFundingInput.OrganisationGroupsToSave;

            TemplateMetadataContents templateMetadataContents = publishedFundingInput.TemplateMetadataContents;
            string templateVersion = publishedFundingInput.TemplateVersion;
            FundingPeriod fundingPeriod = publishedFundingInput.FundingPeriod;

            FundingValueAggregator fundingValueAggregator = new FundingValueAggregator();

            foreach ((PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult) organisationGroup in organisationGroupsToSave)
            {
                // TODO: extract interface
                IEnumerable<string> providerIds = organisationGroup.OrganisationGroupResult.Providers.Select(p => p.ProviderId);
                IEnumerable<string> publishedProvidersIds = publishedProviders.Select(p => p.Current.ProviderId);

                List<PublishedProvider> publishedProvidersForOrganisationGroup = new List<PublishedProvider>(publishedProviders.Where(p
                    => providerIds.Contains(p.Current.ProviderId)));
                List<PublishedProviderVersion> publishedProviderVersionsForOrganisationGroup = new List<PublishedProviderVersion>(
                    publishedProvidersForOrganisationGroup.Select(p => p.Current));

                IEnumerable<string> missingProviders = providerIds.Except(publishedProvidersIds);

                if (missingProviders.AnyWithNullCheck())
                {
                    string providerIdsString = string.Join(", ", missingProviders);
                    throw new Exception($"Missing PublishedProvider result for organisation group '{organisationGroup.OrganisationGroupResult.GroupReason}' '{organisationGroup.OrganisationGroupResult.GroupTypeCode}' '{organisationGroup.OrganisationGroupResult.GroupTypeIdentifier}' '{organisationGroup.OrganisationGroupResult.IdentifierValue}'. Provider IDs={providerIdsString}");
                }

                List<AggregateFundingLine> fundingLineAggregates = new List<AggregateFundingLine>(
                    fundingValueAggregator.GetTotals(templateMetadataContents, publishedProviderVersionsForOrganisationGroup));

                IEnumerable<Common.TemplateMetadata.Models.FundingLine> fundingLineDefinitions = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines) ??
                                                                                                 Enumerable.Empty<Common.TemplateMetadata.Models.FundingLine>();

                // Add in calculations in numerator/demoninator and percentagechange targets

                List<PublishingModels.FundingLine> fundingLines = GenerateFundingLines(fundingLineAggregates, fundingLineDefinitions);
                List<FundingCalculation> calculations = GenerateCalculations(fundingLineAggregates.Flatten(_ => _.FundingLines)
                    .SelectMany(c => c.Calculations ?? Enumerable.Empty<AggregateFundingCalculation>()));

                decimal? totalFunding = publishedProviderVersionsForOrganisationGroup.Sum(_ => _.TotalFunding);

                PublishedFundingVersion publishedFundingVersion = new PublishedFundingVersion
                {
                    FundingStreamId = publishedFundingInput.FundingStream.Id,
                    FundingStreamName = publishedFundingInput.FundingStream.Name,
                    TotalFunding = totalFunding,
                    FundingPeriod = new PublishedFundingPeriod
                    {
                        Type = Enum.Parse<PublishedFundingPeriodType>(fundingPeriod.Type.GetValueOrDefault().ToString()),
                        Period = fundingPeriod.Period,
                        EndDate = fundingPeriod.EndDate,
                        StartDate = fundingPeriod.StartDate,
                        Name = fundingPeriod.Name,
                    },
                    SpecificationId = publishedFundingInput.SpecificationId,
                    OrganisationGroupTypeCode = organisationGroup.OrganisationGroupResult.GroupTypeCode.ToString(),
                    OrganisationGroupTypeIdentifier = organisationGroup.OrganisationGroupResult.GroupTypeIdentifier.ToString(),
                    OrganisationGroupIdentifierValue = organisationGroup.OrganisationGroupResult.IdentifierValue,
                    OrganisationGroupTypeClassification = organisationGroup.OrganisationGroupResult.GroupTypeClassification.ToString(),
                    OrganisationGroupName = organisationGroup.OrganisationGroupResult.Name,
                    OrganisationGroupSearchableName = organisationGroup.OrganisationGroupResult.SearchableName,
                    OrganisationGroupIdentifiers = _mapper.Map<IEnumerable<PublishedOrganisationGroupTypeIdentifier>>(organisationGroup.OrganisationGroupResult.Identifiers),
                    FundingLines = fundingLines,
                    Calculations = calculations,
                    SchemaVersion = templateMetadataContents.SchemaVersion,
                    Status = PublishedFundingStatus.Approved,
                    GroupingReason = organisationGroup.OrganisationGroupResult.GroupReason.AsMatchingEnum<PublishingModels.GroupingReason>(),
                    ProviderFundings = publishedProviderVersionsForOrganisationGroup.Select(_ => _.FundingId),
                    TemplateVersion = templateVersion,
                    StatusChangedDate = publishedFundingInput.PublishingDates.StatusChangedDate.TrimToTheSecond(),
                    EarliestPaymentAvailableDate = publishedFundingInput.PublishingDates.EarliestPaymentAvailableDate.TrimToTheMinute(),
                    ExternalPublicationDate = publishedFundingInput.PublishingDates.ExternalPublicationDate.TrimToTheMinute(),
                };

                publishedFundingVersion.FundingId = _publishedFundingIdGeneratorResolver.GetService(templateMetadataContents.SchemaVersion).GetFundingId(publishedFundingVersion);

                PublishedFunding publishedFundingResult = organisationGroup.PublishedFunding;

                if (publishedFundingResult == null)
                {
                    publishedFundingResult = new PublishedFunding()
                    {
                        Current = publishedFundingVersion,
                    };
                }

                yield return (publishedFundingResult, publishedFundingVersion);
            }
        }

        private List<FundingCalculation> GenerateCalculations(IEnumerable<AggregateFundingCalculation> aggregateCalculations)
        {
            List<FundingCalculation> calculations = new List<FundingCalculation>();

            foreach (AggregateFundingCalculation aggregateFundingCalculation in aggregateCalculations.Where(_ => _.Value != null))
            {
                calculations.Add(new FundingCalculation()
                {
                    TemplateCalculationId = aggregateFundingCalculation.TemplateCalculationId,
                    Value = aggregateFundingCalculation.Value,
                });

                if (aggregateFundingCalculation.Calculations != null && aggregateFundingCalculation.Calculations.AnyWithNullCheck())
                {
                    calculations.AddRange(GenerateCalculations(aggregateFundingCalculation.Calculations));
                }
            }

            return calculations;
        }

        private List<PublishingModels.FundingLine> GenerateFundingLines(IEnumerable<AggregateFundingLine> fundingLineAggregates,
            IEnumerable<Common.TemplateMetadata.Models.FundingLine> fundingLineDefinitions)
        {
            List<PublishingModels.FundingLine> fundingLines = new List<PublishingModels.FundingLine>();

            foreach (AggregateFundingLine aggregateFundingLine in fundingLineAggregates)
            {
                if (aggregateFundingLine == null)
                {
                    throw new InvalidOperationException("Null aggregate funding line");
                }

                Common.TemplateMetadata.Models.FundingLine fundingLineDefinition = fundingLineDefinitions.FirstOrDefault(_
                    => _.TemplateLineId == aggregateFundingLine.TemplateLineId);
                if (fundingLineDefinition == null)
                {
                    throw new InvalidOperationException($"Unable to find funding line with TemplateLineId '{aggregateFundingLine.TemplateLineId}'");
                }

                PublishingModels.FundingLine fundingLine = new PublishingModels.FundingLine()
                {
                    FundingLineCode = fundingLineDefinition.FundingLineCode,
                    Name = fundingLineDefinition.Name,
                    TemplateLineId = fundingLineDefinition.TemplateLineId,
                    Type = fundingLineDefinition.Type.AsMatchingEnum<FundingLineType>(),
                    Value = aggregateFundingLine.Value,
                };

                fundingLines.Add(fundingLine);

                fundingLine.DistributionPeriods = aggregateFundingLine.DistributionPeriods?.ToList();

                if (aggregateFundingLine.FundingLines.AnyWithNullCheck())
                {
                    fundingLines.AddRange(GenerateFundingLines(aggregateFundingLine.FundingLines, fundingLineDefinitions));
                }
            }

            return fundingLines;
        }
    }
}