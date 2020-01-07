using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Models.Providers;

using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using TrustStatus = CalculateFunding.Models.ProviderLegacy.TrustStatus;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderServiceTests
    {
        [TestMethod]
        public async Task PopulateProviderSummariesForSpecification_GivenSpecificationWithProviderVersionId_TotalCountOfProvidersReturned()
        {
            //Arrange
            string specificationId = Guid.NewGuid().ToString();
            string providerVersionId = Guid.NewGuid().ToString();
            string cacheKeyForList = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            Provider provider = CreateProvider();

            ProviderVersion providerVersion = new ProviderVersion
            {
                Providers = new List<Provider> { provider }
            };

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = specificationId,
                ProviderVersionId = providerVersionId
            };

            ISpecificationsApiClientProxy specificationsApiClientProxy = CreateSpecificationsApiClientProxy();
            specificationsApiClientProxy
                .GetAsync<SpecificationSummary>(Arg.Any<string>())
                .Returns(specificationSummary);

            IProviderVersionService providerVersionService = CreateProviderVersionService();
            providerVersionService
                .GetProvidersByVersion(Arg.Is(providerVersionId))
                .Returns(providerVersion);

            IResultsApiClient resultsApiClient = CreateResultsApiClient();
            ApiResponse<IEnumerable<string>> scopedProviderResponse = new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, new List<string> { { "1234" } });

            resultsApiClient
                .GetScopedProviderIdsBySpecificationId(Arg.Any<string>())
                .Returns(scopedProviderResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IScopedProvidersService providerService = CreateProviderService(resultsApiClient: resultsApiClient, specificationsApiClient: specificationsApiClientProxy, providerVersionService: providerVersionService, cacheProvider: cacheProvider);

            //Act
            IActionResult totalCountResult = await providerService.PopulateProviderSummariesForSpecification(specificationId);

            await specificationsApiClientProxy
                .Received(1)
                .GetAsync<SpecificationSummary>(Arg.Any<string>());

            await providerVersionService
                .Received(1)
                .GetProvidersByVersion(Arg.Is(providerVersionId));

            await cacheProvider
                .Received(1)
                .CreateListAsync<ProviderSummary>(Arg.Is<IEnumerable<ProviderSummary>>(x =>
                    x.First().Id == provider.ProviderId &&
                    x.First().Name == provider.Name &&
                    x.First().ProviderProfileIdType == provider.ProviderProfileIdType &&
                    x.First().UKPRN == provider.UKPRN &&
                    x.First().URN == provider.URN &&
                    x.First().Authority == provider.Authority &&
                    x.First().UPIN == provider.UPIN &&
                    x.First().ProviderSubType == provider.ProviderSubType &&
                    x.First().EstablishmentNumber == provider.EstablishmentNumber &&
                    x.First().ProviderType == provider.ProviderType &&
                    x.First().DateOpened == provider.DateOpened &&
                    x.First().DateClosed == provider.DateClosed &&
                    x.First().LACode == provider.LACode &&
                    x.First().CrmAccountId == provider.CrmAccountId &&
                    x.First().LegalName == provider.LegalName &&
                    x.First().NavVendorNo == provider.NavVendorNo &&
                    x.First().DfeEstablishmentNumber == provider.DfeEstablishmentNumber &&
                    x.First().Status == provider.Status &&
                    x.First().PhaseOfEducation == provider.PhaseOfEducation &&
                    x.First().ReasonEstablishmentClosed == provider.ReasonEstablishmentClosed &&
                    x.First().ReasonEstablishmentOpened == provider.ReasonEstablishmentOpened &&
                    x.First().Successor == provider.Successor &&
                    x.First().TrustStatus == (TrustStatus)provider.TrustStatus &&
                    x.First().TrustName == provider.TrustName &&
                    x.First().TrustCode == provider.TrustCode &&
                    x.First().Town == provider.Town &&
                    x.First().Postcode == provider.Postcode &&
                    x.First().LocalAuthorityName == provider.LocalAuthorityName &&
                    x.First().CompaniesHouseNumber == provider.CompaniesHouseNumber &&
                    x.First().GroupIdNumber == provider.GroupIdNumber &&
                    x.First().RscRegionName == provider.RscRegionName &&
                    x.First().RscRegionCode == provider.RscRegionCode &&
                    x.First().GovernmentOfficeRegionName == provider.GovernmentOfficeRegionName &&
                    x.First().GovernmentOfficeRegionCode == provider.GovernmentOfficeRegionCode &&
                    x.First().DistrictName == provider.DistrictName &&
                    x.First().DistrictCode == provider.DistrictCode &&
                    x.First().WardName == provider.WardName &&
                    x.First().WardCode == provider.WardCode &&
                    x.First().CensusWardName == provider.CensusWardName &&
                    x.First().CensusWardCode == provider.CensusWardCode &&
                    x.First().MiddleSuperOutputAreaName == provider.MiddleSuperOutputAreaName &&
                    x.First().MiddleSuperOutputAreaCode == provider.MiddleSuperOutputAreaCode &&
                    x.First().LowerSuperOutputAreaName == provider.LowerSuperOutputAreaName &&
                    x.First().LowerSuperOutputAreaCode == provider.LowerSuperOutputAreaCode &&
                    x.First().ParliamentaryConstituencyName == provider.ParliamentaryConstituencyName &&
                    x.First().ParliamentaryConstituencyCode == provider.ParliamentaryConstituencyCode &&
                    x.First().CountryCode == provider.CountryCode &&
                    x.First().CountryName == provider.CountryName &&
                    x.First().LocalGovernmentGroupTypeCode == provider.LocalGovernmentGroupTypeCode &&
                    x.First().LocalGovernmentGroupTypeName == provider.LocalGovernmentGroupTypeName
                ), Arg.Is(cacheKeyForList));

            await cacheProvider
               .Received(1)
               .SetExpiry<ProviderSummary>(Arg.Is(cacheKeyForList), Arg.Any<DateTime>());

            totalCountResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = totalCountResult as OkObjectResult;


            int? totalCount = objectResult.Value as int?;

            totalCount
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task FetchCoreProviderData_WhenNotInFileSystemCache_ThenReturnsFromRedisCacheValue()
        {
            // Arrange
            string specificationId = Guid.NewGuid().ToString();
            string providerVersionId = Guid.NewGuid().ToString();
            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyAllProviderSummaries = $"{CacheKeys.AllProviderSummaries}{specificationId}";

            Provider provider = CreateProvider();

            ProviderVersion providerVersion = new ProviderVersion
            {
                Providers = new List<Provider> { provider }
            };

            List<ProviderSummary> cachedProviderSummaries = new List<ProviderSummary>
            {
                MapProviderToSummary(provider)
            };

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                ProviderVersionId = providerVersionId
            };

            IScopedProvidersServiceSettings settings = CreateSettings();
            settings
                .IsFileSystemCacheEnabled
                .Returns(true);

            IFileSystemCache fileSystemCache = CreateFileSystemCache();

            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cachedProviderSummaries as IEnumerable<ProviderSummary>)));
            // memoryStream.Position = 0;


            fileSystemCache.Get(Arg.Any<ScopedProvidersFileSystemCacheKey>())
                .Returns(memoryStream);

            fileSystemCache.Exists(Arg.Any<ScopedProvidersFileSystemCacheKey>())
                .Returns(false);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKeyScopedProviderSummariesCount))
                .Returns("1");

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKeyAllProviderSummaries), Arg.Is(0), Arg.Is(1))
                .Returns(cachedProviderSummaries);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(Arg.Is(cacheKeyAllProviderSummaries))
                .Returns(1);

            ISpecificationsApiClientProxy specificationsApiClient = CreateSpecificationsApiClientProxy();
            specificationsApiClient
                .GetAsync<SpecificationSummary>(Arg.Any<string>())
                .Returns(specificationSummary);


            IProviderVersionService providerVersionService = CreateProviderVersionService();

            IScopedProvidersService providerService = CreateProviderService(cacheProvider: cacheProvider,
                providerVersionService: providerVersionService,
                specificationsApiClient: specificationsApiClient,
                 fileSystemCache: fileSystemCache,
                settings: settings);

            providerVersionService
                .GetProvidersByVersion(Arg.Is(providerVersionId))
                .Returns(providerVersion);

            // Act
            IActionResult result = await providerService.FetchCoreProviderData(specificationId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            IEnumerable<ProviderSummary> results = okObjectResult.Value as IEnumerable<ProviderSummary>;
            IEnumerable<ProviderSummary> expected = cachedProviderSummaries as IEnumerable<ProviderSummary>;
            results.Should().Equals(expected);
        }

        [TestMethod]
        public async Task FetchCoreProviderData_WhenInFileSystemCache_ThenReturnsFileSystemCacheValue()
        {
            // Arrange
            string specificationId = Guid.NewGuid().ToString();
            string providerVersionId = Guid.NewGuid().ToString();
            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyAllProviderSummaries = $"{CacheKeys.AllProviderSummaries}{specificationId}";

            Provider provider = CreateProvider();

            ProviderVersion providerVersion = new ProviderVersion
            {
                Providers = new List<Provider> { provider }
            };

            List<ProviderSummary> cachedProviderSummaries = new List<ProviderSummary>
            {
                MapProviderToSummary(provider)
            };

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                ProviderVersionId = providerVersionId
            };

            IScopedProvidersServiceSettings settings = CreateSettings();
            settings
                .IsFileSystemCacheEnabled
                .Returns(true);

            IFileSystemCache fileSystemCache = CreateFileSystemCache();

            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cachedProviderSummaries)));
            memoryStream.Position = 0;

            fileSystemCache.Get(Arg.Any<ScopedProvidersFileSystemCacheKey>())
                .Returns(memoryStream);

            fileSystemCache.Exists(Arg.Any<ScopedProvidersFileSystemCacheKey>())
                .Returns(true);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKeyScopedProviderSummariesCount))
                .Returns("1");

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKeyAllProviderSummaries), Arg.Is(0), Arg.Is(1))
                .Returns(cachedProviderSummaries);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(Arg.Is(cacheKeyAllProviderSummaries))
                .Returns(1);

            ISpecificationsApiClientProxy specificationsApiClient = CreateSpecificationsApiClientProxy();
            specificationsApiClient
                .GetAsync<SpecificationSummary>(Arg.Any<string>())
                .Returns(specificationSummary);


            IProviderVersionService providerVersionService = CreateProviderVersionService();

            IScopedProvidersService providerService = CreateProviderService(cacheProvider: cacheProvider,
                providerVersionService: providerVersionService,
                specificationsApiClient: specificationsApiClient,
                fileSystemCache: fileSystemCache,
                settings: settings);

            providerVersionService
                .GetProvidersByVersion(Arg.Is(providerVersionId))
                .Returns(providerVersion);

            // Act
            IActionResult result = await providerService.FetchCoreProviderData(specificationId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            IEnumerable<ProviderSummary> results = okObjectResult.Value as IEnumerable<ProviderSummary>;
            IEnumerable<ProviderSummary> expected = cachedProviderSummaries as IEnumerable<ProviderSummary>;
            results.Should().Equals(expected);

            fileSystemCache
               .DidNotReceive()
               .Add(Arg.Any<ScopedProvidersFileSystemCacheKey>(),
                   Arg.Is(memoryStream),
                   Arg.Is(CancellationToken.None));

            fileSystemCache
                .Received(1)
                .EnsureFoldersExist(ScopedProvidersFileSystemCacheKey.Folder);
        }

        [TestMethod]
        public async Task FetchCoreProviderData_WhenNotInCache_ThenReturnsProviderVersion()
        {
            // Arrange
            string specificationId = Guid.NewGuid().ToString();
            string providerVersionId = Guid.NewGuid().ToString();
            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyAllProviderSummaries = $"{CacheKeys.AllProviderSummaries}{specificationId}";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKeyScopedProviderSummariesCount))
                .Returns("0");

            Provider provider = CreateProvider();

            ProviderVersion providerVersion = new ProviderVersion
            {
                Providers = new List<Provider> { provider }
            };

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                ProviderVersionId = providerVersionId
            };

            ISpecificationsApiClientProxy specificationsApiClient = CreateSpecificationsApiClientProxy();
            specificationsApiClient
                .GetAsync<SpecificationSummary>(Arg.Any<string>())
                .Returns(specificationSummary);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            IScopedProvidersService providerService = CreateProviderService(cacheProvider: cacheProvider, specificationsApiClient: specificationsApiClient, providerVersionService: providerVersionService);

            providerVersionService
                .GetProvidersByVersion(Arg.Is(providerVersionId))
                .Returns(providerVersion);

            // Act
            IActionResult result = await providerService.FetchCoreProviderData(specificationId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            IEnumerable<ProviderSummary> results = okObjectResult.Value as IEnumerable<ProviderSummary>;

            results
               .Should()
               .HaveCount(1);

            results.Should().Contain(r => r.Id == "1234");
        }

        [TestMethod]
        public async Task FetchCoreProviderData_WhenNotInCache_ThenAddsToCache()
        {
            // Arrange
            string specificationId = Guid.NewGuid().ToString();
            string providerVersionId = Guid.NewGuid().ToString();
            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyAllProviderSummaries = $"{CacheKeys.AllProviderSummaries}{specificationId}"; ;

            Provider provider = CreateProvider();

            ProviderVersion providerVersion = new ProviderVersion
            {
                Providers = new List<Provider> { provider }
            };

            List<ProviderSummary> cachedProviderSummaries = new List<ProviderSummary>
            {
                MapProviderToSummary(provider)
            };

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                ProviderVersionId = providerVersionId
            };

            IScopedProvidersServiceSettings settings = CreateSettings();
            settings
                .IsFileSystemCacheEnabled
                .Returns(true);

            IFileSystemCache fileSystemCache = CreateFileSystemCache();

            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cachedProviderSummaries as IEnumerable<ProviderSummary>)));
            // memoryStream.Position = 0;


            fileSystemCache.Get(Arg.Any<ScopedProvidersFileSystemCacheKey>())
                .Returns(memoryStream);

            fileSystemCache.Exists(Arg.Any<ScopedProvidersFileSystemCacheKey>())
                .Returns(false);

            ISpecificationsApiClientProxy specificationsApiClient = CreateSpecificationsApiClientProxy();
            specificationsApiClient
                .GetAsync<SpecificationSummary>(Arg.Any<string>())
                .Returns(specificationSummary);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(cacheKeyScopedProviderSummariesCount))
                .Returns("0");

            IProviderVersionService providerVersionService = CreateProviderVersionService();
            providerVersionService
                .GetProvidersByVersion(Arg.Is(providerVersionId))
                .Returns(providerVersion);

            IScopedProvidersService providerService = CreateProviderService(cacheProvider: cacheProvider,
                providerVersionService: providerVersionService,
                specificationsApiClient: specificationsApiClient,
                fileSystemCache: fileSystemCache,
                settings: settings);

            // Act
            IActionResult result = await providerService.FetchCoreProviderData(specificationId);

            // Assert
            await cacheProvider
                .Received(1)
                .KeyDeleteAsync<ProviderSummary>(Arg.Is(cacheKeyAllProviderSummaries));

            await cacheProvider
                .Received(1)
                .CreateListAsync(Arg.Is<IEnumerable<ProviderSummary>>(l => l.Count() == 1), Arg.Is(cacheKeyAllProviderSummaries));

            OkObjectResult okObjectResult = result as OkObjectResult;
            IEnumerable<ProviderSummary> results = okObjectResult.Value as IEnumerable<ProviderSummary>;
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(results)));


            fileSystemCache
              .Received(1)
              .Add(Arg.Any<ScopedProvidersFileSystemCacheKey>(),
                   Arg.Any<MemoryStream>(),
                  Arg.Is(CancellationToken.None));

            fileSystemCache
                .Received(1)
                .EnsureFoldersExist(ScopedProvidersFileSystemCacheKey.Folder);
        }

        private IScopedProvidersService CreateProviderService(IProviderVersionService providerVersionService = null,
            ISpecificationsApiClientProxy specificationsApiClient = null,
            ICacheProvider cacheProvider = null,
            IFileSystemCache fileSystemCache = null,
            IScopedProvidersServiceSettings settings = null,
            IResultsApiClient resultsApiClient = null)
        {
            return new ScopedProvidersService(
                cacheProvider ?? CreateCacheProvider(),
                resultsApiClient ?? CreateResultsApiClient(),
                specificationsApiClient ?? CreateSpecificationsApiClientProxy(),
                providerVersionService ?? CreateProviderVersionService(),
                CreateMapper(),
                settings ?? CreateSettings(),
                fileSystemCache ?? CreateFileSystemCache()
                );
        }

        private IProviderVersionService CreateProviderVersionService()
        {
            return Substitute.For<IProviderVersionService>();
        }

        private ISpecificationsApiClientProxy CreateSpecificationsApiClientProxy()
        {
            return Substitute.For<ISpecificationsApiClientProxy>();
        }

        private ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private IResultsApiClient CreateResultsApiClient()
        {
            return Substitute.For<IResultsApiClient>();
        }

        static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private IMapper CreateMapper()
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(c => c.AddProfile<ProviderVersionsMappingProfile>());
            return mapperConfiguration.CreateMapper();
        }

        private ProviderSummary MapProviderToSummary(Provider provider)
        {
            return new ProviderSummary
            {
                Id = provider.ProviderId,
                Name = provider.Name,
                ProviderProfileIdType = provider.ProviderProfileIdType,
                UKPRN = provider.UKPRN,
                URN = provider.URN,
                Authority = provider.Authority,
                UPIN = provider.UPIN,
                ProviderSubType = provider.ProviderSubType,
                EstablishmentNumber = provider.EstablishmentNumber,
                ProviderType = provider.ProviderType,
                DateOpened = provider.DateOpened,
                DateClosed = provider.DateClosed,
                LACode = provider.LACode,
                CrmAccountId = provider.CrmAccountId,
                LegalName = provider.LegalName,
                NavVendorNo = provider.NavVendorNo,
                DfeEstablishmentNumber = provider.DfeEstablishmentNumber,
                Status = provider.Status,
                PhaseOfEducation = provider.PhaseOfEducation,
                ReasonEstablishmentClosed = provider.ReasonEstablishmentClosed,
                ReasonEstablishmentOpened = provider.ReasonEstablishmentOpened,
                Successor = provider.Successor,
                TrustStatus = (TrustStatus)provider.TrustStatus,
                TrustName = provider.TrustName,
                TrustCode = provider.TrustCode,
                Town = provider.Town,
                Postcode = provider.Postcode,
                LocalAuthorityName = provider.LocalAuthorityName,
                CompaniesHouseNumber = provider.CompaniesHouseNumber,
                GroupIdNumber = provider.GroupIdNumber,
                RscRegionName = provider.RscRegionName,
                RscRegionCode = provider.RscRegionCode,
                GovernmentOfficeRegionName = provider.GovernmentOfficeRegionName,
                GovernmentOfficeRegionCode = provider.GovernmentOfficeRegionCode,
                DistrictName = provider.DistrictName,
                DistrictCode = provider.DistrictCode,
                WardName = provider.WardName,
                WardCode = provider.WardCode,
                CensusWardName = provider.CensusWardName,
                CensusWardCode = provider.CensusWardCode,
                MiddleSuperOutputAreaName = provider.MiddleSuperOutputAreaName,
                MiddleSuperOutputAreaCode = provider.MiddleSuperOutputAreaCode,
                LowerSuperOutputAreaName = provider.LowerSuperOutputAreaName,
                LowerSuperOutputAreaCode = provider.LowerSuperOutputAreaCode,
                ParliamentaryConstituencyName = provider.ParliamentaryConstituencyName,
                ParliamentaryConstituencyCode = provider.ParliamentaryConstituencyCode,
                CountryCode = provider.CountryCode,
                CountryName = provider.CountryName,
                LocalGovernmentGroupTypeCode = provider.LocalGovernmentGroupTypeCode,
                LocalGovernmentGroupTypeName = provider.LocalGovernmentGroupTypeName
            };
        }

        private Provider CreateProvider()
        {
            return new Provider
            {
                Name = "provider name",
                ProviderId = "1234",
                ProviderProfileIdType = "provider id type",
                UKPRN = "UKPRN",
                URN = "URN",
                Authority = "Authority",
                UPIN = "UPIN",
                ProviderSubType = "ProviderSubType",
                EstablishmentNumber = "EstablishmentNumber",
                ProviderType = "ProviderType",
                DateOpened = DateTime.UtcNow.Date,
                DateClosed = DateTime.UtcNow.Date,
                LACode = "LACode",
                CrmAccountId = "CrmAccountId",
                LegalName = "LegalName",
                NavVendorNo = "NavVendorNo",
                DfeEstablishmentNumber = "DfeEstablishmentNumber",
                Status = "Status",
                PhaseOfEducation = "PhaseOfEducation",
                ReasonEstablishmentClosed = "ReasonEstablishmentClosed",
                ReasonEstablishmentOpened = "ReasonEstablishmentOpened",
                Successor = "Successor",
                TrustStatus = TrustStatus.NotApplicable,
                TrustName = "TrustName",
                TrustCode = "TrustCode",
                LocalAuthorityName = "LocalAuthorityName",
                CompaniesHouseNumber = "CompaniesHouseNumber",
                GroupIdNumber = "GroupIdNumber",
                RscRegionName = "RscRegionName",
                RscRegionCode = "RscRegionCode",
                GovernmentOfficeRegionName = "GovernmentOfficeRegionName",
                GovernmentOfficeRegionCode = "GovernmentOfficeRegionCode",
                DistrictName = "DistrictName",
                DistrictCode = "DistrictCode",
                WardName = "WardName",
                WardCode = "WardCode",
                CensusWardName = "CensusWardName",
                CensusWardCode = "CensusWardCode",
                MiddleSuperOutputAreaName = "MiddleSuperOutputAreaName",
                MiddleSuperOutputAreaCode = "MiddleSuperOutputAreaCode",
                LowerSuperOutputAreaName = "LowerSuperOutputAreaName",
                LowerSuperOutputAreaCode = "LowerSuperOutputAreaCode",
                ParliamentaryConstituencyName = "ParliamentaryConstituencyName",
                ParliamentaryConstituencyCode = "ParliamentaryConstituencyCode",
                CountryCode = "CountryCode",
                CountryName = "CountryName",
                LocalGovernmentGroupTypeCode = "LocalGovernmentGroupTypeCode",
                LocalGovernmentGroupTypeName = "LocalGovernmentGroupTypeName"
            };
        }

        private IScopedProvidersServiceSettings CreateSettings()
        {
            return Substitute.For<IScopedProvidersServiceSettings>();
        }

        private IFileSystemCache CreateFileSystemCache()
        {
            return Substitute.For<IFileSystemCache>();
        }
    }
}
