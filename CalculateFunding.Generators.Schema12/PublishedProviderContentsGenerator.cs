using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static CalculateFunding.Models.Publishing.AggregationType;
using AggregationType = CalculateFunding.Common.TemplateMetadata.Enums.AggregationType;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;


namespace CalculateFunding.Generators.Schema12
{
    public class PublishedProviderContentsGenerator : IPublishedProviderContentsGenerator
    {
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

        public string GenerateContents(PublishedProviderVersion publishedProviderVersion,
            TemplateMetadataContents templateMetadataContents,
            TemplateMapping templateMapping)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));
            Guard.ArgumentNotNull(templateMapping, nameof(templateMapping));

            Guard.ArgumentNotNull(publishedProviderVersion.Provider, nameof(publishedProviderVersion.Provider));

            IEnumerable<TemplateFundingLine> fundingLines = templateMetadataContents.RootFundingLines?.Flatten(x => x.FundingLines);
            IEnumerable<Calculation> calculations = fundingLines?.SelectMany(fl => fl.Calculations.Flatten(cal => cal.Calculations));

            dynamic contents = new
            {
                Id = publishedProviderVersion.FundingId,
                FundingVersion = $"{publishedProviderVersion.MajorVersion}_{publishedProviderVersion.MinorVersion}",
                Provider = new
                {
                    Identifier = publishedProviderVersion.ProviderId,
                    publishedProviderVersion.Provider.Name,
                    SearchableName = Sanitiser.SanitiseName(publishedProviderVersion.Provider.Name),
                    OtherIdentifiers = GetOtherIdentifiers(publishedProviderVersion.Provider),
                    publishedProviderVersion.Provider.ProviderVersionId,
                    publishedProviderVersion.Provider.ProviderType,
                    publishedProviderVersion.Provider.ProviderSubType,
                    ProviderDetails = new
                    {
                        publishedProviderVersion.Provider.DateOpened,
                        publishedProviderVersion.Provider.DateClosed,
                        publishedProviderVersion.Provider.Status,
                        publishedProviderVersion.Provider.PhaseOfEducation,
                        LocalAuthorityName = publishedProviderVersion.Provider.Authority,
                        OpenReason = publishedProviderVersion.Provider.ReasonEstablishmentOpened,
                        CloseReason = publishedProviderVersion.Provider.ReasonEstablishmentClosed,
                        TrustStatus = publishedProviderVersion.Provider.TrustStatus.ToEnumMemberAttrValue(),
                        publishedProviderVersion.Provider.TrustName,
                        publishedProviderVersion.Provider.Town,
                        publishedProviderVersion.Provider.Postcode,
                        publishedProviderVersion.Provider.CompaniesHouseNumber,
                        publishedProviderVersion.Provider.GroupIdNumber,
                        publishedProviderVersion.Provider.RscRegionName,
                        publishedProviderVersion.Provider.RscRegionCode,
                        publishedProviderVersion.Provider.GovernmentOfficeRegionName,
                        publishedProviderVersion.Provider.GovernmentOfficeRegionCode,
                        publishedProviderVersion.Provider.DistrictName,
                        publishedProviderVersion.Provider.DistrictCode,
                        publishedProviderVersion.Provider.WardName,
                        publishedProviderVersion.Provider.WardCode,
                        publishedProviderVersion.Provider.CensusWardName,
                        publishedProviderVersion.Provider.CensusWardCode,
                        publishedProviderVersion.Provider.MiddleSuperOutputAreaName,
                        publishedProviderVersion.Provider.MiddleSuperOutputAreaCode,
                        publishedProviderVersion.Provider.LowerSuperOutputAreaName,
                        publishedProviderVersion.Provider.LowerSuperOutputAreaCode,
                        publishedProviderVersion.Provider.ParliamentaryConstituencyName,
                        publishedProviderVersion.Provider.ParliamentaryConstituencyCode,
                        publishedProviderVersion.Provider.CountryCode,
                        publishedProviderVersion.Provider.CountryName
                    }
                },
                FundingStreamCode = publishedProviderVersion.FundingStreamId,
                publishedProviderVersion.FundingPeriodId,
                FundingValue = new
                {
                    TotalValue = publishedProviderVersion.TotalFunding,
                    FundingLines = fundingLines?.DistinctBy(x => x.TemplateLineId).OrderBy(_ => _.TemplateLineId).Select(x => ToFundingLine(x,
                         publishedProviderVersion.FundingLines)),
                    Calculations = calculations?.DistinctBy(x => x.TemplateCalculationId)?.OrderBy(_ => _.TemplateCalculationId).Select(calculation => BuildCalculations(publishedProviderVersion.Calculations,
                          calculation,
                          publishedProviderVersion.ProviderId)).Where(_ => _ != null)
                },
                publishedProviderVersion.VariationReasons,
                Successors = publishedProviderVersion.Provider.GetSuccessors(),
                publishedProviderVersion.Predecessors
            };

            JsonSerializerSettings serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            return JsonConvert.SerializeObject(contents, serializerSettings);
        }

        private List<dynamic> GetOtherIdentifiers(Provider provider)
        {
            List<dynamic> otherIdentifiers = new List<dynamic>();

            otherIdentifiers.AddIfIdentifierHasValue(provider.URN,
                nameof(provider.URN));
            otherIdentifiers.AddIfIdentifierHasValue(provider.UKPRN,
                nameof(provider.UKPRN));
            otherIdentifiers.AddIfIdentifierHasValue(provider.LACode,
                nameof(provider.LACode));
            otherIdentifiers.AddIfIdentifierHasValue(provider.UPIN,
                nameof(provider.UPIN));
            otherIdentifiers.AddIfIdentifierHasValue(provider.DfeEstablishmentNumber,
                "DfeNumber"); // NOTE: the CFS provider property name is not the same as the funding data model

            return otherIdentifiers;
        }

        private dynamic ToFundingLine(TemplateFundingLine fundingLine,
            IEnumerable<FundingLine> fundingLineValues)
        {
            FundingLine fundingLineValue = fundingLineValues.First(_ =>
                _.TemplateLineId == fundingLine.TemplateLineId);

            return new
            {
                fundingLine.Name,
                fundingLine.FundingLineCode,
                Value = fundingLineValue.Value.DecimalAsObject(),
                fundingLine.TemplateLineId,
                Type = fundingLine.Type.ToString(),
                fundingLineValue.DistributionPeriods
            };
        }

        private static SchemaJsonCalculation BuildCalculations(IEnumerable<FundingCalculation> fundingCalculations,
            Calculation templateCalculation,
            string providerId)
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

            return mapper.ToCalculation(templateCalculation,
                publishedFundingCalculation,
                fundingCalculations,
                providerId);
        }

        private interface ICalculationMapper
        {
            SchemaJsonCalculation ToCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string providerId);
        }

        private class PercentageChangeBetweenAandBCalculation : BasicCalculation
        {
            public override SchemaJsonCalculation ToCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string providerId)
            {
                SchemaJsonCalculation schemaJsonCalculation = base.ToCalculation(templateCalculation,
                    publishedFundingCalculation,
                    publishedFundingCalculations,
                    providerId);

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
            public override SchemaJsonCalculation ToCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string providerId)
            {
                dynamic schemaJsonCalculation = base.ToCalculation(templateCalculation,
                    publishedFundingCalculation,
                    publishedFundingCalculations,
                    providerId);

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
            public virtual SchemaJsonCalculation ToCalculation(Calculation templateCalculation,
                FundingCalculation publishedFundingCalculation,
                IEnumerable<FundingCalculation> publishedFundingCalculations,
                string providerId) =>
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
            fundingCalculation.AggregationType != (AggregationType)None
            || fundingCalculation.Calculations?.Any() == true;
    }
}