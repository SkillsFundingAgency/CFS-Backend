using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Providers.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderServiceTests
    {
        [TestMethod]
        public async Task FetchCoreProviderData_WhenInCache_ThenReturnsCacheValue()
        {
            // Arrange
            IResultsApiClientProxy resultsApiClient = CreateResultsApiClient();
            resultsApiClient
                .PostAsync<ProviderSearchResults, SearchModel>(Arg.Is("results/providers-search"), Arg.Any<SearchModel>())
                .Returns(new ProviderSearchResults { TotalCount = 3 });

            List<ProviderSummary> cachedProviderSummaries = new List<ProviderSummary>
            {
                new ProviderSummary { Id = "one" },
                new ProviderSummary { Id = "two" },
                new ProviderSummary { Id = "three" }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(CacheKeys.AllProviderSummaryCount))
                .Returns("3");
            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(CacheKeys.AllProviderSummaries), Arg.Is(0), Arg.Is(3))
                .Returns(cachedProviderSummaries);

            IProviderService providerService = CreateProviderService(cacheProvider: cacheProvider, resultsApiClient: resultsApiClient);

            // Act
            IEnumerable<ProviderSummary> results = await providerService.FetchCoreProviderData();

            // Assert
            results.Should().Contain(cachedProviderSummaries);
        }

        [TestMethod]
        public async Task FetchCoreProviderData_WhenNotInCache_ThenReturnsSearchValue()
        {
            // Arrange
            IResultsApiClientProxy resultsApiClient = CreateResultsApiClient();
            resultsApiClient
                .PostAsync<ProviderSearchResults, SearchModel>(Arg.Is("results/providers-search"), Arg.Any<SearchModel>())
                .Returns(new ProviderSearchResults
                {
                    TotalCount = 3,
                    Results = new List<ProviderSearchResult>
                    {
                        new ProviderSearchResult { ProviderId = "one"},
                        new ProviderSearchResult { ProviderId = "two"},
                        new ProviderSearchResult { ProviderId = "three"}
                    }
                });

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(CacheKeys.AllProviderSummaryCount))
                .Returns("0");

            IProviderService providerService = CreateProviderService(cacheProvider: cacheProvider, resultsApiClient: resultsApiClient);

            // Act
            IEnumerable<ProviderSummary> results = await providerService.FetchCoreProviderData();

            // Assert
            results
               .Should()
               .HaveCount(3);

            results.Should().Contain(r => r.Id == "one");
            results.Should().Contain(r => r.Id == "two");
            results.Should().Contain(r => r.Id == "three");
        }

        [TestMethod]
        public async Task FetchCoreProviderData_WhenNotInCache_ThenAddsToCache()
        {
            // Arrange
            IResultsApiClientProxy resultsApiClient = CreateResultsApiClient();
            resultsApiClient
                .PostAsync<ProviderSearchResults, SearchModel>(Arg.Is("results/providers-search"), Arg.Any<SearchModel>())
                .Returns(new ProviderSearchResults
                {
                    TotalCount = 3,
                    Results = new List<ProviderSearchResult>
                    {
                        new ProviderSearchResult { ProviderId = "one"},
                        new ProviderSearchResult { ProviderId = "two"},
                        new ProviderSearchResult { ProviderId = "three"}
                    }
                });

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is(CacheKeys.AllProviderSummaryCount))
                .Returns("0");

            IProviderService providerService = CreateProviderService(cacheProvider: cacheProvider, resultsApiClient: resultsApiClient);

            // Act
            IEnumerable<ProviderSummary> results = await providerService.FetchCoreProviderData();

            // Assert
            await cacheProvider
                .Received(1)
                .KeyDeleteAsync<List<ProviderSummary>>(Arg.Is(CacheKeys.AllProviderSummaries));

            await cacheProvider
                .Received(1)
                .CreateListAsync(Arg.Is<List<ProviderSummary>>(l => l.Count == 3), Arg.Is(CacheKeys.AllProviderSummaries));
        }

        private IProviderService CreateProviderService(ICacheProvider cacheProvider = null, IResultsApiClientProxy resultsApiClient = null)
        {
            return new ProviderService(
                cacheProvider ?? CreateCacheProvider(),
                resultsApiClient ?? CreateResultsApiClient(),
                CreateMapper());
        }

        private ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private IResultsApiClientProxy CreateResultsApiClient()
        {
            return Substitute.For<IResultsApiClientProxy>();
        }

        private IMapper CreateMapper()
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(c => c.AddProfile<ProviderMappingProfile>());
            return mapperConfiguration.CreateMapper();
        }
    }
}
