using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Generators.Schema10
{
    public class PublishedProviderContentsGenerator : IPublishedProviderContentsGenerator
    {
        public string GenerateContents(PublishedProviderVersion publishedProviderVersion, TemplateMetadataContents templateMetadataContents, TemplateMapping templateMapping, IEnumerable<CalculationResult> calculationResults, IEnumerable<FundingLine> fundingLines)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));
            Guard.ArgumentNotNull(templateMapping, nameof(templateMapping));
            Guard.ArgumentNotNull(calculationResults, nameof(calculationResults));
            Guard.ArgumentNotNull(fundingLines, nameof(fundingLines));
            Guard.ArgumentNotNull(publishedProviderVersion.Provider, nameof(publishedProviderVersion.Provider));

            dynamic contents = new {
                Id = publishedProviderVersion.FundingId,
                FundingVersion = $"{publishedProviderVersion.MajorVersion}_{publishedProviderVersion.MinorVersion}",
                Provider = new
                {
                    Identifier = publishedProviderVersion.ProviderId,
                    Name = publishedProviderVersion.Provider.Name,
                    SearchableName = System.Text.RegularExpressions.Regex.Replace(publishedProviderVersion.Provider.Name, @"\s+", string.Empty),
                    OtherIdentifiers = GetOtherIdentifiers(publishedProviderVersion.Provider),
                    ProviderVersionId = publishedProviderVersion.Provider.ProviderVersionId,
                    ProviderType = publishedProviderVersion.Provider.ProviderType,
                    ProviderSubType = publishedProviderVersion.Provider.ProviderSubType,
                    ProviderDetails = new
                    {
                        DateOpened = publishedProviderVersion.Provider.DateOpened,
                        DateClosed = publishedProviderVersion.Provider.DateClosed,
                        Status = publishedProviderVersion.Provider.Status,
                        PhaseOfEducation = publishedProviderVersion.Provider.PhaseOfEducation,
                        LocalAuthorityName = publishedProviderVersion.Provider.LocalAuthorityName,
                        OpenReason = publishedProviderVersion.Provider.ReasonEstablishmentOpened,
                        CloseReason = publishedProviderVersion.Provider.ReasonEstablishmentClosed,
                        TrustStatus = publishedProviderVersion.Provider.TrustStatus.ToEnumMemberAttrValue(),
                        TrustName = publishedProviderVersion.Provider.TrustName,
                        Town = publishedProviderVersion.Provider.Town,
                        Postcode = publishedProviderVersion.Provider.Postcode,
                        CompaniesHouseNumber = publishedProviderVersion.Provider.CompaniesHouseNumber,
                        GroupIdNumber = publishedProviderVersion.Provider.GroupIdNumber,
                        RscRegionName = publishedProviderVersion.Provider.RscRegionName,
                        RscRegionCode = publishedProviderVersion.Provider.RscRegionCode,
                        GovernmentOfficeRegionName = publishedProviderVersion.Provider.GovernmentOfficeRegionName,
                        GovernmentOfficeRegionCode = publishedProviderVersion.Provider.GovernmentOfficeRegionCode,
                        DistrictName = publishedProviderVersion.Provider.DistrictName,
                        DistrictCode = publishedProviderVersion.Provider.DistrictCode,
                        WardName = publishedProviderVersion.Provider.WardName,
                        WardCode = publishedProviderVersion.Provider.WardCode,
                        CensusWardName = publishedProviderVersion.Provider.CensusWardName,
                        CensusWardCode = publishedProviderVersion.Provider.CensusWardCode,
                        MiddleSuperOutputAreaName = publishedProviderVersion.Provider.MiddleSuperOutputAreaName,
                        MiddleSuperOutputAreaCode = publishedProviderVersion.Provider.MiddleSuperOutputAreaCode,
                        LowerSuperOutputAreaName = publishedProviderVersion.Provider.LowerSuperOutputAreaName,
                        LowerSuperOutputAreaCode = publishedProviderVersion.Provider.LowerSuperOutputAreaCode,
                        ParliamentaryConstituencyName = publishedProviderVersion.Provider.ParliamentaryConstituencyName,
                        ParliamentaryConstituencyCode = publishedProviderVersion.Provider.ParliamentaryConstituencyCode,
                        CountryCode = publishedProviderVersion.Provider.CountryCode,
                        CountryName = publishedProviderVersion.Provider.CountryName
                    }
                },
                FundingStreamCode = publishedProviderVersion.FundingStreamId,
                FundingPeriodId = publishedProviderVersion.FundingPeriodId,
                FundingValue = new { TotalValue = Convert.ToInt32(fundingLines.Sum(x => x.Value)), FundingLines = templateMetadataContents.RootFundingLines?.Select(x => ToFundingLine(x, fundingLines, templateMapping, calculationResults)) },
                VariationReasons = publishedProviderVersion.VariationReasons,
                Successors = string.IsNullOrWhiteSpace(publishedProviderVersion.Provider.Successor) ? null : new List<string> { publishedProviderVersion.Provider.Successor },
                Predecessors = publishedProviderVersion.Predecessors
            };

            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            return JsonConvert.SerializeObject(contents, serializerSettings);
        }

        private List<dynamic> GetOtherIdentifiers(Provider provider)
        {
            List<dynamic> otherIdentifiers = new List<dynamic>();

            if (!string.IsNullOrWhiteSpace(provider.URN))
            {
                otherIdentifiers.Add(new
                {
                    Type = "URN",
                    Value = provider.URN
                });
            }

            if (!string.IsNullOrWhiteSpace(provider.UKPRN))
            {
                otherIdentifiers.Add(new
                {
                    Type = "UKPRN",
                    Value = provider.UKPRN
                });
            }

            if (!string.IsNullOrWhiteSpace(provider.LACode))
            {
                otherIdentifiers.Add(new
                {
                    Type = "LACode",
                    Value = provider.LACode
                });
            }

            if (!string.IsNullOrWhiteSpace(provider.UPIN))
            {
                otherIdentifiers.Add(new
                {
                    Type = "UPIN",
                    Value = provider.UPIN
                });
            }

            if (!string.IsNullOrWhiteSpace(provider.DfeEstablishmentNumber))
            {
                otherIdentifiers.Add(new
                {
                    Type = "DfeNumber",
                    Value = provider.DfeEstablishmentNumber
                });
            }

            return otherIdentifiers;
        }

        private dynamic ToFundingLine(TemplateFundingLine fundingLine, IEnumerable<FundingLine> fundingLineValues, TemplateMapping templateMapping, IEnumerable<CalculationResult> calculationResults)
        {
            return new
            {
                Name = fundingLine.Name,
                FundingLineCode = fundingLine.FundingLineCode,
                Value = Convert.ToInt32(fundingLineValues.Where(x => x.TemplateLineId == fundingLine.TemplateLineId)?.Single().Value),
                TemplateLineId = fundingLine.TemplateLineId,
                Type = fundingLine.Type.ToString(),
                Calculations = fundingLine.Calculations?.Select(x => ToCalculation(x, templateMapping, calculationResults)),
                FundingLines = fundingLine.FundingLines?.Select(x => ToFundingLine(x, fundingLineValues, templateMapping, calculationResults)),
                DistributionPeriods = fundingLineValues.Where(x => x.TemplateLineId == fundingLine.TemplateLineId)?.Single().DistributionPeriods
            };
        }

        private dynamic ToCalculation(Calculation calculation, TemplateMapping templateMapping, IEnumerable<CalculationResult> calculationResults)
        {
            string calculationId = templateMapping.TemplateMappingItems.Where(x => x.TemplateId == calculation.TemplateCalculationId)?.Single().CalculationId;
            
            return new
            {
                Name = calculation.Name,
                TemplateCalculationId = calculation.TemplateCalculationId,
                Value = string.Format("{0:0}", calculationResults.Where(x => x.Id == calculationId)?.Single().Value),
                ValueFormat = calculation.ValueFormat.ToString(),
                Type = calculation.Type.ToString(),
                FormulaText = calculation.FormulaText,
                AggregationType = calculation.AggregationType.ToString(),
                Calculations = calculation.Calculations?.Select(x => ToCalculation(x, templateMapping, calculationResults)),
                ReferenceData = calculation.ReferenceData?.Select(x => ToReferenceData(x))
            };
        }

        private dynamic ToReferenceData(ReferenceData referenceData)
        {
            return new
            {
                Name = referenceData.Name,
                TemplateReferenceId = referenceData.TemplateReferenceId,
                Format = referenceData.Format.ToString(),
                Value = referenceData.Value,
                AggregationType = referenceData.AggregationType.ToString()
            };
        }
    }
}