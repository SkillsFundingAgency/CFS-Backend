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
        }

        public string GenerateContents(PublishedFundingVersion publishedFundingVersion,
            TemplateMetadataContents templateMetadataContents)
        {
            Guard.ArgumentNotNull(publishedFundingVersion, nameof(publishedFundingVersion));
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));

            Dictionary<uint, Calculation> templateCalculations =
                templateMetadataContents.RootFundingLines
                    .Flatten(_ => _.FundingLines)
                    .SelectMany(_ => _.Calculations)
                    .Flatten(_ => _.Calculations)
                    .ToDictionary(_ => _.TemplateCalculationId, _ => _);

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
                        GroupTypeCategory = publishedFundingVersion.OrganisationGroupTypeCategory,
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
                        FundingLines = BuildSchemaJsonFundingLines(publishedFundingVersion.FundingLines, templateCalculations)
                    },
                    ProviderFundings = publishedFundingVersion.ProviderFundings?.ToArray(),
                    publishedFundingVersion.GroupingReason,
                    publishedFundingVersion.StatusChangedDate,
                    publishedFundingVersion.ExternalPublicationDate,
                    publishedFundingVersion.EarliestPaymentAvailableDate
                }
            };

            return contents.AsJson();
        }

        private dynamic BuildSchemaJsonFundingLines(IEnumerable<FundingLine> fundingLines,
            Dictionary<uint, Calculation> templateCalculations)
        {
            return fundingLines?.Select(fundingLine => new SchemaJsonFundingLine
            {
                Name = fundingLine.Name,
                FundingLineCode = fundingLine.FundingLineCode,
                Value = DecimalAsObject(fundingLine.Value),
                TemplateLineId = fundingLine.TemplateLineId,
                Type = fundingLine.Type.ToString(),
                Calculations = BuildSchemaJsonCalculations(fundingLine.Calculations, templateCalculations),
                DistributionPeriods = fundingLine.DistributionPeriods?.Select(distributionPeriod => new
                {
                    Value = Convert.ToInt32(distributionPeriod.Value),
                    distributionPeriod.DistributionPeriodId,
                    ProfilePeriods = distributionPeriod.ProfilePeriods?.Select(profilePeriod => new
                    {
                        Type = profilePeriod.Type.ToString(),
                        profilePeriod.TypeValue,
                        profilePeriod.Year,
                        profilePeriod.Occurrence,
                        ProfiledValue = DecimalAsObject(profilePeriod.ProfiledValue),
                        profilePeriod.DistributionPeriodId
                    }).ToArray()
                }).ToArray() ?? new dynamic[0],
                FundingLines = BuildSchemaJsonFundingLines(fundingLine.FundingLines, templateCalculations)
            }).ToArray();
        }

        private IEnumerable<SchemaJsonCalculation> BuildSchemaJsonCalculations(IEnumerable<FundingCalculation> fundingCalculations,
            Dictionary<uint, Calculation> templateCalculations)
        {
            return fundingCalculations?.Where(IsAggregationOrHasChildCalculations)
                .Select(calculation =>
                {
                    Calculation templateCalculation = templateCalculations[calculation.TemplateCalculationId];

                    return new SchemaJsonCalculation
                    {
                        Name = templateCalculation.Name,
                        Type = calculation.Type.ToString(),
                        AggregationType = calculation.AggregationType.ToString(),
                        FormulaText = templateCalculation.FormulaText,
                        Value = calculation.Value,
                        TemplateCalculationId = calculation.TemplateCalculationId,
                        ValueFormat = templateCalculation.ValueFormat.ToString(),
                        Calculations = BuildSchemaJsonCalculations(calculation.Calculations, templateCalculations)
                    };
                }).ToArray();
        }

        private static bool IsAggregationOrHasChildCalculations(FundingCalculation fundingCalculation)
        {
            return fundingCalculation.AggregationType != AggregationType.None
                   || fundingCalculation.Calculations?.Any() == true;
        }

        private object DecimalAsObject(decimal value)
        {
            bool isWholeNumber = value % 1M == 0M;
            
            object decimalAsObject = isWholeNumber ? Convert.ToInt32(value) : (object)value;
            
            return decimalAsObject;
        }
    }
}