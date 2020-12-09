using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.IoC;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Serilog.Core;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class FundingValueAggregatorTests
    {
        private TemplateMetadataContents _contents;
        private FundingValueAggregator _fundingValueAggregator;

        [TestInitialize]
        public void SetUp()
        {
            _fundingValueAggregator = new FundingValueAggregator();  
        }

        [TestMethod]
        public void GroupRateAggregation()
        {
            IEnumerable<AggregateFundingLine> fundingLines = WhenTheSchema1_1FundingLinesAreAggregated();
            
            //check the group rate calc results
            IEnumerable<AggregateFundingCalculation> aggregateFundingCalculations = fundingLines.Single().Calculations;

            AggregateFundingCalculation groupRate = aggregateFundingCalculations.SingleOrDefault(_ => _.TemplateCalculationId == 9003);

            groupRate
                .Value
                .Should()
                .Be(4701.66478M);

            AggregateFundingCalculation groupRateDivideByZero = aggregateFundingCalculations.SingleOrDefault(_ => _.TemplateCalculationId == 9004);

            groupRateDivideByZero
                .Value
                .Should()
                .Be(0M);
        }

        [TestMethod]
        public void PercentageChangedAggregation()
        {
            IEnumerable<AggregateFundingLine> fundingLines = WhenTheSchema1_1FundingLinesAreAggregated();
            
            //check the percentage difference calc results
            IEnumerable<AggregateFundingCalculation> aggregateFundingCalculations = fundingLines.Single().Calculations;
            
            AggregateFundingCalculation sum = aggregateFundingCalculations.SingleOrDefault(_ => _.TemplateCalculationId == 9001);

            sum.Value
                .Should()
                .Be(-4.7833722155895512153611024900M);
            
            AggregateFundingCalculation average = aggregateFundingCalculations.SingleOrDefault(_ => _.TemplateCalculationId == 9002);

            average.Value
                .Should()
                .Be(5.0236732038232500M);

            AggregateFundingCalculation grouprate = aggregateFundingCalculations.SingleOrDefault(_ => _.TemplateCalculationId == 9005);

            grouprate.Value
                .Should()
                .Be(5.0236732038232500M);
        }

        private IEnumerable<AggregateFundingLine> WhenTheSchema1_1FundingLinesAreAggregated()
        {
            ITemplateMetadataGenerator templateMetaDataGenerator = CreateSchema11TemplateGenerator();

            _contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProviderTemplate1_Schema1_1.json"));
            
           return _fundingValueAggregator.GetTotals(_contents, GetProviderVersions("_Schema1_1"));    
        }
        
        [TestMethod]
        public void GetTotals_GivenValidPublishedProviderVersions_ReturnsFundingLines()
        {
            //Arrange
            ITemplateMetadataGenerator templateMetaDataGenerator = CreateSchema10TemplateGenerator();

            _contents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProviderTemplate1_Schema1_0.json"));

            //Act
            IEnumerable<AggregateFundingLine> fundingLines = _fundingValueAggregator.GetTotals(_contents, GetProviderVersions());

            //Assert
            fundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Value
                .Should()
                .Be(6000.975M);

            fundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Calculations.Where(x => x.TemplateCalculationId == 156).First().Calculations.Where(x => x.TemplateCalculationId == 152).First().Value
                .Should()
                .Be(4590000.975M);

            fundingLines.First().Calculations.Where(x => x.TemplateCalculationId == 1).First().Calculations.Where(x => x.TemplateCalculationId == 156).First().Calculations.Where(x => x.TemplateCalculationId == 157 && x.Value != null).IsNullOrEmpty()
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

        public ITemplateMetadataGenerator CreateSchema10TemplateGenerator() 
            => new TemplateMetadataGenerator(Logger.None);

        public ITemplateMetadataGenerator CreateSchema11TemplateGenerator()
            => new CalculateFunding.Common.TemplateMetadata.Schema11.TemplateMetadataGenerator(Logger.None);

        public IEnumerable<PublishedProviderVersion> GetProviderVersions(string schema = null)
        {
            List<PublishedProviderVersion> providerVersions = new List<PublishedProviderVersion>();

            for (int i = 1; i <= 3; i++)
            {
                providerVersions.Add(new PublishedProviderVersion
                {
                    Provider = GetProvider(i),
                    Calculations = JsonConvert.DeserializeObject<IEnumerable<FundingCalculation>>(GetResourceString($"CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProvider{i}Calculations{schema}.json")),
                    FundingLines = new FundingLine[] { NewFundingLine(fl => fl.WithTemplateLineId(1).WithValue(0)) },
                    ProviderId = "1234" + i,
                    FundingStreamId = "PSG",
                    FundingPeriodId = "AY-1920",
                    Version = 1,
                    MajorVersion = 1,
                    MinorVersion = 0,
                    VariationReasons = new List<VariationReason> { VariationReason.NameFieldUpdated, VariationReason.FundingUpdated }
                });
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
