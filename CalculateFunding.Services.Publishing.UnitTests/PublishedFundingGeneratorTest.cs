using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Schema10;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using PublishingModels = CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingGeneratorTest
    {
        private PublishedFundingGenerator _publishedFundingGenerator;
        private IMapper _mapper;
        private PublishingModels.PublishedFunding _publishedFunding;
        private OrganisationGroupResult _organisationGroupResult;
        private const string _schema = "1.0";
        private const string _templateVersion = "1";
        private const string _fundingStreamId = "stream1";
        private string _publishedFundingPeriodId;
        private Common.TemplateMetadata.Models.TemplateMetadataContents _templateMetadataContents;
        private PublishingModels.PublishedProvider _publishedProvider;
        private PublishingModels.Provider _provider;
        private PublishingModels.PublishedProvider _publishedProvider2;
        private PublishingModels.Provider _provider2;
        private IEnumerable<(PublishingModels.PublishedFunding PublishedFunding, PublishingModels.PublishedFundingVersion PublishedFundingVersion)> _publishedFundingAndPublishedFundingVersion;
        private IEnumerable<Common.ApiClient.Providers.Models.Provider> _scopedProviders;
        private IPublishedFundingIdGenerator _publishedFundingIdGenerator;
        private Reference _fundingStreamObject;
        private Common.ApiClient.Policies.Models.FundingPeriod _fundingPeriod;
        private Reference _fundingStream;
        private PublishedFundingDates _fundingPublishingDates;
        private string _specificationId;

        [TestInitialize]
        public void SetUp()
        {
            _mapper = new MapperConfiguration(c =>
            {
                c.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper();

            IPublishedFundingIdGeneratorResolver publishedFundingIdGeneratorResolver = Substitute.For<IPublishedFundingIdGeneratorResolver>();

            _publishedFundingGenerator = new PublishedFundingGenerator(_mapper, publishedFundingIdGeneratorResolver);

            ILogger logger = Substitute.For<ILogger>();

            ITemplateMetadataGenerator templateMetaDataGenerator = new TemplateMetadataGenerator(logger);

            _templateMetadataContents = templateMetaDataGenerator.GetMetadata(GetResourceString("CalculateFunding.Services.Publishing.UnitTests.Resources.exampleProviderTemplate1.json"));

            _publishedFundingIdGenerator = Substitute.For<IPublishedFundingIdGenerator>();

            publishedFundingIdGeneratorResolver.GetService(Arg.Is(_templateMetadataContents.SchemaVersion))
                .Returns(_publishedFundingIdGenerator);

            _publishedFunding = Substitute.For<PublishingModels.PublishedFunding>();

            _scopedProviders = GenerateScopedProviders();
        }

        [TestMethod]
        public void GeneratePublishedFunding_GivenPublishedFundingOrganisationResultAndTemplate_ReturnsPublishedFundingVersion()
        {
            // Arrange
            GivenOrganisationGroupResult();

            GivenPublishedProvider();

            GivenFundingIdSet();

            GivenFundingStreamSet();

            GivenFundingPeriodSet();

            GivenPublishingDatesSet();

            GivenSpecificationIdIsSet();

            // Act
            WhenPublishedFundingGenerated();

            // Assert
            _publishedFundingAndPublishedFundingVersion.Count()
                .Should()
                .Be(1);

            _publishedFundingAndPublishedFundingVersion.First().PublishedFundingVersion.FundingId
                .Should()
                .Be($"{_organisationGroupResult.GroupTypeIdentifier}_{_organisationGroupResult.IdentifierValue}_{_publishedProvider.Current.FundingPeriodId}_{_publishedProvider.Current.FundingStreamId}_{1}");

            _publishedFundingAndPublishedFundingVersion.First().PublishedFundingVersion.FundingLines.First().DistributionPeriods.First().Value
                .Should()
                .Be(150);

            _publishedFundingAndPublishedFundingVersion.First().PublishedFundingVersion.FundingLines.First().DistributionPeriods.First().ProfilePeriods.First().ProfiledValue
                .Should()
                .Be(300);

        }

        private void GivenSpecificationIdIsSet()
        {
            _specificationId = "specId1";
        }

        private void GivenPublishingDatesSet()
        {
            DateTime dateTime = DateTime.Now;
            _fundingPublishingDates = new PublishedFundingDates()
            {
                StatusChangedDate = dateTime,
                EarliestPaymentAvailableDate = dateTime,
                ExternalPublicationDate = dateTime,
            };
        }

        private void GivenFundingPeriodSet()
        {
            _fundingPeriod = new Common.ApiClient.Policies.Models.FundingPeriod()
            {
                Id = "AY-1920",
                EndDate = new DateTimeOffset(2019, 12, 12, 0, 0, 0, TimeSpan.Zero),
                StartDate = new DateTimeOffset(2019, 12, 1, 0, 0, 0, TimeSpan.Zero),
                Name = "Funding Period Test",
                Period = "1920",
                Type = Common.ApiClient.Policies.Models.FundingPeriodType.AY,
            };
        }

        private void GivenFundingStreamSet()
        {
            _fundingStream = new Reference("FS-ID", "Funding Stream Name");

        }

        private void GivenFundingIdSet()
        {
            _publishedFundingIdGenerator.GetFundingId(Arg.Any<PublishedFundingVersion>())
                .Returns($"{_organisationGroupResult.GroupTypeIdentifier}_{_organisationGroupResult.IdentifierValue}_{_publishedProvider.Current.FundingPeriodId}_{_publishedProvider.Current.FundingStreamId}_{1}");
        }

        private void GivenOrganisationGroupResult()
        {
            _organisationGroupResult = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithIdentifierValue("101")
            .WithIdentifiers(new List<OrganisationIdentifier> { new OrganisationIdentifier { Type = OrganisationGroupTypeIdentifier.LACode, Value = "101" } })
            .WithProviders(_scopedProviders.Where(p => p.TrustCode == "101")));
        }

        private void GivenPublishedProvider()
        {
            _publishedFundingPeriodId = "AY-1920";

            _provider = new PublishingModels.Provider { ProviderId = "provider1" };

            _publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(_publishedFundingPeriodId)
            .WithFundingLines(NewFundingLine(fl => fl.WithTemplateLineId(1).WithValue(0)
                .WithDistributionPeriods(new DistributionPeriod[] { new DistributionPeriod { DistributionPeriodId = _publishedFundingPeriodId, ProfilePeriods = new ProfilePeriod[] { new ProfilePeriod { TypeValue = "April", DistributionPeriodId = _publishedFundingPeriodId, ProfiledValue = 200, Type = ProfilePeriodType.CalendarMonth, Year = 2019 } }, Value = 100 } })))
            .WithFundingStreamId(_fundingStreamId)
            .WithProviderId(_provider.ProviderId)
            .WithProvider(_provider))));

            _provider2 = new PublishingModels.Provider { ProviderId = "provider2" };

            _publishedProvider2 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(_publishedFundingPeriodId)
            .WithFundingLines(NewFundingLine(fl => fl.WithTemplateLineId(1).WithValue(0)
                .WithDistributionPeriods(new DistributionPeriod[] { new DistributionPeriod { DistributionPeriodId = _publishedFundingPeriodId, ProfilePeriods = new ProfilePeriod[] { new ProfilePeriod { TypeValue = "April", DistributionPeriodId = _publishedFundingPeriodId, ProfiledValue = 100, Type = ProfilePeriodType.CalendarMonth, Year = 2019 } }, Value = 50 } })))
            .WithFundingStreamId(_fundingStreamId)
            .WithProviderId(_provider2.ProviderId)
            .WithProvider(_provider2))));
        }

        private void WhenPublishedFundingGenerated()
        {
            _publishedFundingAndPublishedFundingVersion = _publishedFundingGenerator.GeneratePublishedFunding(
                new GeneratePublishedFundingInput()
                {
                    OrganisationGroupsToSave = new List<(PublishingModels.PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> { (_publishedFunding, _organisationGroupResult) },
                    TemplateMetadataContents = _templateMetadataContents,
                    PublishedProviders = new List<PublishingModels.PublishedProvider> { _publishedProvider, _publishedProvider2 }, //TODO; this also needs the second item in the list
                    TemplateVersion = _templateVersion,
                    FundingPeriod = _fundingPeriod,
                    FundingStream = _fundingStream,
                    PublishingDates = _fundingPublishingDates,
                    SpecificationId = _specificationId,
                }).ToArray();
        }

        private OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }

        private PublishingModels.PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private PublishingModels.PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private IEnumerable<Common.ApiClient.Providers.Models.Provider> GenerateScopedProviders()
        {
            List<Common.ApiClient.Providers.Models.Provider> providers = new List<Common.ApiClient.Providers.Models.Provider>();

            providers.Add(new Common.ApiClient.Providers.Models.Provider()
            {
                ProviderId = "provider1",
                Name = "Provider 1",
                UKPRN = "1001",
                LACode = "101",
                Authority = "Local Authority 1",
                TrustCode = "101",
                TrustName = "Academy Trust 1",
                ParliamentaryConstituencyCode = "BOS",
                ParliamentaryConstituencyName = "Bermondsey and Old Southwark",
                MiddleSuperOutputAreaCode = "MSOA1",
                MiddleSuperOutputAreaName = "Middle Super Output Area 1",
                CensusWardCode = "CW1",
                CensusWardName = "Census Ward 1",
                DistrictCode = "D1",
                DistrictName = "District 1",
                GovernmentOfficeRegionCode = "GOR1",
                GovernmentOfficeRegionName = "Government Office Region 1",
                LowerSuperOutputAreaCode = "LSOA1",
                LowerSuperOutputAreaName = "Lower Super Output Area 1",
                WardCode = "W1",
                WardName = "Ward 1",
                RscRegionCode = "RSC1",
                RscRegionName = "Rsc Region 1",
                CountryCode = "C1",
                CountryName = "Country 1",
                ProviderType = "ProviderType",
                ProviderSubType = "ProviderSubType"
            });

            providers.Add(new Common.ApiClient.Providers.Models.Provider()
            {
                ProviderId = "provider2",
                Name = "Provider 2",
                UKPRN = "1002",
                LACode = "102",
                Authority = "Local Authority 2",
                TrustCode = "101",
                TrustName = "Academy Trust 2",
                ParliamentaryConstituencyCode = "BOS",
                ParliamentaryConstituencyName = "Bermondsey and Old Southwark",
                MiddleSuperOutputAreaCode = "MSOA1",
                MiddleSuperOutputAreaName = "Middle Super Output Area 1",
                CensusWardCode = "CW1",
                CensusWardName = "Census Ward 1",
                DistrictCode = "D1",
                DistrictName = "District 1",
                GovernmentOfficeRegionCode = "GOR1",
                GovernmentOfficeRegionName = "Government Office Region 1",
                LowerSuperOutputAreaCode = "LSOA1",
                LowerSuperOutputAreaName = "Lower Super Output Area 1",
                WardCode = "W1",
                WardName = "Ward 1",
                RscRegionCode = "RSC1",
                RscRegionName = "Rsc Region 1",
                CountryCode = "C1",
                CountryName = "Country 1",
                ProviderType = "ProviderType",
                ProviderSubType = "ProviderSubType"
            });

            providers.Add(new Common.ApiClient.Providers.Models.Provider()
            {
                ProviderId = "provider3",
                Name = "Provider 3",
                UKPRN = "1003",
                LACode = "102",
                Authority = "Local Authority 2",
                TrustCode = "102",
                TrustName = "Academy Trust 2",
                ParliamentaryConstituencyCode = "CA",
                ParliamentaryConstituencyName = "Camden",
                MiddleSuperOutputAreaCode = "MSOA2",
                MiddleSuperOutputAreaName = "Middle Super Output Area 2",
                CensusWardCode = "CW2",
                CensusWardName = "Census Ward 2",
                DistrictCode = "D2",
                DistrictName = "District 2",
                GovernmentOfficeRegionCode = "GOR2",
                GovernmentOfficeRegionName = "Government Office Region 2",
                LowerSuperOutputAreaCode = "LSOA2",
                LowerSuperOutputAreaName = "Lower Super Output Area 2",
                WardCode = "W2",
                WardName = "Ward 2",
                RscRegionCode = "RSC2",
                RscRegionName = "Rsc Region 2",
                CountryCode = "C2",
                CountryName = "Country 2",
                ProviderType = "ProviderType",
                ProviderSubType = "ProviderSubType"
            });

            providers.Add(new Common.ApiClient.Providers.Models.Provider()
            {
                ProviderId = "provider4",
                Name = "Provider 4",
                UKPRN = "1004",
                LACode = "103",
                TrustCode = "103",
                TrustName = "Academy Trust 3",
                Authority = "Local Authority 3",
                DistrictCode = "D2",
                DistrictName = "District 2",
                ProviderType = "ProviderType",
                ProviderSubType = "ProviderSubType"
            });

            providers.Add(new Common.ApiClient.Providers.Models.Provider()
            {
                ProviderId = "provider5",
                Name = "Provider 5",
                UKPRN = "1004",
                LACode = "103",
                TrustCode = "103",
                TrustName = "Academy Trust 3",
                Authority = "Local Authority 3",
                DistrictCode = "D2",
                DistrictName = "District 2",
                ProviderType = "ProviderType2",
                ProviderSubType = "ProviderSubType2"
            });

            return providers;
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(PublishedFundingGeneratorTest)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }
    }
}
