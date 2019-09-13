using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
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
        private const int MaxFundingResults = 2;
        private const int MaxFundingLines = 100;
        private const int ValueMultiplicationFactor = 1000;

        [TestMethod]
        public void GenerateContents_GivenValidPublishedProviderVersion_ReturnsValidJson()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Generators.Schema10.UnitTests.Resources.exampleProviderTemplate1.json"));

            PublishedProviderContentsGenerator publishedProviderContentsGenerator = new PublishedProviderContentsGenerator();

            //Act
            string publishedcontents = publishedProviderContentsGenerator.GenerateContents(GetProviderVersion(), contents, GetTemplateMapping(), GetGeneratedProviderResult());

            //Assert
            string expectedOutput = GetResourceString("CalculateFunding.Generators.Schema10.UnitTests.Resources.exampleProviderOutput1.json").Prettify();

            publishedcontents
                .Prettify()
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

        public GeneratedProviderResult GetGeneratedProviderResult()
        {
            return new GeneratedProviderResult { FundingLines = GetFundingLines(), Calculations = GetCalculationResults(), ReferenceData = GetReferenceData() };
        }

        public IEnumerable<Models.Publishing.FundingReferenceData> GetReferenceData()
        {
            List<Models.Publishing.FundingReferenceData> results = new List<Models.Publishing.FundingReferenceData>();

            for (uint i = 0; i <= MaxFundingResults; i++)
            {
                results.Add(new Models.Publishing.FundingReferenceData { TemplateReferenceId = i, Value = i * ValueMultiplicationFactor });
            }

            return results;
        }

        public IEnumerable<Models.Publishing.FundingCalculation> GetCalculationResults()
        {
            List<Models.Publishing.FundingCalculation> results = new List<Models.Publishing.FundingCalculation>();

            for (uint i = 0; i <= MaxCalculationResults; i++)
            {
                results.Add(new Models.Publishing.FundingCalculation { TemplateCalculationId = i, Value = i * ValueMultiplicationFactor });
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
