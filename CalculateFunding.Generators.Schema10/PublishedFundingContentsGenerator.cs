using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Newtonsoft.Json;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;

namespace CalculateFunding.Generators.Schema10
{
    public class PublishedFundingContentsGenerator : IPublishedFundingContentsGenerator
    {
        private class SchemaJson
        {
            [JsonProperty("$schema")]
            public string Schema { get; set; }

            public string SchemaVersion { get; set; }
            public object Funding { get; set; }
        }

        private class SchemaJsonFundingLine
        {
            public string Name { get; set; }

            public string FundingLineCode { get; set; }

            public object Value { get; set; }

            public uint TemplateLineId { get; set; }

            public string Type { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public IEnumerable<SchemaJsonCalculation> Calculations { get; set; }

            public IEnumerable<dynamic> DistributionPeriods { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public IEnumerable<SchemaJsonFundingLine> FundingLines { get; set; }
        }

        private class SchemaJsonCalculation
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string AggregationType { get; set; }
            public string FormulaText { get; set; }
            public uint TemplateCalculationId { get; set; }
            public object Value { get; set; }
            public string ValueFormat { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public IEnumerable<SchemaJsonCalculation> Calculations { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public IEnumerable<dynamic> ReferenceData { get; set; }
        }

        public string GenerateContents(PublishedFundingVersion publishedFundingVersion,
            TemplateMetadataContents templateMetadataContents)
        {
            Guard.ArgumentNotNull(publishedFundingVersion, nameof(publishedFundingVersion));
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));

            SchemaJson contents = new SchemaJson
            {
                Schema = "https://fundingschemas.blob.core.windows.net/schemas/logicalmodel-1.0.json#schema",
                SchemaVersion = publishedFundingVersion.SchemaVersion,
                Funding = new
                {
                    publishedFundingVersion.TemplateVersion,
                    Id = publishedFundingVersion.FundingId,
                    FundingVersion = $"{publishedFundingVersion.MajorVersion}_{publishedFundingVersion.MinorVersion}",
                    Status = publishedFundingVersion.Status.ToString(),
                    FundingStream = new
                    {
                        Code = publishedFundingVersion.FundingStreamId,
                        Name = publishedFundingVersion.FundingStreamName
                    },
                    FundingPeriod = new
                    {
                        publishedFundingVersion.FundingPeriod.Id,
                        publishedFundingVersion.FundingPeriod.Period,
                        publishedFundingVersion.FundingPeriod.Name,
                        Type = publishedFundingVersion.FundingPeriod.Type.ToString(),
                        publishedFundingVersion.FundingPeriod.StartDate,
                        publishedFundingVersion.FundingPeriod.EndDate
                    },
                    OrganisationGroup = new
                    {
                        GroupTypeCode = publishedFundingVersion.OrganisationGroupTypeCode,
                        GroupTypeIdentifier = publishedFundingVersion.OrganisationGroupTypeIdentifier,
                        IdentifierValue = publishedFundingVersion.OrganisationGroupIdentifierValue,
                        GroupTypeClassification = publishedFundingVersion.OrganisationGroupTypeClassification,
                        Name = publishedFundingVersion.OrganisationGroupName,
                        SearchableName = publishedFundingVersion.OrganisationGroupSearchableName,
                        Identifiers = publishedFundingVersion.OrganisationGroupIdentifiers?.Select(groupTypeIdentifier => new
                        {
                            groupTypeIdentifier.Type,
                            groupTypeIdentifier.Value
                        }).ToArray()
                    },
                    FundingValue = new
                    {
                        TotalValue = publishedFundingVersion.TotalFunding,
                        FundingLines = templateMetadataContents.RootFundingLines?.Select(_ => BuildSchemaJsonFundingLines(publishedFundingVersion.ReferenceData, publishedFundingVersion.Calculations, publishedFundingVersion.FundingLines, _))
                    },
                    ProviderFundings = publishedFundingVersion.ProviderFundings?.ToArray(),
                    GroupingReason = publishedFundingVersion.GroupingReason,
                    StatusChangedDate = publishedFundingVersion.StatusChangedDate,
                    ExternalPublicationDate = publishedFundingVersion.ExternalPublicationDate,
                    EarliestPaymentAvailableDate = publishedFundingVersion.EarliestPaymentAvailableDate
                }
            };

            return contents.AsJson();
        }

        private SchemaJsonFundingLine BuildSchemaJsonFundingLines(IEnumerable<FundingReferenceData> referenceData, IEnumerable<FundingCalculation> fundingCalculations, IEnumerable<FundingLine> fundingLines,
            Common.TemplateMetadata.Models.FundingLine templateFundingLine)
        {
            FundingLine publishedFundingLine = fundingLines.Where(_ => _.TemplateLineId == templateFundingLine.TemplateLineId).FirstOrDefault();

            return new SchemaJsonFundingLine
            {
                Name = templateFundingLine.Name,
                FundingLineCode = templateFundingLine.FundingLineCode,
                Value = DecimalAsObject(publishedFundingLine.Value),
                TemplateLineId = templateFundingLine.TemplateLineId,
                Type = templateFundingLine.Type.ToString(),
                Calculations = templateFundingLine.Calculations?.Where(IsAggregationOrHasChildCalculations)?.Select(_ => BuildSchemaJsonCalculations(referenceData, fundingCalculations, _)),
                DistributionPeriods = publishedFundingLine.DistributionPeriods?.Where(_ => _ != null).Select(distributionPeriod => new
                {
                    Value = Convert.ToInt32(distributionPeriod.Value),
                    distributionPeriod.DistributionPeriodId,
                    ProfilePeriods = distributionPeriod.ProfilePeriods?.Where(_ => _ != null).Select(profilePeriod => new
                    {
                        Type = profilePeriod.Type.ToString(),
                        profilePeriod.TypeValue,
                        profilePeriod.Year,
                        profilePeriod.Occurrence,
                        ProfiledValue = DecimalAsObject(profilePeriod.ProfiledValue),
                        profilePeriod.DistributionPeriodId
                    }).ToArray()
                }).ToArray() ?? new dynamic[0],
                FundingLines = templateFundingLine.FundingLines?.Select(_ => BuildSchemaJsonFundingLines(referenceData, fundingCalculations, fundingLines, _))
            };
        }

        private SchemaJsonCalculation BuildSchemaJsonCalculations(IEnumerable<FundingReferenceData> referenceData, IEnumerable<FundingCalculation> fundingCalculations, Common.TemplateMetadata.Models.Calculation calculation)
        {
            FundingCalculation publishedFundingCalculation = fundingCalculations?.Where(_ => _.TemplateCalculationId == calculation.TemplateCalculationId)?.First();
            IEnumerable<FundingReferenceData> publishedFundingReferenceData = referenceData?.Where(_ => calculation.ReferenceData?.Any(calcReferenceData => calcReferenceData.TemplateReferenceId == _.TemplateReferenceId) ?? false);
            IEnumerable<dynamic> refernceData = publishedFundingReferenceData?.Select(_ => ToReferenceData(calculation.ReferenceData.Where(calcReferenceData => calcReferenceData.TemplateReferenceId == _.TemplateReferenceId).Single(), _));

            return new SchemaJsonCalculation
            {
                Name = calculation.Name,
                Type = calculation.Type.ToString(),
                AggregationType = calculation.AggregationType.ToString(),
                FormulaText = calculation.FormulaText,
                Value = publishedFundingCalculation.Value,
                TemplateCalculationId = calculation.TemplateCalculationId,
                ValueFormat = calculation.ValueFormat.ToString(),
                Calculations = calculation.Calculations?.Where(IsAggregationOrHasChildCalculations)?.Select(_ => BuildSchemaJsonCalculations(referenceData, fundingCalculations, _)),
                ReferenceData = !refernceData.IsNullOrEmpty() ? refernceData : null
            };
        }

        private dynamic ToReferenceData(ReferenceData referenceData, FundingReferenceData fundingReferenceData)
        {
            return new
            {
                referenceData.Name,
                referenceData.TemplateReferenceId,
                Format = referenceData.Format.ToString(),
                fundingReferenceData.Value,
                AggregationType = referenceData.AggregationType.ToString()
            };
        }

        private static bool IsAggregationOrHasChildCalculations(Common.TemplateMetadata.Models.Calculation fundingCalculation)
        {
            return fundingCalculation.AggregationType != Common.TemplateMetadata.Enums.AggregationType.None
                   || fundingCalculation.Calculations?.Any() == true;
        }

        private object DecimalAsObject(decimal? value)
        {
            bool isWholeNumber = value % 1M == 0M;

            object decimalAsObject = isWholeNumber ? Convert.ToInt32(value) : (object)value;

            return decimalAsObject;
        }
    }
}