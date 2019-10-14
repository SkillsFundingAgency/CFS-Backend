using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedProviderIndexerServiceTests
    {
        private ResiliencePolicies _resiliencePolicies;

        [TestMethod]
        public void IndexPublishedProvider_GivenANullDefinitionPublishedProviderSupplied_LogsAndThrowsException()
        {
            //Arrange
            PublishedProviderVersion publishedProviderVersion = null;

            ILogger logger = CreateLogger();
            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();

            PublishedProviderIndexerService publishedProviderIndexerService = CreatePublishedProviderIndexerService(logger: logger);

            //Act
            Func<Task> test = () => publishedProviderIndexerService.IndexPublishedProvider(publishedProviderVersion);

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
        public async Task IndexPublishedProvider_GivenSearchRepository_NoExceptionThrownAsync()
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
                VariationReasons = new List<VariationReason> { VariationReason.NameFieldUpdated, VariationReason.FundingUpdated }
            };

            ILogger logger = CreateLogger();
            ISearchRepository<PublishedProviderIndex> searchRepository = CreateSearchRepository();


            PublishedProviderIndexerService publishedProviderIndexerService = CreatePublishedProviderIndexerService(logger: logger,
                                                                                    searchRepository: searchRepository);

            //Act
            Func<Task> result = async () => await publishedProviderIndexerService.IndexPublishedProvider(publishedProviderVersion);

            //Assert
            result
                .Should().NotThrow();

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<PublishedProviderIndex>>(
                    d => d.First().Id == $"{publishedProviderVersion.ProviderId}-{publishedProviderVersion.FundingPeriodId}-{publishedProviderVersion.FundingStreamId}" &&
                    d.First().ProviderType == publishedProviderVersion.Provider.ProviderType &&
                    d.First().LocalAuthority == publishedProviderVersion.Provider.LocalAuthorityName &&
                    d.First().FundingStatus == publishedProviderVersion.Status.ToString() &&
                    d.First().ProviderName == publishedProviderVersion.Provider.Name &&
                    d.First().UKPRN == publishedProviderVersion.Provider.UKPRN &&
                    d.First().FundingValue == Convert.ToDouble(publishedProviderVersion.TotalFunding) &&
                    d.First().SpecificationId == publishedProviderVersion.SpecificationId &&
                    d.First().FundingStreamId == "PSG" &&
                    d.First().FundingPeriodId == publishedProviderVersion.FundingPeriodId
              ));
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
            };
        }

        protected ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private PublishedProviderIndexerService CreatePublishedProviderIndexerService(ILogger logger = null,
            ISearchRepository<PublishedProviderIndex> searchRepository = null)
        {
            _resiliencePolicies = new ResiliencePolicies()
            {
                BlobClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                FundingFeedSearchRepository = Policy.NoOpAsync(),
                JobsApiClient = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                ProvidersApiClient = Policy.NoOpAsync(),
                PublishedFundingBlobRepository = Policy.NoOpAsync(),
                PublishedFundingRepository = Policy.NoOpAsync(),
                PublishedProviderVersionRepository = Policy.NoOpAsync(),
                CalculationResultsRepository = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                PublishedProviderSearchRepository = Policy.NoOpAsync(),
            };

            return new PublishedProviderIndexerService(
                logger ?? CreateLogger(),
                searchRepository ?? CreateSearchRepository(),
                _resiliencePolicies,
                new PublishingEngineOptions()
                );
        }

        protected ISearchRepository<PublishedProviderIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<PublishedProviderIndex>>();
        }
    }
}
