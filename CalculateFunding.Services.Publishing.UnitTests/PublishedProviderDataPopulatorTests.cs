using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Models.Publishing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using DistributionPeriod = CalculateFunding.Models.Publishing.DistributionPeriod;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using ProfilePeriod = CalculateFunding.Models.Publishing.ProfilePeriod;
using Provider = CalculateFunding.Models.Publishing.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderDataPopulatorTests
    {
        [TestMethod]
        public void UpdatePublishedProvider_GivenNoChanges_ReturnsFalse()
        {
            //Arrange
            PublishedProviderVersion publishedProviderVersion = CreateProviderVersion();
            GeneratedProviderResult generatedProviderResult = CreateGeneratedProviderResult();
            Common.ApiClient.Providers.Models.Provider provider = CreateProvider();

            IMapper mapper = CreateMapper();
            mapper
                .Map<Provider>(provider)
                .Returns(publishedProviderVersion.Provider);

            PublishedProviderDataPopulator publishedProviderDataPopulator = new PublishedProviderDataPopulator(mapper);

            //Act
            bool result = publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion, generatedProviderResult, provider);

            //Act
            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenChangedProvider_ReturnsTrue()
        {
            //Arrange
            PublishedProviderVersion publishedProviderVersion = CreateProviderVersion();

            PublishedProviderVersion publishedProviderVersionForMapping = publishedProviderVersion.Clone() as PublishedProviderVersion;
            publishedProviderVersionForMapping.Provider.Name = "NewName";

            GeneratedProviderResult generatedProviderResult = CreateGeneratedProviderResult();

            Common.ApiClient.Providers.Models.Provider provider = CreateProvider();

            IMapper mapper = CreateMapper();
            mapper
                .Map<Provider>(provider)
                .Returns(publishedProviderVersionForMapping.Provider);

            PublishedProviderDataPopulator publishedProviderDataPopulator = new PublishedProviderDataPopulator(mapper);

            //Act
            bool result = publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion, generatedProviderResult, provider);

            //Act
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenFundingLineChanges_ReturnsFalse()
        {
            //Arrange
            PublishedProviderVersion publishedProviderVersion = CreateProviderVersion();

            GeneratedProviderResult generatedProviderResult = CreateGeneratedProviderResult();
            generatedProviderResult.FundingLines.First().Name = "New Name";

            Common.ApiClient.Providers.Models.Provider provider = CreateProvider();

            IMapper mapper = CreateMapper();
            mapper
                .Map<Provider>(provider)
                .Returns(publishedProviderVersion.Provider);

            PublishedProviderDataPopulator publishedProviderDataPopulator = new PublishedProviderDataPopulator(mapper);

            //Act
            bool result = publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion, generatedProviderResult, provider);

            //Act
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenCalculationChanges_ReturnsFalse()
        {
            //Arrange
            PublishedProviderVersion publishedProviderVersion = CreateProviderVersion();

            GeneratedProviderResult generatedProviderResult = CreateGeneratedProviderResult();
            generatedProviderResult.Calculations.First().Value = 56;

            Common.ApiClient.Providers.Models.Provider provider = CreateProvider();

            IMapper mapper = CreateMapper();
            mapper
                .Map<Provider>(provider)
                .Returns(publishedProviderVersion.Provider);

            PublishedProviderDataPopulator publishedProviderDataPopulator = new PublishedProviderDataPopulator(mapper);

            //Act
            bool result = publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion, generatedProviderResult, provider);

            //Act
            result
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenReferenceDataChanges_ReturnsFalse()
        {
            //Arrange
            PublishedProviderVersion publishedProviderVersion = CreateProviderVersion();

            GeneratedProviderResult generatedProviderResult = CreateGeneratedProviderResult();
            generatedProviderResult.ReferenceData.First().Value = 56;

            Common.ApiClient.Providers.Models.Provider provider = CreateProvider();

            IMapper mapper = CreateMapper();
            mapper
                .Map<Provider>(provider)
                .Returns(publishedProviderVersion.Provider);

            PublishedProviderDataPopulator publishedProviderDataPopulator = new PublishedProviderDataPopulator(mapper);

            //Act
            bool result = publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion, generatedProviderResult, provider);

            //Act
            result
                .Should()
                .BeTrue();
        }

        private static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        private static IEnumerable<FundingLine> CreateFundingLines()
        {
            return new[]
            {
                new FundingLine { Name="Abc",FundingLineCode = "FL1", Type = OrganisationGroupingReason.Payment,Value = 500, TemplateLineId = 123,
                    DistributionPeriods = new[] { new DistributionPeriod
                    {
                        ProfilePeriods = new[]
                        {
                            new ProfilePeriod { DistributionPeriodId = "2018-2019", Occurrence = 1, Type = ProfilePeriodType.CalendarMonth, TypeValue = "October", ProfiledValue = 150.0M, Year = 2018},
                            new ProfilePeriod { DistributionPeriodId = "2018-2019", Occurrence = 1, Type = ProfilePeriodType.CalendarMonth, TypeValue = "October", ProfiledValue = 150.0M, Year = 2018}
                        },
                        DistributionPeriodId = "2018-2019",
                        Value = 300.0M
                    }
                } },
                new FundingLine { Name="Xyz",FundingLineCode = "AB1", Type = OrganisationGroupingReason.Payment,Value = 600, TemplateLineId = 123, DistributionPeriods = new[] { new DistributionPeriod
                {
                    ProfilePeriods = new[]
                    {
                        new ProfilePeriod { DistributionPeriodId = "2018-2019", Occurrence = 1, Type = ProfilePeriodType.CalendarMonth, TypeValue = "October", ProfiledValue = 100.0M, Year = 2018},
                        new ProfilePeriod { DistributionPeriodId = "2018-2019", Occurrence = 1, Type = ProfilePeriodType.CalendarMonth, TypeValue = "October", ProfiledValue = 100.0M, Year = 2018}
                    },
                    DistributionPeriodId = "2018-2019",
                    Value = 200.0M
                }}}
            };
        }

        private static GeneratedProviderResult CreateGeneratedProviderResult()
        {
            return new GeneratedProviderResult
            {
                FundingLines = CreateFundingLines(),
                Calculations = CreateCalculations(),
                ReferenceData = CreateReferenceData()
            };
        }

        private static IEnumerable<FundingCalculation> CreateCalculations()
        {
            return new[]
            {
                new FundingCalculation { TemplateCalculationId = 1, Value = 123 },
                new FundingCalculation { TemplateCalculationId = 2, Value = 456 },
                new FundingCalculation { TemplateCalculationId = 3, Value = 789 }
            };
        }

        private static IEnumerable<FundingReferenceData> CreateReferenceData()
        {
            return new[]
            {
                new FundingReferenceData { TemplateReferenceId = 1, Value = 123 },
                new FundingReferenceData { TemplateReferenceId = 2, Value = 456 },
                new FundingReferenceData { TemplateReferenceId = 3, Value = 789 }
            };
        }

        private static PublishedProviderVersion CreateProviderVersion()
        {
            return new PublishedProviderVersion
            {
                FundingLines = CreateFundingLines(),
                Calculations = CreateCalculations(),
                ReferenceData = CreateReferenceData(),
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
                VariationReasons = new List<Models.Publishing.VariationReason> { Models.Publishing.VariationReason.NameFieldUpdated, Models.Publishing.VariationReason.FundingUpdated }
            };
        }

        private static Common.ApiClient.Providers.Models.Provider CreateProvider()
        {
            return new Common.ApiClient.Providers.Models.Provider
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
    }
}
