using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Newtonsoft.Json;
using AggregationType = CalculateFunding.Common.TemplateMetadata.Enums.AggregationType;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Generators.Schema12
{
    public class PublishedFundingContentsGenerator : IPublishedFundingContentsGenerator
    {
        private class SchemaJson
        {
            [JsonProperty("$schema")] public string Schema { get; set; }

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
        }

        private class SchemaJsonCalculation
        {
            public string Name { get; set; }

            public string Type { get; set; }

            public string AggregationType { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public SchemaJsonGroupRate GroupRate { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public SchemaJsonPercentageChangeBetweenAandB PercentageChangeBetweenAandB { get; set; }

            public string FormulaText { get; set; }

            public uint TemplateCalculationId { get; set; }

            public object Value { get; set; }

            public string ValueFormat { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public IEnumerable<string> AllowedEnumTypeValues { get; set; }
        }

        public class SchemaJsonPercentageChangeBetweenAandB
        {
            public uint CalculationA { get; set; }

            public uint CalculationB { get; set; }

            public string CalculationAggregationType { get; set; }
        }

        public class SchemaJsonGroupRate
        {
            public uint Numerator { get; set; }

            public uint Denominator { get; set; }
        }

        public string GenerateContents(PublishedFundingVersion publishedFundingVersion,
            TemplateMetadataContents templateMetadataContents)
        {
            Guard.ArgumentNotNull(publishedFundingVersion, nameof(publishedFundingVersion));
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));

            IEnumerable<TemplateFundingLine> fundingLines = templateMetadataContents.RootFundingLines?.Flatten(x => x.FundingLines);
            IEnumerable<Calculation> calculations = fundingLines?.SelectMany(fl => fl.Calculations.Flatten(cal => cal.Calculations));

            SchemaJson contents = new SchemaJson
            {
                Schema = "https://fundingschemas.blob.core.windows.net/schemas/funding-schema-1.2.json#schema",
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
                        GroupTypeClassification = publishedFundingVersion.OrganisationGroupTypeClassification,
                        Name = publishedFundingVersion.OrganisationGroupName,
                        SearchableName = publishedFundingVersion.OrganisationGroupSearchableName,
                        GroupTypeIdentifier = new
                        {
                            Type = publishedFundingVersion.OrganisationGroupTypeIdentifier,
                            Value = publishedFundingVersion.OrganisationGroupIdentifierValue
                        },
                        Identifiers = publishedFundingVersion.OrganisationGroupIdentifiers?.Select(groupIdentifier => new
                        {
                            groupIdentifier.Type,
                            groupIdentifier.Value
                        }).ToArray()
                    },
                    FundingValue = new
                    {
                        TotalValue = publishedFundingVersion.TotalFunding,
                        FundingLines = fundingLines?.DistinctBy(x => x.TemplateLineId).OrderBy(_ => _.TemplateLineId).Select(rootFundingLine =>
                              BuildSchemaJsonFundingLines(publishedFundingVersion.FundingLines,
                                  rootFundingLine)),
                        Calculations = calculations?.DistinctBy(_ => _.TemplateCalculationId).OrderBy(_ => _.TemplateCalculationId).Select(calculation =>
                                BuildSchemaJsonCalculations(publishedFundingVersion.Calculations,
                                    calculation,
                                    publishedFundingVersion.OrganisationGroupTypeIdentifier,
                                    publishedFundingVersion.OrganisationGroupIdentifierValue)).Where(_ => _ != null)
                    },
                    ProviderFundings = publishedFundingVersion.ProviderFundings.ToArray(),
                    publishedFundingVersion.GroupingReason,
                    publishedFundingVersion.StatusChangedDate,
                    publishedFundingVersion.ExternalPublicationDate,
                    publishedFundingVersion.EarliestPaymentAvailableDate
                }
            };

            return contents.AsJson();
        }

        private SchemaJsonFundingLine BuildSchemaJsonFundingLines(IEnumerable<FundingLine> fundingLines,
            TemplateFundingLine templateFundingLine)
        {
            FundingLine publishedFundingLine = fundingLines.FirstOrDefault(_ => _.TemplateLineId == templateFundingLine.TemplateLineId);

            return new SchemaJsonFundingLine
            {
                Name = templateFundingLine.Name,
                FundingLineCode = templateFundingLine.FundingLineCode,
                Value = publishedFundingLine.Value.DecimalAsObject(),
                TemplateLineId = templateFundingLine.TemplateLineId,
                Type = templateFundingLine.Type.ToString(),
                DistributionPeriods = publishedFundingLine.DistributionPeriods?.Where(_ => _ != null).Select(distributionPeriod => new
                {
                    Value = distributionPeriod.Value.DecimalAsObject(),
                    distributionPeriod.DistributionPeriodId,
                    ProfilePeriods = distributionPeriod.ProfilePeriods?.Where(_ => _ != null).Select(profilePeriod => new
                    {
                        Type = profilePeriod.Type.ToString(),
                        profilePeriod.TypeValue,
                        profilePeriod.Year,
                        profilePeriod.Occurrence,
                        ProfiledValue = profilePeriod.ProfiledValue.DecimalAsObject(),
                        profilePeriod.DistributionPeriodId
                    }).ToArray()
                }).ToArray() ?? new dynamic[0]
            };
        }

        private static SchemaJsonCalculation BuildSchemaJsonCalculations(IEnumerable<FundingCalculation> fundingCalculations,
            Calculation templateCalculation,
            string organisationGroupTypeIdentifier,
            string organisationGroupIdentifierValue)
        {
            FundingCalculation publishedFundingCalculation = fundingCalculations?.Where(_ =>
                    _.TemplateCalculationId == templateCalculation.TemplateCalculationId)
                .FirstOrDefault();

            if (publishedFundingCalculation == null)
            {
                return null;
            }

            ICalculationMapper mapper = templateCalculation.AggregationType switch
            {
                AggregationType.PercentageChangeBetweenAandB => new PercentageChangeBetweenAandBCalculation(),
                AggregationType.GroupRate => new GroupRateCalculation(),
                _ => new BasicCalculation()
            };

            return mapper.ToSchemaJsonCalculation(templateCalculation,
                publishedFundingCalculation,
                fundingCalculations,
                organisationGroupTypeIdentifier,
                organisationGroupIdentifierValue);
        }

        private interface ICalculationMapper
        {
            SchemaJsonCalculation ToSchemaJsonCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string organisationGroupTypeIdentifier,
                string organisationGroupIdentifierValue);
        }

        private class PercentageChangeBetweenAandBCalculation : BasicCalculation
        {
            public override SchemaJsonCalculation ToSchemaJsonCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string organisationGroupTypeIdentifier,
                string organisationGroupIdentifierValue)
            {
                SchemaJsonCalculation schemaJsonCalculation = base.ToSchemaJsonCalculation(templateCalculation,
                    publishedFundingCalculation,
                    publishedFundingCalculations,
                    organisationGroupTypeIdentifier,
                    organisationGroupIdentifierValue);

                PercentageChangeBetweenAandB templateCalculationPercentageChangeBetweenAandB = templateCalculation.PercentageChangeBetweenAandB;

                schemaJsonCalculation.PercentageChangeBetweenAandB = new SchemaJsonPercentageChangeBetweenAandB
                {
                    CalculationA = templateCalculationPercentageChangeBetweenAandB.CalculationA,
                    CalculationB = templateCalculationPercentageChangeBetweenAandB.CalculationB,
                    CalculationAggregationType = templateCalculationPercentageChangeBetweenAandB.CalculationAggregationType.ToString()
                };

                return schemaJsonCalculation;
            }
        }

        private class GroupRateCalculation : BasicCalculation
        {
            public override SchemaJsonCalculation ToSchemaJsonCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string organisationGroupTypeIdentifier,
                string organisationGroupIdentifierValue)
            {
                SchemaJsonCalculation schemaJsonCalculation = base.ToSchemaJsonCalculation(templateCalculation,
                    publishedFundingCalculation,
                    publishedFundingCalculations,
                    organisationGroupTypeIdentifier,
                    organisationGroupIdentifierValue);

                GroupRate templateCalculationGroupRate = templateCalculation.GroupRate;

                schemaJsonCalculation.GroupRate = new SchemaJsonGroupRate
                {
                    Denominator = templateCalculationGroupRate.Denominator,
                    Numerator = templateCalculationGroupRate.Numerator
                };

                return schemaJsonCalculation;
            }
        }

        private class BasicCalculation : ICalculationMapper
        {
            public virtual SchemaJsonCalculation ToSchemaJsonCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string organisationGroupTypeIdentifier,
                string organisationGroupIdentifierValue) =>
                new SchemaJsonCalculation
                {
                    Name = templateCalculation.Name,
                    Type = templateCalculation.Type.ToString(),
                    AggregationType = templateCalculation.AggregationType.ToString(),
                    FormulaText = templateCalculation.FormulaText,
                    Value = publishedFundingCalculation.Value,
                    TemplateCalculationId = templateCalculation.TemplateCalculationId,
                    ValueFormat = templateCalculation.ValueFormat.ToString(),
                    AllowedEnumTypeValues = templateCalculation.AllowedEnumTypeValues?.Any() == true ? templateCalculation.AllowedEnumTypeValues : null
                };
        }

        private static bool IsAggregationOrHasChildCalculations(Calculation fundingCalculation) =>
            fundingCalculation.AggregationType != AggregationType.None
            || fundingCalculation.Calculations?.Any() == true;
    }
}