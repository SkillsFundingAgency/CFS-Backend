using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Search.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionSearchServiceTests
    {
        [TestMethod]
        public async Task GetProviderById_WhenProviderIdExistsForProviderVersion_ProviderReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository);

            // Act
            IActionResult okRequest = await providerService.GetProviderById(providerVersionViewModel.ProviderVersionId, provider.ProviderId);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderId)));


            okRequest
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)okRequest).Value
                .Should()
                .BeOfType<ProviderVersionSearchResult>();

            Assert.AreEqual(((ProviderVersionSearchResult)((OkObjectResult)okRequest).Value).Id, provider.ProviderVersionId + "_" + provider.UKPRN);
        }

        [TestMethod]
        public async Task GetProviderById_WhenProviderIdDoesNotExistForProviderVersion_NotFoundReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository);

            // Act
            IActionResult notFoundRequest = await providerService.GetProviderById(providerVersionViewModel.ProviderVersionId, string.Empty);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(providerVersionViewModel.ProviderVersionId)));

            notFoundRequest
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetProviderById_WhenSearchThrowsError_InternalServerErrorThrown()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                    .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository);

            // Act
            IActionResult internalServerResult = await providerService.GetProviderById(providerVersionViewModel.ProviderVersionId, string.Empty);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(providerVersionViewModel.ProviderVersionId)));

            internalServerResult
                .Should()
                .BeOfType<InternalServerErrorResult>();
        }

        [TestMethod]
        [DataRow(12, 12, 2019)]
        public async Task GetProviderById_WhenProviderIdExistsForDate_ProviderReturned(int day, int month, int year)
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            providerVersionService
                .GetProviderVersionByDate(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns<ProviderVersionByDate>(new ProviderVersionByDate { Day = day, Month = month, Year = year, ProviderVersionId = provider.ProviderVersionId });

            // Act
            IActionResult okRequest = await providerService.GetProviderById(year, month, day, provider.ProviderId);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderId)));

            okRequest
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)okRequest).Value
                .Should()
                .BeOfType<ProviderVersionSearchResult>();

            Assert.AreEqual(((ProviderVersionSearchResult)((OkObjectResult)okRequest).Value).Id, provider.ProviderVersionId + "_" + provider.UKPRN);
        }

        [TestMethod]
        [DataRow(12, 12, 2019)]
        public async Task GetProviderById_WhenProviderIdDoesNotExistsForDate_NotFoundReturned(int day, int month, int year)
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            string providerVersionId = Guid.NewGuid().ToString();

            providerVersionService
                .GetProviderVersionByDate(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns<ProviderVersionByDate>(new ProviderVersionByDate { Day = day, Month = month, Year = year, ProviderVersionId = providerVersionId });

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            // Act
            IActionResult notFoundRequest = await providerService.GetProviderById(year, month, day, "12345");

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(providerVersionId)));

            notFoundRequest
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        [DataRow(12, 12, 2019)]
        public async Task GetProviderById_WhenProviderVersionIdDoesNotExistsForDate_NotFoundReturned(int day, int month, int year)
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionsMetadataRepository providerVersionMetadataRepository = CreateProviderVersionMetadataRepository();

            string providerVersionId = Guid.NewGuid().ToString();

            providerVersionMetadataRepository
                .GetProviderVersionByDate(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns<ProviderVersionByDate>((ProviderVersionByDate)null);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionMetadataRepository: providerVersionMetadataRepository);

            // Act
            IActionResult notFoundRequest = await providerService.GetProviderById(year, month, day, "12345");

            notFoundRequest
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetProviderByIdFromMaster_WhenProviderIdExistsInMaster_ReturnProvider()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            providerVersionService
                .GetMasterProviderVersion()
                .Returns<MasterProviderVersion>(new MasterProviderVersion { ProviderVersionId = provider.ProviderVersionId });

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            // Act
            IActionResult okRequest = await providerService.GetProviderByIdFromMaster(provider.ProviderId);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderId)));


            okRequest
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)okRequest).Value
                .Should()
                .BeOfType<ProviderVersionSearchResult>();

            Assert.AreEqual(((ProviderVersionSearchResult)((OkObjectResult)okRequest).Value).Id, provider.ProviderVersionId + "_" + provider.UKPRN);
        }

        [TestMethod]
        public async Task GetProviderByIdFromMaster_WhenProviderIdDoesNotExistsInMaster_NotFoundReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            string providerVersionId = Guid.NewGuid().ToString();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            providerVersionService
                .GetMasterProviderVersion()
                .Returns<MasterProviderVersion>(new MasterProviderVersion { ProviderVersionId = providerVersionId });

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            // Act
            IActionResult notFoundResult = await providerService.GetProviderByIdFromMaster(provider.ProviderId);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderId) && c.Filter.Contains(providerVersionId)));

            notFoundResult
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetProviderByIdFromMaster_WhenNoProviderVersionSetAsMaster_NotFoundReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            providerVersionService
                .GetMasterProviderVersion()
                .Returns<MasterProviderVersion>((MasterProviderVersion)null);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            // Act
            IActionResult notFoundResult = await providerService.GetProviderByIdFromMaster(provider.ProviderId);

            notFoundResult
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        [DataRow(12, 12, 2019)]
        public async Task SearchProviders_WhenNoProviderVersionSetAsMaster_NotFoundReturned(int day, int month, int year)
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            providerVersionService
                .GetMasterProviderVersion()
                .Returns<MasterProviderVersion>((MasterProviderVersion)null);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            // Act
            IActionResult notFoundResult = await providerService.SearchProviders(year, month, day, new Models.SearchModel());

            notFoundResult
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task SearchMasterProviders_WhenNoProviderVersionSetAsMaster_NotFoundReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            providerVersionService
                .GetMasterProviderVersion()
                .Returns<MasterProviderVersion>((MasterProviderVersion)null);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            // Act
            IActionResult notFoundResult = await providerService.SearchMasterProviders(new Models.SearchModel());

            notFoundResult
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task SearchProviders_WhenSearchingAndProvidersExistInMaster_ReturnProviders()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository);

            // Act
            IActionResult okRequest = await providerService.SearchProviders(provider.ProviderVersionId);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderVersionId)));

            okRequest
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)okRequest).Value
                .Should()
                .BeOfType<ProviderVersionSearchResults>();
        }

        [TestMethod]
        public async Task SearchProviders_WhenSearchThrowsError_InternalServerErrorThrown()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository);

            // Act
            IActionResult okRequest = await providerService.SearchProviders(provider.ProviderVersionId);

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderVersionId)));

            okRequest
                .Should()
                .BeOfType<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task SearchMasterProviders_WhenSearchingAndProvidersExistInMaster_ReturnProviders()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            providerVersionService
                .GetMasterProviderVersion()
                .Returns<MasterProviderVersion>(new MasterProviderVersion { ProviderVersionId = provider.ProviderVersionId });

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            // Act
            IActionResult okRequest = await providerService.SearchMasterProviders(new Models.SearchModel { Filters = new Dictionary<string, string[]> { { "providerId", new List<string> { provider.ProviderId }.ToArray() } } });

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderVersionId) && c.Filter.Contains(provider.ProviderId)));

            okRequest
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)okRequest).Value
                .Should()
                .BeOfType<ProviderVersionSearchResults>();
        }

        [TestMethod]
        [DataRow(12, 12, 2019)]
        public async Task SearchProviders_WhenProviderIdExistsForDate_ProviderReturned(int day, int month, int year)
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            IProviderVersionService providerVersionService = CreateProviderVersionService();

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository, providerVersionService: providerVersionService);

            providerVersionService
                .GetProviderVersionByDate(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns<ProviderVersionByDate>(new ProviderVersionByDate { Day = day, Month = month, Year = year, ProviderVersionId = provider.ProviderVersionId });

            // Act
            IActionResult okRequest = await providerService.SearchProviders(year, month, day, new Models.SearchModel { Filters = new Dictionary<string, string[]> { { "providerId", new List<string> { provider.ProviderId }.ToArray() } } });

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderId)));

            okRequest
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)okRequest).Value
                .Should()
                .BeOfType<ProviderVersionSearchResults>();
        }

        [TestMethod]
        public async Task SearchProviderVersions_WhenProviderIdExists_ProviderReturned()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(Arg.Any<string>(), Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository);

            // Act
            IActionResult okRequest = await providerService.SearchProviderVersions(new Models.SearchModel { Filters = new Dictionary<string, string[]> { { "providerId", new List<string> { provider.ProviderId }.ToArray() } } });

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderId)));

            okRequest
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)okRequest).Value
                .Should()
                .BeOfType<ProviderVersionSearchResults>();
        }

        [TestMethod]
        public async Task SearchProviderVersions_WhenSearchThrowsError_InternalServerErrorThrown()
        {
            // Arrange
            ProviderVersionViewModel providerVersionViewModel = CreateProviderVersion();

            Provider provider = GetProvider();

            providerVersionViewModel.Providers = providerVersionViewModel.Providers.Concat(new[] { provider });
            providerVersionViewModel.VersionType = ProviderVersionType.Custom;

            ICacheProvider cacheProvider = CreateCacheProvider();

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex> { Results = new List<Repositories.Common.Search.SearchResult<ProvidersIndex>> { new Repositories.Common.Search.SearchResult<ProvidersIndex> { Result = new ProvidersIndex { ProviderVersionId = provider.ProviderVersionId, UKPRN = provider.UKPRN } } } };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(s => s.Search(Arg.Any<string>(), Arg.Any<SearchParameters>()))
                .Do(x => { throw new FailedToQuerySearchException("Test Message", null); });

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(cacheProvider: cacheProvider, searchRepository: searchRepository);

            // Act
            IActionResult okRequest = await providerService.SearchProviderVersions(new Models.SearchModel { Filters = new Dictionary<string, string[]> { { "providerId", new List<string> { provider.ProviderId }.ToArray() } } });

            await searchRepository.Received(1)
                    .Search(Arg.Any<string>(), Arg.Is<SearchParameters>(c => c.Filter.Contains(provider.ProviderId)));

            okRequest
                .Should()
                .BeOfType<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task GetFacetValues_WhenFacetNotProvided_NotFoundResultReturned()
        {
            // Arrange
            IProviderVersionSearchService providerService = CreateProviderVersionSearchService();

            // Act
            IActionResult notFoundRequest = await providerService.GetFacetValues(string.Empty);

            // Assert
            notFoundRequest
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFacetValues_WhenValidFacetProvided_FacetDistinctValuesReturned()
        {
            // Arrange

            string authorityFacet = "authority";
            
            string facetValue1 = "Kent";
            string facetValue2 = "Essex";

            SearchResults<ProvidersIndex> searchResults = new SearchResults<ProvidersIndex>
            {
                Facets = new List<Facet>
                {
                    new Facet
                    {
                        Name = authorityFacet,
                        FacetValues = new List<FacetValue>
                        {
                            new FacetValue { Name = facetValue1 },
                            new FacetValue { Name = facetValue2 }
                        }
                    }
                }
            };

            ISearchRepository<ProvidersIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Search(string.Empty, Arg.Any<SearchParameters>())
                .Returns(searchResults);

            IProviderVersionSearchService providerService = CreateProviderVersionSearchService(searchRepository: searchRepository);

            // Act
            IActionResult okObjectRequest = await providerService.GetFacetValues(authorityFacet);

            // Assert
            okObjectRequest
                .Should()
                .BeOfType<OkObjectResult>();

            await searchRepository
                .Received(1)
                .Search(string.Empty, Arg.Is<SearchParameters>(x => x.Top == 0 && x.Facets.Count == 1 && x.Facets.First().Contains(authorityFacet)));

            ((okObjectRequest as OkObjectResult).Value as IEnumerable<string>)
                .Should()
                .HaveCount(2);
        }

        private ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private IProviderVersionsMetadataRepository CreateProviderVersionMetadataRepository()
        {
            return Substitute.For<IProviderVersionsMetadataRepository>();
        }

        private ISearchRepository<ProvidersIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ProvidersIndex>>();
        }

        private IProvidersResiliencePolicies CreateResiliencePolicies()
        {
            IProvidersResiliencePolicies providersResiliencePolicies = Substitute.For<IProvidersResiliencePolicies>();
            providersResiliencePolicies.ProviderVersionMetadataRepository = Policy.NoOpAsync();
            providersResiliencePolicies.ProviderVersionsSearchRepository = Policy.NoOpAsync();
            return providersResiliencePolicies;
        }

        private IProviderVersionService CreateProviderVersionService()
        {
            return Substitute.For<IProviderVersionService>();
        }

        private IProviderVersionSearchService CreateProviderVersionSearchService(ICacheProvider cacheProvider = null, ISearchRepository<ProvidersIndex> searchRepository = null, IProviderVersionsMetadataRepository providerVersionMetadataRepository = null, IProviderVersionService providerVersionService = null)
        {
            return new ProviderVersionSearchService(
                CreateLogger(),
                searchRepository ?? CreateSearchRepository(),
                providerVersionMetadataRepository ?? CreateProviderVersionMetadataRepository(),
                CreateResiliencePolicies(),
                providerVersionService ?? CreateProviderVersionService());
        }

        private ProviderVersionViewModel CreateProviderVersion()
        {
            return new ProviderVersionViewModel
            {
                ProviderVersionId = System.Guid.NewGuid().ToString(),
                Description = "Test provider version description",
                Name = "Test provider version",
                Providers = new[]
                {
                    GetProvider()
                }
            };
        }

        public Provider GetProvider()
        {
            return new Provider
            {
                ProviderVersionId = System.Guid.NewGuid().ToString(),
                ProviderId = "UKPRN",
                Name = "EstablishmentName",
                URN = "URN",
                UKPRN = "UKPRN",
                UPIN = "UPIN",
                EstablishmentNumber = "EstablishmentNumber",
                DfeEstablishmentNumber = "LA (code) EstablishmentNumber",
                Authority = "LA (name)",
                ProviderType = "TypeOfEstablishment (name)",
                ProviderSubType = "EstablishmentTypeGroup (name)",
                DateOpened = System.DateTime.Now,
                DateClosed = null,
                ProviderProfileIdType = "",
                LACode = "LA (code)",
                NavVendorNo = "",
                CrmAccountId = "",
                LegalName = "",
                Status = "EstablishmentStatus (name)",
                PhaseOfEducation = "PhaseOfEducation (code)",
                ReasonEstablishmentOpened = "",
                ReasonEstablishmentClosed = "",
                Successor = "",
                TrustName = "Trusts (name)",
                TrustCode = "",
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
                LocalGovernmentGroupTypeName = "LocalGovernmentGroupTypeName",
                Street = "Street",
                Locality = "Locality",
                Address3 = "Address3"
            };
        }

    }
}
