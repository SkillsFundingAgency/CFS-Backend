using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Generators.Schema10.UnitTests
{
    [TestClass]
    public class PublishedProviderContentsGeneratorTests
    {
        private const int MaxCalculationResults = 1000;
        private const int MaxFundingLines = 100;
        private const int ValueMultiplicationFactor = 1000;

        [TestMethod]
        public void GenerateContents_GivenValidProviderVersionTemplateMappingCalculationsAndFundingLines_ReturnsValidJson()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Generators.Schema10.UnitTests.Resources.dsg1.0.json"));

            PublishedProviderContentsGenerator publishedProviderContentsGenerator = new PublishedProviderContentsGenerator();

            //Act
            string publishedcontents = publishedProviderContentsGenerator.GenerateContents(GetProviderVersion(), contents, GetTemplateMapping(), GetCalculationResults(), GetFundingLines());

            //Assert
            JObject json = JsonConvert.DeserializeObject<JObject>(publishedcontents);

            json.TryGetValue("fundingStreamCode", out JToken fundingStreamCodeToken);
            ((JValue)fundingStreamCodeToken).Value<string>().Should().Be("PSG");

            json.TryGetValue("fundingPeriodId", out JToken fundingPeriodIdToken);
            ((JValue)fundingPeriodIdToken).Value<string>().Should().Be("AY-1920");

            json.TryGetValue("provider", out JToken providerToken);
            providerToken.Value<JObject>().TryGetValue("otherIdentifiers", out JToken otherIdentifiersToken);
            otherIdentifiersToken[0].Value<JObject>().TryGetValue("type", out JToken urn);
            ((JValue)urn).Value<string>().Should().Be("URN");

            otherIdentifiersToken[1].Value<JObject>().TryGetValue("type", out JToken ukprn);
            ((JValue)ukprn).Value<string>().Should().Be("UKPRN");

            json.TryGetValue("fundingValue", out JToken fundigValueToken);

            fundigValueToken.Value<JObject>().TryGetValue("totalValue", out JToken fundingTotalValue);
            ((JValue)fundingTotalValue).Value<string>().Should().Be("5050000");

            fundigValueToken.Value<JObject>().TryGetValue("fundingLines", out JToken fundingLines);
            fundingLines[0].Value<JObject>().TryGetValue("name", out JToken fundingLineName);
            ((JValue)fundingLineName).Value<string>().Should().Be("Prior To Recoupment");

            fundingLines[0].Value<JObject>().TryGetValue("fundingLines", out JToken fundingLines1);

            fundingLines1[0].Value<JObject>().TryGetValue("fundingLines", out JToken fundingLines2);

            fundingLines2[0].Value<JObject>().TryGetValue("calculations", out JToken fundingLineCalculations);
            fundingLineCalculations[0].Value<JObject>().TryGetValue("value", out JToken calculationValue);

            ((JValue)calculationValue).Value<string>().Should().Be("3000");

            fundingLineCalculations[0].Value<JObject>().TryGetValue("calculations", out JToken fundingLineSubCalculations);
            fundingLineSubCalculations[0].Value<JObject>().TryGetValue("value", out JToken subcalculationValue);

            ((JValue)subcalculationValue).Value<string>().Should().Be("4000");
        }

        [TestMethod]
        public void GenerateContents_GivenValidPublishedProviderVersion_ReturnsValidJson()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Generators.Schema10.UnitTests.Resources.exampleProviderTemplate1.json"));

            PublishedProviderContentsGenerator publishedProviderContentsGenerator = new PublishedProviderContentsGenerator();

            //Act
            string publishedcontents = System.Text.RegularExpressions.Regex.Replace(publishedProviderContentsGenerator.GenerateContents(GetProviderVersion(), contents, GetTemplateMapping(), GetCalculationResults(), GetFundingLines()), @"\r\n|\s+", string.Empty);

            //Assert
            string expectedOutput = System.Text.RegularExpressions.Regex.Replace(GetResourceString("CalculateFunding.Generators.Schema10.UnitTests.Resources.exampleProviderOutput1.json"), @"\r\n|\s+", string.Empty);

            publishedcontents
                .Should()
                .Be(expectedOutput);
        }

        public ITemplateMetadataGenerator CreateTemplateGenerator(ILogger logger = null)
        {
            return new TemplateMetadataGenerator(logger ?? CreateLogger());
        }

        public ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public TemplateMapping GetTemplateMapping()
        {
            List<TemplateMappingItem> items = new List<TemplateMappingItem>();

            for (uint i = 0; i <= MaxCalculationResults; i++)
            {
                items.Add(new TemplateMappingItem { TemplateId = i, CalculationId = i.ToString() });
            }

            return new TemplateMapping { TemplateMappingItems = items };
        }

        public IEnumerable<CalculationResult> GetCalculationResults()
        {
            List<CalculationResult> results = new List<CalculationResult>();

            for (uint i = 0; i <= MaxCalculationResults; i++)
            {
                results.Add(new CalculationResult { Id = i.ToString(), Value = i * ValueMultiplicationFactor });
            }

            return results;
        }

        public IEnumerable<Models.Publishing.FundingLine> GetFundingLines()
        {
            List<Models.Publishing.FundingLine> lines = new List<Models.Publishing.FundingLine>();

            for (uint i = 0; i <= MaxFundingLines; i++)
            {
                lines.Add(new Models.Publishing.FundingLine { TemplateLineId = i, Value = i * ValueMultiplicationFactor });
            }

            return lines;
        }

        public PublishedProviderVersion GetProviderVersion()
        {
            return new PublishedProviderVersion
            {
                TotalFunding = 5050000,
                Provider = new Provider
                {
                    ProviderId = "12345678",
                    Name = "Example School 1",
                    ProviderVersionId = "3",
                    ProviderType = "Academies",
                    ProviderSubType = "Academy alternative provision converter",
                    URN = "123453",
                    UKPRN = "12345678",
                    DateOpened = DateTime.Parse("2012-12-02T00:00:00+00:00"),
                    DateClosed = null,
                    Status = "Open",
                    PhaseOfEducation = "Secondary",
                    LocalAuthorityName = "Camden",
                    ReasonEstablishmentOpened = "Academy Converter",
                    ReasonEstablishmentClosed = null,
                    TrustStatus = ProviderTrustStatus.SupportedByASingleAacademyTrust,
                    TrustName = "Trust Name",
                    Town = "MOCK TOWN",
                    Postcode = "MOCK POSTCODE",
                    CompaniesHouseNumber = "6237225",
                    GroupIdNumber = "GroupID2522",
                    RscRegionName = "North West",
                    RscRegionCode = "NW",
                    GovernmentOfficeRegionName = "Gov Office Region 2",
                    GovernmentOfficeRegionCode = "GRCC2",
                    DistrictName = "District Name",
                    DistrictCode = "DC",
                    WardName = "South Bermondsey",
                    WardCode = "WC522257",
                    CensusWardName = "Census Ward Name",
                    CensusWardCode = "Census Ward Code 1",
                    MiddleSuperOutputAreaName = "MSOA Fifty Six",
                    MiddleSuperOutputAreaCode = "MSOA56",
                    LowerSuperOutputAreaName = "Lower 66",
                    LowerSuperOutputAreaCode = "L66",
                    ParliamentaryConstituencyName = "Bermondsey and Old Southwark",
                    ParliamentaryConstituencyCode = "BOS",
                    CountryCode = "E",
                    CountryName = "England"
                },
                FundingId = "PSG-AY-1920-12345678-1_0",
                ProviderId = "12345678",
                FundingStreamId = "PSG",
                FundingPeriodId = "AY-1920",
                Version = 1,
                MajorVersion = 1,
                MinorVersion = 0,
                VariationReasons = new List<VariationReason> { VariationReason.NameFieldUpdated, VariationReason.FundingUpdated }
            };
        }

        private string GetResourceString(string resourceName)
        {
            return typeof(PublishedProviderContentsGeneratorTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}
