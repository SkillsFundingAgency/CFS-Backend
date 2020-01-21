using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using DistributionPeriod = CalculateFunding.Models.Publishing.DistributionPeriod;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using ProfilePeriod = CalculateFunding.Models.Publishing.ProfilePeriod;
using Provider = CalculateFunding.Models.Publishing.Provider;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderDataPopulatorTests
    {
        //Apart from CopiesVariationReasonsWhenPopulatingPublishedProviderVersion
        //I don't see any tests here that actually check the internal side effects of this method (of which there are many)
        //it's only ever testing the output - it's not really providing much assurance currently.

        private PublishedProviderVersion _publishedProviderVersion;
        private PublishedProviderVersion _publishedProviderVersionForMapping;
        private GeneratedProviderResult _generatedProviderResult;
        private ApiProvider _provider;
        private ProviderVariationResult _providerVariationResult;

        private IMapper _mapper;
        private ILogger _logger;
        private PublishedProviderDataPopulator _publishedProviderDataPopulator;
        private string _templateVersion;

        [TestInitialize]
        public void SetUp()
        {
            _templateVersion = NewRandomString();
            _publishedProviderVersion = CreateProviderVersion(_templateVersion);
            _generatedProviderResult = CreateGeneratedProviderResult();
            _provider = CreateProvider();
            _publishedProviderVersionForMapping = (PublishedProviderVersion)_publishedProviderVersion.Clone();

            _mapper = CreateMapper();
            _mapper
                .Map<Provider>(_provider)
                .Returns(_publishedProviderVersionForMapping.Provider);

            _logger = CreateLogger();

            _providerVariationResult = NewProviderVariationResult();
            
            _publishedProviderDataPopulator = new PublishedProviderDataPopulator(_mapper, _logger);
        }

        [TestMethod]
        public void CopiesVariationReasonsWhenPopulatingPublishedProviderVersion()
        {
            VariationReason[] expectedVariationReasons = new[]
            {
                NewRandomVariationReason(), 
                NewRandomVariationReason(), 
                NewRandomVariationReason()
            };
            
            _providerVariationResult.VariationReasons = expectedVariationReasons;

            WhenThePublishedProviderIsUpdated();

            _publishedProviderVersion.VariationReasons
                .Should()
                .BeEquivalentTo(expectedVariationReasons);
        }

        private VariationReason NewRandomVariationReason() => new RandomEnum<VariationReason>();

        [TestMethod]
        public void UpdatePublishedProvider_GivenTemplateVersionChange_ReturnsTrue()
        {
            string initialTemplateVersion = _publishedProviderVersion.TemplateVersion.ToString();
            string newTemplateVersion = NewRandomString();
            _publishedProviderVersion.TemplateVersion = newTemplateVersion;

            bool result = WhenThePublishedProviderIsUpdated();
            
            result
                .Should()
                .BeTrue();

            _logger
                .Received(1)
                .Information($"changes for new published provider version : {_publishedProviderVersion.Id} : [\"TemplateVersion: {newTemplateVersion} != {initialTemplateVersion}\"]");
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenNoChanges_ReturnsFalse()
        {
            _publishedProviderVersionForMapping.Provider.ProviderVersionId = "1";
            
            bool result = WhenThePublishedProviderIsUpdated();

            result
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenChangedProvider_ReturnsTrue()
        {
            string initialProviderName = _publishedProviderVersionForMapping.Provider.Name;
            string newProviderName = "NewName";
            _publishedProviderVersionForMapping.Provider.Name = newProviderName;

            bool result = WhenThePublishedProviderIsUpdated();

            result
                .Should()
                .BeTrue();
            
            _logger
                 .Received(1)
                 .Information($"changes for new published provider version : {_publishedProviderVersion.Id} : [\"Provider: Name: {initialProviderName} != {newProviderName}\"]");
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenFundingLineChanges_ReturnsFalse()
        {
            _generatedProviderResult.FundingLines.First().Name = "New Name";

            bool result = WhenThePublishedProviderIsUpdated();
            
            //NB - test title says should return false but assertion is returns true

            result
                .Should()
                .BeTrue();

            _logger
                 .Received(1)
                 .Information($"changes for new published provider version : {_publishedProviderVersion.Id} : [\"FundingLine:{_generatedProviderResult.FundingLines.First().FundingLineCode}\"]");
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenCalculationChanges_ReturnsFalse()
        {
            _generatedProviderResult.Calculations.First().Value = 56;

            bool result = WhenThePublishedProviderIsUpdated();

            //NB - test title says should return false but assertion is returns true

            result
                .Should()
                .BeTrue();

            _logger
                 .Received(1)
                 .Information($"changes for new published provider version : {_publishedProviderVersion.Id} : [\"Calculation:{_generatedProviderResult.Calculations.First().TemplateCalculationId}\"]");
        }

        [TestMethod]
        public void UpdatePublishedProvider_GivenReferenceDataChanges_ReturnsFalse()
        {
            _generatedProviderResult.ReferenceData.First().Value = 56;

            bool result = WhenThePublishedProviderIsUpdated();

            //NB - test title says should return false but assertion is returns true

            result
                .Should()
                .BeTrue();

            _logger
                 .Received(1)
                 .Information($"changes for new published provider version : {_publishedProviderVersion.Id} : [\"ReferenceData:{_generatedProviderResult.ReferenceData.First().TemplateReferenceId}\"]");
        }

        private bool WhenThePublishedProviderIsUpdated()
        {
            return _publishedProviderDataPopulator.UpdatePublishedProvider(_publishedProviderVersion, 
                _generatedProviderResult, 
                _provider, 
                _templateVersion,
                _providerVariationResult,
                false);
        }

        private ProviderVariationResult NewProviderVariationResult(Action<ProviderVariationResultBuilder> setUp = null)
        {
            ProviderVariationResultBuilder providerVariationResultBuilder = new ProviderVariationResultBuilder();

            setUp?.Invoke(providerVariationResultBuilder);
            
            return providerVariationResultBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
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
                ReferenceData = CreateReferenceData(),
                TotalFunding = 5050000,
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

        private static PublishedProviderVersion CreateProviderVersion(string templateVersion = null)
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
                TemplateVersion = templateVersion ?? NewRandomString(),
                VariationReasons = new List<VariationReason> { VariationReason.NameFieldUpdated, VariationReason.FundingUpdated }
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
