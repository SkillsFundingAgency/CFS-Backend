using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedFundingChangeDetectorServiceTests
    {
        private PublishedFundingChangeDetectorService _prerequisites;

        [TestInitialize]
        public void SetUp()
        {
            _prerequisites = new PublishedFundingChangeDetectorService();
        }

        [TestMethod]
        public void GenerateOrganisationGroupsToSave_GivenOrganisationGroupsUnchanged_NoGroupResultsReturned()
        {
            IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders = GenerateScopedProviders();

            OrganisationGroupResult organisationGroupResult1 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("101").WithProviders(scopedProviders.Where(p => p.TrustCode == "101")));
            OrganisationGroupResult organisationGroupResult2 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("102").WithProviders(scopedProviders.Where(p => p.TrustCode == "102")));
            
            PublishedFundingPeriod publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };

            PublishedFunding publishedFunding1 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding1")
            .WithProviderFundings(new string[] { "provider1-AY-123-PSG-1_0", "provider2-AY-123-PSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("PSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("101"))));

            PublishedFunding publishedFunding2 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding2")
            .WithProviderFundings(new string[] { "provider3-AY-123-DSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("DSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("102"))));

            PublishedProvider PublishedProvider1 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("PSG")
            .WithProviderId("provider1"))));

            PublishedProvider PublishedProvider2 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("PSG")
            .WithProviderId("provider2"))));

            PublishedProvider PublishedProvider3 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("DSG")
            .WithProviderId("provider3"))));

            IEnumerable<(PublishedFunding, OrganisationGroupResult)> results = _prerequisites.GenerateOrganisationGroupsToSave(new OrganisationGroupResult[] { organisationGroupResult1, organisationGroupResult2 },
                new PublishedFunding[] { publishedFunding1, publishedFunding2 },
                new PublishedProvider[] { PublishedProvider1, PublishedProvider2, PublishedProvider3 }
            );

            results.Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        public void GenerateOrganisationGroupsToSave_GivenOrganisationGroupsWithMissingPublishedProviders_GroupResultsReturned()
        {
            // Arrange
            IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders = GenerateScopedProviders();

            OrganisationGroupResult organisationGroupResult1 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("101").WithProviders(scopedProviders.Where(p => p.TrustCode == "101")));
            OrganisationGroupResult organisationGroupResult2 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("102").WithProviders(scopedProviders.Where(p => p.TrustCode == "102")));
            OrganisationGroupResult organisationGroupResult3 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("103").WithProviders(scopedProviders.Where(p => p.TrustCode == "103")));

            PublishedFundingPeriod publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };

            PublishedFunding publishedFunding1 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding1")
            .WithProviderFundings(new string[] { "provider1-AY-123-DSG-1_0", "provider2-AY-123-DSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("DSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("101"))));

            PublishedFunding publishedFunding2 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding2")
            .WithProviderFundings(new string[] { "provider3-AY-123-PSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("PSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("102"))));

            PublishedFunding publishedFunding3 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding3")
            .WithProviderFundings(new string[] { "provider4-AY-123-PES-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("PES")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("103"))));

            PublishedProvider PublishedProvider1 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("DSG")
            .WithProviderId("provider1"))));

            PublishedProvider PublishedProvider2 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("DSG")
            .WithProviderId("provider2"))));

            PublishedProvider PublishedProvider3 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("PSG")
            .WithProviderId("provider3"))));

            // Act
            IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> results = _prerequisites.GenerateOrganisationGroupsToSave(new OrganisationGroupResult[] { organisationGroupResult1, organisationGroupResult2, organisationGroupResult3 },
                new PublishedFunding[] { publishedFunding1, publishedFunding2, publishedFunding3 },
                new PublishedProvider[] { PublishedProvider1, PublishedProvider2, PublishedProvider3 }
            );

            // Assert
            results.Count()
                .Should()
                .Be(1);

            results.First().OrganisationGroupResult.IdentifierValue
                .Should()
                .Be("103");

            results.First().PublishedFunding.Current.FundingId
                .Should()
                .Be("funding3");
        }

        [TestMethod]
        public void GenerateOrganisationGroupsToSave_GivenOrganisationGroupsWithPublishedProviderChangingFundingId_GroupResultsReturned()
        {
            // Arrange
            IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders = GenerateScopedProviders();

            OrganisationGroupResult organisationGroupResult1 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("101").WithProviders(scopedProviders.Where(p => p.TrustCode == "101")));
            OrganisationGroupResult organisationGroupResult2 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("102").WithProviders(scopedProviders.Where(p => p.TrustCode == "102")));
            OrganisationGroupResult organisationGroupResult3 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("103").WithProviders(scopedProviders.Where(p => p.TrustCode == "103" && p.ProviderType == "ProviderType")));

            PublishedFundingPeriod publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };

            PublishedFunding publishedFunding1 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding1")
            .WithProviderFundings(new string[] { "provider1-AY-123-DSG-1_0", "provider2-AY-123-DSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("DSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("101"))));

            PublishedFunding publishedFunding2 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding2")
            .WithProviderFundings(new string[] { "provider3-AY-123-PSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("PSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("102"))));

            PublishedFunding publishedFunding3 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding3")
            .WithProviderFundings(new string[] { "PES-AY-123-provider4-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("PES")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("103"))));

            PublishedProvider PublishedProvider1 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("DSG")
            .WithProviderId("provider1"))));

            PublishedProvider PublishedProvider2 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("DSG")
            .WithProviderId("provider2"))));

            PublishedProvider PublishedProvider3 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("PSG")
            .WithProviderId("provider3"))));

            PublishedProvider PublishedProvider4 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("PES")
            .WithProviderId("provider4")
            .WithMinorVersion(1))));

            // Act
            IEnumerable<(PublishedFunding PublishedFunding, OrganisationGroupResult OrganisationGroupResult)> results = _prerequisites.GenerateOrganisationGroupsToSave(new OrganisationGroupResult[] { organisationGroupResult1, organisationGroupResult2, organisationGroupResult3 },
                new PublishedFunding[] { publishedFunding1, publishedFunding2, publishedFunding3 },
                new PublishedProvider[] { PublishedProvider1, PublishedProvider2, PublishedProvider3, PublishedProvider4 }
            );

            // Assert
            results.Count()
                .Should()
                .Be(1);

            results.First().OrganisationGroupResult.IdentifierValue
                .Should()
                .Be("103");

            results.First().PublishedFunding.Current.FundingId
                .Should()
                .Be("funding3");
        }



        [TestMethod]
        public void GenerateOrganisationGroupsToSave_GivenOrganisationGroupsWithPublishedProviderAdded_GroupResultsReturned()
        {
            // Arrange
            IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProviders = GenerateScopedProviders();

            OrganisationGroupResult organisationGroupResult1 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("101").WithProviders(scopedProviders.Where(p => p.TrustCode == "101")));
            OrganisationGroupResult organisationGroupResult2 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("102").WithProviders(scopedProviders.Where(p => p.TrustCode == "102")));
            OrganisationGroupResult organisationGroupResult3 = NewOrganisationGroupResult(_ => _.WithGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust).WithGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode).WithIdentifierValue("103").WithProviders(scopedProviders.Where(p => p.TrustCode == "103" && p.ProviderType == "ProviderType")));

            PublishedFundingPeriod publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };

            PublishedFunding publishedFunding1 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding1")
            .WithProviderFundings(new string[] { "provider1-AY-123-DSG-1_0", "provider2-AY-123-DSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("DSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("101"))));

            PublishedFunding publishedFunding2 = NewPublishedFunding(_ => _.WithCurrent(NewPublishedFundingVersion(version => version.WithFundingId("funding1")
            .WithProviderFundings(new string[] { "provider3-AY-123-PSG-1_0" })
            .WithFundingPeriod(publishedFundingPeriod)
            .WithFundingStreamId("PSG")
            .WithOrganisationGroupTypeClassification(OrganisationGroupTypeClassification.LegalEntity)
            .WithOrganisationGroupTypeIdentifier(OrganisationGroupTypeIdentifier.AcademyTrustCode)
            .WithOrganisationGroupTypeCode(OrganisationGroupTypeCode.AcademyTrust)
            .WithOrganisationGroupIdentifierValue("102"))));

            PublishedProvider PublishedProvider1 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("DSG")
            .WithProviderId("provider1"))));

            PublishedProvider PublishedProvider2 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("DSG")
            .WithProviderId("provider2"))));

            PublishedProvider PublishedProvider3 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("PSG")
            .WithProviderId("provider3"))));

            PublishedProvider PublishedProvider4 = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(publishedFundingPeriod.Id)
            .WithFundingStreamId("PES")
            .WithProviderId("provider4"))));

            // Act
            IEnumerable<(PublishedFunding, OrganisationGroupResult)> results = _prerequisites.GenerateOrganisationGroupsToSave(new OrganisationGroupResult[] { organisationGroupResult1, organisationGroupResult2, organisationGroupResult3 },
                new PublishedFunding[] { publishedFunding1, publishedFunding2},
                new PublishedProvider[] { PublishedProvider1, PublishedProvider2, PublishedProvider3, PublishedProvider4}
            );

            // Assert
            results.Count()
                .Should()
                .Be(1);

            results.First().Item2.IdentifierValue
                .Should()
                .Be("103");

            results.First().Item1
                .Should()
                .Be(null);
        }

        private OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }

        private PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        private PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }
        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
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
    }
}
