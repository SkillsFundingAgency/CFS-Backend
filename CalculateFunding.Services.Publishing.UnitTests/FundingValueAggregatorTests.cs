using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class FundingValueAggregatorTests
    {
        [TestMethod]
        public void GetTotals_GivenValidPublishedProviderVersions_ReturnsFundingLines()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ITemplateMetadataGenerator templateMetaDataGenerator = CreateTemplateGenerator(logger);

            TemplateMetadataContents contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProviderTemplate1.json"));

            FundingValueAggregator fundingValueAggregator = new FundingValueAggregator();

            //Act
            IEnumerable<AggregateFundingLine> fundingLines = fundingValueAggregator.GetTotals(contents, GetProviderVersions());

            //Assert
            fundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Value
                .Should()
                .Be(6000.975M);

            fundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Calculations.Where(x => x.TemplateCalculationId == 156).First().Calculations.Where(x => x.TemplateCalculationId == 152).First().Value
                .Should()
                .Be(4590000.975M);

            fundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Calculations.Where(x => x.TemplateCalculationId == 156).First().Calculations.Where(x => x.TemplateCalculationId == 157).IsNullOrEmpty()
                .Should()
                .BeTrue();

            fundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 126).First().Value
                .Should()
                .Be(127000.325M);

            fundingLines.First().FundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Value
                .Should()
                .Be(6000.975M);

            fundingLines.First().FundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Calculations.Where(x => x.TemplateCalculationId == 156).First().Calculations.Where(x => x.TemplateCalculationId == 152).First().Value
                .Should()
                .Be(4590000.975M);

            fundingLines.First().FundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 126).First().Value
                .Should()
                .Be(127000.325M);
        }

        public ITemplateMetadataGenerator CreateTemplateGenerator(ILogger logger = null)
        {
            return new TemplateMetadataGenerator(logger ?? CreateLogger());
        }

        public ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public IEnumerable<PublishedProviderVersion> GetProviderVersions()
        {
            List<PublishedProviderVersion> providerVersions = new List<PublishedProviderVersion>();

            for (int i = 1; i <= 3; i++)
            {
                providerVersions.Add(new PublishedProviderVersion
                {
                    Provider = GetProvider(i),
                    Calculations = JsonConvert.DeserializeObject<IEnumerable<FundingCalculation>>(GetResourceString($"CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProvider{i}Calculations.json")),
                    FundingLines = new FundingLine[] { NewFundingLine(fl => fl.WithTemplateLineId(1).WithValue(0)) },
                    ProviderId = "1234" + i,
                    FundingStreamId = "PSG",
                    FundingPeriodId = "AY-1920",
                    Version = 1,
                    MajorVersion = 1,
                    MinorVersion = 0,
                    VariationReasons = new List<VariationReason> { VariationReason.NameFieldUpdated, VariationReason.FundingUpdated }
                }); ;
            }

            return providerVersions;
        }

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        public Provider GetProvider(int index)
        {
            return new Provider
            {
                ProviderId = $"1234{index}",
                Name = $"Example School {index}",
                ProviderVersionId = "3",
                ProviderType = "Academies",
                ProviderSubType = "Academy alternative provision converter",
                URN = "123453",
                UKPRN = "12345678",
                DateOpened = DateTime.Parse("2012-12-02T00:00:00+00:00"),
                DateClosed = null,
                Status = "Open",
                PhaseOfEducation = "Secondary",
                Authority = "Camden",
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
            };
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(FundingValueAggregatorTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}
