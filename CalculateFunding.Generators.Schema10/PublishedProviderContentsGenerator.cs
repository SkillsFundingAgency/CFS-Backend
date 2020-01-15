using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
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
        public string GenerateContents(PublishedProviderVersion publishedProviderVersion, TemplateMetadataContents templateMetadataContents, TemplateMapping templateMapping)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));
            Guard.ArgumentNotNull(templateMetadataContents, nameof(templateMetadataContents));
            Guard.ArgumentNotNull(templateMapping, nameof(templateMapping));

            Guard.ArgumentNotNull(publishedProviderVersion.Provider, nameof(publishedProviderVersion.Provider));

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
                        publishedProviderVersion.Provider.LocalAuthorityName,
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
                        publishedProviderVersion.Provider.CountryName,
                    }
                },
                FundingStreamCode = publishedProviderVersion.FundingStreamId,
                publishedProviderVersion.FundingPeriodId,
                FundingValue = new { TotalValue = publishedProviderVersion.TotalFunding, FundingLines = templateMetadataContents.RootFundingLines?.Select(x => ToFundingLine(x, publishedProviderVersion.FundingLines, templateMapping, publishedProviderVersion.Calculations, publishedProviderVersion.ReferenceData, publishedProviderVersion.ProviderId)) },
                publishedProviderVersion.VariationReasons,
                Successors = string.IsNullOrWhiteSpace(publishedProviderVersion.Provider.Successor) ? null : new List<string> { publishedProviderVersion.Provider.Successor },
                publishedProviderVersion.Predecessors
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

        private dynamic ToFundingLine(TemplateFundingLine fundingLine, IEnumerable<FundingLine> fundingLineValues, TemplateMapping templateMapping, IEnumerable<FundingCalculation> calculationResults, IEnumerable<FundingReferenceData> referenceData, string providerId)
        {
            return new
            {
                fundingLine.Name,
                fundingLine.FundingLineCode,
                Value = fundingLineValues.Where(x => x.TemplateLineId == fundingLine.TemplateLineId)?.Single().Value.DecimalAsObject(),
                fundingLine.TemplateLineId,
                Type = fundingLine.Type.ToString(),
                Calculations = fundingLine.Calculations?.Select(x => ToCalculation(x, templateMapping, calculationResults, referenceData, providerId)),
                FundingLines = fundingLine.FundingLines?.Select(x => ToFundingLine(x, fundingLineValues, templateMapping, calculationResults, referenceData, providerId)),
                fundingLineValues.Where(x => x.TemplateLineId == fundingLine.TemplateLineId)?.Single().DistributionPeriods
            };
        }

        private dynamic ToCalculation(Common.TemplateMetadata.Models.Calculation calculation, TemplateMapping templateMapping, IEnumerable<FundingCalculation> calculationResults, IEnumerable<FundingReferenceData> referenceData, string providerId)
        {
            string calculationId = templateMapping.TemplateMappingItems.Where(x => x.TemplateId == calculation.TemplateCalculationId)?.Single().CalculationId;
            IEnumerable<FundingReferenceData> publishedFundingReferenceData = referenceData?.Where(_ => calculation.ReferenceData?.Any(calcReferenceData => calcReferenceData.TemplateReferenceId == _.TemplateReferenceId) ?? false);
            IEnumerable<dynamic> refernceData = publishedFundingReferenceData?.Select(_ => ToReferenceData(calculation.ReferenceData?.Where(calcReferenceData => calcReferenceData.TemplateReferenceId == _.TemplateReferenceId).Single(), _));

            FundingCalculation fundingCalculationResult = calculationResults.Where(x => x.TemplateCalculationId == calculation.TemplateCalculationId)?.SingleOrDefault();
            if (fundingCalculationResult == null)
            {
                int totalCount = calculationResults.Count(c => c.TemplateCalculationId == calculation.TemplateCalculationId);
                throw new InvalidOperationException($"Unable to find calculation result for provider '{providerId}' with TemplateCalculationId '{calculation.TemplateCalculationId}'. A total of {totalCount} results found.");
            }

            return new
            {
                calculation.Name,
                calculation.TemplateCalculationId,
                fundingCalculationResult.Value,
                ValueFormat = calculation.ValueFormat.ToString(),
                Type = calculation.Type.ToString(),
                calculation.FormulaText,
                AggregationType = calculation.AggregationType.ToString(),
                Calculations = calculation.Calculations?.Select(x => ToCalculation(x, templateMapping, calculationResults, referenceData, providerId)),
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
                Value = $"{fundingReferenceData.Value}",
                AggregationType = referenceData.AggregationType.ToString()
            };
        }
    }
}