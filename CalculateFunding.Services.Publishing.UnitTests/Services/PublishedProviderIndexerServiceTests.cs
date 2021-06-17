using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Processing.Functions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Cosmos.Core.Networking;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedProviderIndexerServiceTests
    {
        private ResiliencePolicies _resiliencePolicies;
        private Mock<IPoliciesApiClient> _policies;

        [TestInitialize]
        public void SetUp()
        {
            _policies = new Mock<IPoliciesApiClient>();
        }

        [TestMethod]
        public void IndexPublishedProvider_GivenANullDefinitionPublishedProviderSupplied_LogsAndThrowsException()
        {
            //Arrange

            ILogger logger = CreateLogger();

            PublishedProviderIndexerService publishedProviderIndexerService = CreatePublishedProviderIndexerService(logger: logger);

            //Act
            Func<Task> test = () => publishedProviderIndexerService.IndexPublishedProvider(null);

            //Assert
            test
               .Should()
               .ThrowExactly<NonRetriableException>();

            logger
                .Received(1)
                .Error("Null published provider version supplied");
        }

        [TestMethod]
        public void IndexPublishedProvider_GivenSearchRepositoryCausesException_LogsAndThrowsException()
        {
            //Arrange
            const string errorMessage = "Encountered error 802 code";
            PublishedProviderVersion publishedProviderVersion = new PublishedProviderVersion
            {
                Provider = GetProvider(1),
                ProviderId = "1234",
                FundingStreamId = "PSG",
                FundingPeriodId = "AY-1920",
                Version = 1,
                MajorVersion = 1,
                MinorVersion = 0,
                VariationReasons = new List<VariationReason> { VariationReason.NameFieldUpdated, VariationReason.FundingUpdated }
            };

            ILogger logger = CreateLogger();
            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<IEnumerable<PublishedProviderIndex>>())
                .Returns(new[] { new IndexError() { ErrorMessage = errorMessage } });

            PublishedProviderIndexerService publishedProviderIndexerService = CreatePublishedProviderIndexerService(logger: logger,
                                                                                    searchRepository: searchRepository);

            //Act
            Func<Task> test = () => publishedProviderIndexerService.IndexPublishedProvider(publishedProviderVersion);

            //Assert
            test
               .Should()
               .ThrowExactly<RetriableException>();

            logger
                .Received(1)
                .Error($"Could not index Published Providers because: {errorMessage}");
        }

        [TestMethod]
        [DataRow(true, false, "Hide indicative allocations", "20 September 2021", "1 September 2021", "28 September 2022", "September 2021")]
        [DataRow(false, true, "Only indicative allocations",  "20 August 2021", "1 September 2021", "28 September 2021", "August 2021")]
        [DataRow(false, true, "Only indicative allocations", "20 July 2021", "1 February 2022", "28 February 2022", "Any other")]
        [DataRow(false, true, "Only indicative allocations", null, "1 February 2022", "28 February 2022", "Any other")]
        public async Task IndexPublishedProvider_GivenSearchRepository_NoExceptionThrownAsync(bool hasErrors,
            bool isIndicative,
            string indicativeIndexText,
            string dateOpenedLiteral,
            string fundingPeriodStartLiteral,
            string fundingPeriodEndLiteral,
            string expectedMonthYearOpen)
        {
            //Arrange           
            PublishedProviderVersion publishedProviderVersion = new PublishedProviderVersion
            {
                Provider = GetProvider(1),
                ProviderId = "1234",
                FundingStreamId = "PSG",
                FundingPeriodId = "AY-1920",
                Version = 1,
                MajorVersion = 1,
                MinorVersion = 0,
                IsIndicative = isIndicative,
                VariationReasons = new List<VariationReason> { VariationReason.NameFieldUpdated, VariationReason.FundingUpdated },
                Errors = hasErrors ? new List<PublishedProviderError>
                {
                    new PublishedProviderError
                    {
                        SummaryErrorMessage = "summary error message"
                    }
                } : null
            };

            publishedProviderVersion.Provider.DateOpened = dateOpenedLiteral?.ToDateTimeOffset();

            ILogger logger = CreateLogger();
            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();


            PublishedProviderIndexerService publishedProviderIndexerService = CreatePublishedProviderIndexerService(logger: logger,
                                                                                    searchRepository: searchRepository);
            
            GivenTheFundingPeriod(publishedProviderVersion.FundingPeriodId, NewFundingPeriod(_ 
                => _.WithStartDate(fundingPeriodStartLiteral.ToDateTimeOffset())
                .WithEndDate(fundingPeriodEndLiteral.ToDateTimeOffset())));

            //Act
            await publishedProviderIndexerService.IndexPublishedProvider(publishedProviderVersion);

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<PublishedProviderIndex>>(
                    _ => MatchesExpectedPublishedProviderIndex(hasErrors, indicativeIndexText, _, publishedProviderVersion, expectedMonthYearOpen)
              ));
        }

        private static bool MatchesExpectedPublishedProviderIndex(bool hasErrors,
            string indicativeIndexText,
            IEnumerable<PublishedProviderIndex> d,
            PublishedProviderVersion publishedProviderVersion,
            string expectedMonthYearOpened)
        {
            Provider provider = publishedProviderVersion.Provider;
            PublishedProviderIndex publishedProviderIndex = d.First();
            
            return publishedProviderIndex.Id == publishedProviderVersion.PublishedProviderId &&
                   publishedProviderIndex.ProviderType == provider.ProviderType &&
                   publishedProviderIndex.ProviderSubType == provider.ProviderSubType &&
                   publishedProviderIndex.LocalAuthority == provider.Authority &&
                   publishedProviderIndex.FundingStatus == publishedProviderVersion.Status.ToString() &&
                   publishedProviderIndex.ProviderName == provider.Name &&
                   publishedProviderIndex.UKPRN == provider.UKPRN &&
                   publishedProviderIndex.UPIN == provider.UPIN &&
                   publishedProviderIndex.URN == provider.URN &&
                   publishedProviderIndex.FundingValue == Convert.ToDouble(publishedProviderVersion.TotalFunding) &&
                   publishedProviderIndex.SpecificationId == publishedProviderVersion.SpecificationId &&
                   publishedProviderIndex.FundingStreamId == "PSG" &&
                   publishedProviderIndex.FundingPeriodId == publishedProviderVersion.FundingPeriodId &&
                   publishedProviderIndex.Indicative == indicativeIndexText &&
                   publishedProviderIndex.HasErrors == publishedProviderVersion.HasErrors &&
                   publishedProviderIndex.Errors.Any() == publishedProviderVersion.HasErrors &&
                   (!hasErrors || publishedProviderIndex.Errors.First() == "summary error message") &&
                   publishedProviderIndex.DateOpened == provider.DateOpened &&
                   publishedProviderIndex.MonthYearOpened == expectedMonthYearOpened;
        }

        private Provider GetProvider(int index)
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
                UPIN = "123454",
                DateOpened = new RandomDateTime(),
                DateClosed = null,
                Status = "Open",
                PhaseOfEducation = "Secondary",
                Authority = "Camden",
                // LocalAuthorityName = "Camden",
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

        private void GivenTheFundingPeriod(string fundingPeriodId,
            FundingPeriod fundingPeriod)
            => _policies.Setup(_ => _.GetFundingPeriodById(fundingPeriodId))
                .ReturnsAsync(new ApiResponse<FundingPeriod>(HttpStatusCode.OK, fundingPeriod));

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private PublishedProviderIndexerService CreatePublishedProviderIndexerService(ILogger logger = null,
            ISearchRepository<PublishedProviderIndex> searchRepository = null)
        {
            _resiliencePolicies = new ResiliencePolicies()
            {
                PublishedProviderSearchRepository = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync()
            };

            IConfiguration configuration = Substitute.For<IConfiguration>();

            return new PublishedProviderIndexerService(
                logger ?? CreateLogger(),
                searchRepository ?? CreateSearchRepository(),
                _resiliencePolicies,
                new PublishingEngineOptions(configuration),
                _policies.Object);
        }

        private ISearchRepository<PublishedProviderIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<PublishedProviderIndex>>();
        }

        private FundingPeriod NewFundingPeriod(Action<ApiFundingPeriodBuilder> setUp = null)
        {
            ApiFundingPeriodBuilder apiFundingPeriodBuilder = new ApiFundingPeriodBuilder();

            setUp?.Invoke(apiFundingPeriodBuilder);
            
            return apiFundingPeriodBuilder.Build();
        }
    }
}
