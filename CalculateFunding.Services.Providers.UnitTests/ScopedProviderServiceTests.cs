using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Models.Providers;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Providers.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using ExpectedObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Core;
using ProviderSource = CalculateFunding.Common.ApiClient.Models.ProviderSource;

// ReSharper disable SuspiciousTypeConversion.Global

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ScopedProviderServiceTests
    {
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private Mock<IResultsApiClient> _resultsApiClient;
        private Mock<ICacheProvider> _cacheProvider;
        private Mock<IProviderVersionService> _providerVersionService;
        private Mock<IScopedProvidersServiceSettings> _settings;
        private Mock<IFileSystemCache> _fileSystemCache;
        private Mock<IJobManagement> _jobManagement;

        private IDictionary<string, byte[]> _bytesWrittenToFileSystemCache;

        private ScopedProvidersService _scopedProvidersService;

        private Message _message;
        
        [TestInitialize]
        public void SetUp()
        {
            _cacheProvider = new Mock<ICacheProvider>();
            _resultsApiClient = new Mock<IResultsApiClient>();
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _providerVersionService = new Mock<IProviderVersionService>();
            _settings = new Mock<IScopedProvidersServiceSettings>();
            _fileSystemCache = new Mock<IFileSystemCache>();
            _jobManagement = new Mock<IJobManagement>();
            
            _scopedProvidersService = new ScopedProvidersService(_cacheProvider.Object,
                _resultsApiClient.Object,
                _specificationsApiClient.Object,
                _providerVersionService.Object,
                _settings.Object,
                _fileSystemCache.Object,
                _jobManagement.Object,
                ProviderResilienceTestHelper.GenerateTestPolicies(),
                Logger.None);
            
            _message = new Message();

            _settings.Setup(_ => _.IsFileSystemCacheEnabled)
                .Returns(true);
            
            _bytesWrittenToFileSystemCache = new Dictionary<string, byte[]>();

            _fileSystemCache.Setup(_ => _.Add(It.IsAny<FileSystemCacheKey>(),
                    It.IsAny<Stream>(),
                    default,
                    false))
                .Callback<FileSystemCacheKey, Stream, CancellationToken, bool>((key,
                    stream,
                    cancellationToken,
                    ensureFolderExists) =>
                {
                    byte[] writtenBytes = new byte[stream.Length];

                    for (int bytePosition = 0; bytePosition < stream.Length; bytePosition++)
                    {
                        writtenBytes[bytePosition] = (byte)stream.ReadByte();
                    }

                    _bytesWrittenToFileSystemCache[key.Key] = writtenBytes;
                });
        }

        [TestMethod]
        public async Task FetchCoreProviderData_ReturnsFileSystemDataForFdzProviderSources()
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();

            Provider providerOne = NewProvider();
            Provider providerTwo = NewProvider();

            ProviderSummary[] expectedProviderSummaries = MapProvidersToSummaries(providerOne, providerTwo);
            
            AndTheProviderVersionInTheFileSystemCache(providerVersionId, NewProviderVersion(_ => 
                _.WithProviders(providerOne, providerTwo)));
            
            ContentResult result = await WhenTheCoreProviderDataIsFetched(specificationId, providerVersionId) as ContentResult;
            
            result
                .Content
                .Should()
                .Be(expectedProviderSummaries.AsJson());

            result
                .ContentType
                .Should()
                .Be("application/json");
        }
        
        [TestMethod]
        public async Task FetchCoreProviderData_ReturnsBlobDataForFdzProviderSourcesAndAddsToFileSystemCacheForProviderVersionIdIfNotAlreadyCached()
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();

            Provider providerOne = NewProvider();
            Provider providerTwo = NewProvider();
            ProviderVersion providerVersion = NewProviderVersion(_ => _.WithProviders(providerOne, providerTwo));
            
            AndTheProviderVersion(providerVersionId, providerVersion);
            
            ContentResult result = await WhenTheCoreProviderDataIsFetched(specificationId, providerVersionId) as ContentResult;
            
            ProviderSummary[] expectedProviderSummaries = MapProvidersToSummaries(providerOne, providerTwo);
            
            result
                .Content
                .Should()
                .Be(expectedProviderSummaries.AsJson());

            result
                .ContentType
                .Should()
                .Be("application/json");
            
            AndTheFileSystemCacheDataWasWritten(new ProviderVersionFileSystemCacheKey(providerVersionId).Key,
                providerVersion.AsJsonBytes());
        }

        [TestMethod]
        public async Task GetScopedProviderIds_WhenProviderSourceIsCFS_FetchesScopedProviderIdsFromResultsService()
        {
            string specificationId = NewRandomString();
            string scopedProviderIdOne = NewRandomString();
            string scopedProviderIdTwo = NewRandomString();
            string scopedProviderIdThree = NewRandomString();
            string scopedProviderIdFour = NewRandomString();
            string scopedProviderIdFive = NewRandomString();
            
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderSource(ProviderSource.CFS));
            
            GivenTheSpecificationSummary(specificationId, specificationSummary);
            AndTheScopedProviderIds(specificationId, 
                scopedProviderIdOne,
                scopedProviderIdTwo,
                scopedProviderIdThree,
                scopedProviderIdFour,
                scopedProviderIdFive);
            
            OkObjectResult result = await WhenTheScopedProviderIdsAreQueried(specificationId) as OkObjectResult;

            result
                .Should()
                .NotBeNull();
            
            result.Value
                .Should()
                .BeEquivalentTo(new []
                {
                    scopedProviderIdOne,
                    scopedProviderIdTwo,
                    scopedProviderIdThree,
                    scopedProviderIdFour,
                    scopedProviderIdFive,
                });
        }

        [TestMethod]
        public async Task FetchCoreProviderData_WhenProviderSourceIsFDZ_FetchesAllProvidersInProviderVersion()
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string cacheKeyForList = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            string cacheKeyForProviderVersion = $"{CacheKeys.ScopedProviderProviderVersion}{specificationId}";

            Provider providerOne = NewProvider();
            Provider providerTwo = NewProvider();
            Provider providerThree = NewProvider();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSource(ProviderSource.FDZ));

            ProviderVersion providerVersion = NewProviderVersion(_ => _.WithProviders(providerOne,
                providerTwo,
                providerThree));

            GivenTheSpecificationSummary(specificationId, specificationSummary);
            AndTheProviderVersion(providerVersionId, providerVersion);
            AndTheMessageProperties(("jobId", NewRandomString()), ("specification-id", specificationId));

            await WhenTheScopedProvidersArePopulated();
            
            ThenTheEquivalentProviderSummariesWereCached(cacheKeyForList, MapProvidersToSummaries(providerOne, providerTwo, providerThree));
            ThenTheProviderVersionIsCached(cacheKeyForProviderVersion, providerVersionId);
            AndACacheExpiryWasSet(cacheKeyForList);
        }
        
        [TestMethod]
        public async Task FetchCoreProviderData_GivenSpecificationWithProviderVersionIdAndProviderSourceOfFDZ_CachesAllProvidersInTheProviderVersion()
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string scopedProviderId = NewRandomString();
            string cacheGuid = NewRandomString();
            string cacheGuidKey = $"{CacheKeys.ScopedProviderSummariesFilesystemKeyPrefix}{specificationId}";
            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyScopedProviderSummaries = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            Provider provider = NewProvider(_ => _.WithProviderId(scopedProviderId));
            ProviderVersion providerVersion = NewProviderVersion(_ => _.WithProviders(provider,
                NewProvider(),
                NewProvider()));
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSource(ProviderSource.CFS));
            ProviderSummary[] cachedProviderSummaries = MapProvidersToSummaries(provider);
            await using MemoryStream memoryStream = new MemoryStream(cachedProviderSummaries.AsJsonBytes());
            
            GivenTheSpecificationSummary(specificationId, specificationSummary);
            AndTheProviderVersion(providerVersionId, providerVersion);
            AndTheCacheGuidForTheSpecification(cacheGuidKey, cacheGuid);
            AndTheCachedScopedProvidersCount(cacheKeyScopedProviderSummariesCount, 1);
            AndTheCachedScopedProvidersInRange(cacheKeyScopedProviderSummaries, 1, 1, cachedProviderSummaries);
            
            ContentResult contentResult = await WhenTheCoreProviderDataIsFetched(specificationId) as ContentResult;
            
            contentResult
                .Should()
                .NotBeNull();

            IEnumerable<ProviderSummary> actualProviderSummaries = contentResult.Content?.AsPoco<IEnumerable<ProviderSummary>>();

            actualProviderSummaries
                .Should()
                .BeEquivalentTo<ProviderSummary>(cachedProviderSummaries);
            
            AndTheProviderSummariesWereAddedToTheFileSystemCache(specificationId, cacheGuid, cachedProviderSummaries);
            AndTheFileSystemCacheFolderWasLazilyInitialised();
        }
        
        [TestMethod]
        public async Task Process_WhenNotCachedAndSourceFDZ_ReturnsAllProvidersInProviderVersion()
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string cacheKeyForList = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            ProviderVersion providerVersion = NewProviderVersion(_ => _.WithProviders(NewProvider(),
                NewProvider(),
                NewProvider()));

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSource(ProviderSource.FDZ));

            GivenTheSpecificationSummary(specificationId, specificationSummary);
            AndTheProviderVersion(providerVersionId, providerVersion);
            AndTheMessageProperties(("jobId", NewRandomString()), ("specification-id", specificationId));

            await WhenTheScopedProvidersArePopulated();
            
            ThenTheEquivalentProviderSummariesWereCached(cacheKeyForList, MapProvidersToSummaries(providerVersion.Providers.ToArray()));
            AndACacheExpiryWasSet(cacheKeyForList);
        }

        [TestMethod]
        public async Task Process_GivenSpecificationWithProviderVersionIdAndProviderSourceOfCFS_CachesOnlyThoseProvidersInScope()
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string scopedProviderId = NewRandomString();
            string cacheKeyForList = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            Provider provider = NewProvider(_ => _.WithProviderId(scopedProviderId));
            ProviderVersion providerVersion = NewProviderVersion(_ => _.WithProviders(provider,
                NewProvider(),
                NewProvider()));
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSource(ProviderSource.CFS));
            
            GivenTheSpecificationSummary(specificationId, specificationSummary);
            AndTheProviderVersion(providerVersionId, providerVersion);
            AndTheScopedProviderIds(specificationId, scopedProviderId);
            AndTheMessageProperties(("jobId", NewRandomString()), ("specification-id", specificationId));

            await WhenTheScopedProvidersArePopulated();
            
            ThenTheEquivalentProviderSummaryWasCached(cacheKeyForList, MapProviderToSummary(provider));
            AndACacheExpiryWasSet(cacheKeyForList);
        }


        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task PopulateProviderSummariesForSpecification_GivenProviderSourceFromFDZ_ThenJobQueued(bool setCacheProviders)
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSource(ProviderSource.FDZ));

            GivenTheSpecificationSummary(specificationId, specificationSummary);
            
            OkObjectResult contentResult = await WhenTheProviderSummariesPopulated(specificationId, false) as OkObjectResult;

            ThenTheJobIsQueued(true);

            contentResult
                .Should()
                .NotBeNull();

            bool? jobQueued = (bool)contentResult.Value;

            jobQueued
                .Should()
                .Be(true);
        }

        [TestMethod]
        [DataRow(false, 0, true)]
        [DataRow(false, 1, false)]
        [DataRow(true, 0, true)]
        public async Task PopulateProviderSummariesForSpecification_GivenProviderSourceFromCFS_ThenTheExpectedResult(bool setCacheProviders, int providerSummaryCacheCount, bool expectedResult)
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string scopedProviderId = NewRandomString();
            string cacheKeyScopedProviderSummariesCount = $"{CacheKeys.ScopedProviderSummariesCount}{specificationId}";
            string cacheKeyScopedProviderSummaries = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            Provider provider = NewProvider(_ => _.WithProviderId(scopedProviderId));
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSource(ProviderSource.CFS));

            ProviderSummary[] cachedProviderSummaries = MapProvidersToSummaries(provider);

            GivenTheSpecificationSummary(specificationId, specificationSummary);
            AndTheCachedScopedProvidersCount(cacheKeyScopedProviderSummariesCount, providerSummaryCacheCount);
            AndTheCachedScopedProvidersInRange(cacheKeyScopedProviderSummaries, 1, 1, cachedProviderSummaries);

            OkObjectResult contentResult = await WhenTheProviderSummariesPopulated(specificationId, false) as OkObjectResult;

            ThenTheJobIsQueued(expectedResult);

            contentResult
                .Should()
                .NotBeNull();

            bool? jobQueued = (bool)contentResult.Value;

            jobQueued
                .Should()
                .Be(expectedResult);
        }


        [TestMethod]
        public async Task FetchCoreProviderData_WhenInFileSystemCache_ThenReturnsFileSystemCacheValue()
        {
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string scopedProviderId = NewRandomString();
            string cacheGuid = NewRandomString();
            string cacheGuidKey = $"{CacheKeys.ScopedProviderSummariesFilesystemKeyPrefix}{specificationId}";

            Provider provider = NewProvider(_ => _.WithProviderId(scopedProviderId));
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSource(ProviderSource.CFS));
            ProviderSummary[] cachedProviderSummaries = MapProvidersToSummaries(provider);
            await using MemoryStream memoryStream = new MemoryStream(cachedProviderSummaries.AsJsonBytes());
            
            GivenTheSpecificationSummary(specificationId, specificationSummary);
            AndTheCacheGuidForTheSpecification(cacheGuidKey, cacheGuid);
            AndTheProviderSummariesInTheFileSystemCache(specificationId, cacheGuid, cachedProviderSummaries);
            
            ContentResult contentResult = await WhenTheCoreProviderDataIsFetched(specificationId) as ContentResult;
            
            contentResult
                .Should()
                .NotBeNull();

            IEnumerable<ProviderSummary> actualProviderSummaries = contentResult.Content?.AsPoco<IEnumerable<ProviderSummary>>();

            actualProviderSummaries
                .Should()
                .BeEquivalentTo<ProviderSummary>(cachedProviderSummaries);

            AndTheFileSystemCacheFolderWasLazilyInitialised();
        }

        private async Task<IActionResult> WhenTheScopedProviderIdsAreQueried(string specificationId)
            => await _scopedProvidersService.GetScopedProviderIds(specificationId);

        private void GivenTheSpecificationSummary(string specificationId,
            SpecificationSummary specificationSummary)
        {
            _specificationsApiClient.Setup(_ => _.GetSpecificationSummaryById(specificationSummary.Id))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));
        }

        private void AndTheProviderVersion(string providerVersionId,
            ProviderVersion providerVersion)
        {
            _providerVersionService.Setup(_ => _.GetProvidersByVersion(providerVersionId))
                .ReturnsAsync(providerVersion);
        }

        private void AndTheScopedProviderIds(string specificationId,
            params string[] scopedProviderIds)
        {
            _resultsApiClient.Setup(_ => _.GetScopedProviderIdsBySpecificationId(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, scopedProviderIds));
        }

        private void AndTheMessageProperties(params (string key, string value)[] properties)
        {
            _message.AddUserProperties(properties);
        }

        private async Task WhenTheScopedProvidersArePopulated()
        {
            await _scopedProvidersService.Process(_message);
        }

        private void ThenTheEquivalentProviderSummaryWasCached(string key, ProviderSummary provider)
        {
            _cacheProvider.Verify(_ => _.CreateListAsync(It.Is<IEnumerable<ProviderSummary>>(summaries =>
                summaries.Count() == 1 &&
                provider.ToExpectedObject().Equals(summaries.Single())),
                key), Times.Once);
        }

        private void ThenTheJobIsQueued(bool called)
        {
            if (called)
            {
                _jobManagement.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job => job.JobDefinitionId == JobConstants.DefinitionNames.PopulateScopedProvidersJob)), Times.Once);
            }
            else
            {
                _jobManagement.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job => job.JobDefinitionId == JobConstants.DefinitionNames.PopulateScopedProvidersJob)), Times.Never);
            }
        }
        
        private void ThenTheEquivalentProviderSummariesWereCached(string key, params ProviderSummary[] providers)
        {
            _cacheProvider.Verify(_ => _.CreateListAsync(It.Is<IEnumerable<ProviderSummary>>(summaries =>
            ProviderSummariesAllMatch(summaries, providers)),
                key), Times.Once);
        }

        private void ThenTheProviderVersionIsCached(string key, string providerVersion)
        {
            _cacheProvider.Verify(_ => _.SetAsync(key, providerVersion, TimeSpan.FromDays(7), true, null), Times.Once);
        }

        private void AndTheEquivalentProviderSummariesWereCached(string key,
            params ProviderSummary[] providers)
        {
            ThenTheEquivalentProviderSummariesWereCached(key, providers);
        }

        private bool ProviderSummariesAllMatch(IEnumerable<ProviderSummary> actualSummaries,
            IEnumerable<ProviderSummary> expectedSummaries)
        {
            for (int summary = 0; summary < actualSummaries.Count(); summary++)
            {
                ProviderSummary expectedSummary = expectedSummaries.ElementAt(summary);
                ProviderSummary actualSummary = actualSummaries.ElementAt(summary);

                if (!actualSummary.ToExpectedObject().Equals(expectedSummary))
                {
                    return false;
                }
            }

            return true;    
        }

        private void AndThenTheEquivalentProviderSummaryWasCached(string key,
            ProviderSummary provider)
        {
            ThenTheEquivalentProviderSummaryWasCached(key, provider);
        }

        private void AndACacheExpiryWasSet(string key)
        {
            _cacheProvider.Verify(_ => _.SetExpiry<ProviderSummary>(key, It.Is<DateTime>(exp => exp > DateTime.UtcNow)));
        }

        private void AndTheCacheGuidForTheSpecification(string key,
            string cacheGuid)
        {
            _cacheProvider.Setup(_ => _.GetAsync<string>(key, null))
                .ReturnsAsync(cacheGuid);
        }

        private void AndTheCachedScopedProvidersCount(string key,
            int count)
        {
            _cacheProvider.Setup(_ => _.GetAsync<string>(key, null))
                .ReturnsAsync(count.ToString());
        }

        private void AndTheCachedScopedProvidersInRange(string key,
            int rangeEnd,
            int length,
            IEnumerable<ProviderSummary> providerSummaries)
        {
            _cacheProvider.Setup(_ => _.ListLengthAsync<ProviderSummary>(key))
                .ReturnsAsync(length);
            _cacheProvider.Setup(_ => _.ListRangeAsync<ProviderSummary>(key, 0, rangeEnd))
                .ReturnsAsync(providerSummaries);
        }

        private void AndTheProviderVersionInTheFileSystemCache(string providerVersionId,
            ProviderVersion providerVersion)
        {
            GivenTheFileSystemCacheContents(new ProviderVersionFileSystemCacheKey(providerVersionId).Key,
                 providerVersion.AsJsonBytes());
        }

        private void AndTheProviderSummariesInTheFileSystemCache(string specificationId,
            string cacheGuid,
            IEnumerable<ProviderSummary> cachedProviderSummaries)
        {
            GivenTheFileSystemCacheContents(new ScopedProvidersFileSystemCacheKey(specificationId, cacheGuid).Key, 
                cachedProviderSummaries.AsJsonBytes());
        }

        private void GivenTheFileSystemCacheContents(string fileSystemCacheKey,
            byte[] documentBytes)
        {
            _fileSystemCache.Setup(_ => _.Exists(It.Is<FileSystemCacheKey>(fs => fs.Key == fileSystemCacheKey)))
                .Returns(true);
            _fileSystemCache.Setup(_ => _.Get(It.Is<FileSystemCacheKey>(fs => fs.Key == fileSystemCacheKey)))
                .Returns(new MemoryStream(documentBytes));
        }

        private void AndTheProviderVersionWasAddedToTheFileSystemCache(string providerVersionId,
            params ProviderSummary[] providerSummaries)
        {
            AndTheFileSystemCacheDataWasWritten(new ProviderVersionFileSystemCacheKey(providerVersionId).Key, 
                providerSummaries.AsJsonBytes());
        }

        private void AndTheProviderSummariesWereAddedToTheFileSystemCache(string specificationId,
            string cacheGuid,
            IEnumerable<ProviderSummary> providerSummaries)
        {
            AndTheFileSystemCacheDataWasWritten(new ScopedProvidersFileSystemCacheKey(specificationId, cacheGuid).Key, 
                providerSummaries.AsJsonBytes());
        }

        private void AndTheFileSystemCacheDataWasWritten(string fileSystemCacheKey,
            byte[] expectedData)
        {
            _bytesWrittenToFileSystemCache.TryGetValue(fileSystemCacheKey, out byte[] actualBytes)
                .Should()
                .BeTrue();

            actualBytes
                .Should()
                .BeEquivalentTo(expectedData);
        }

        private void AndTheFileSystemCacheFolderWasLazilyInitialised()
        {
            _fileSystemCache.Verify(_ => _.EnsureFoldersExist("scopedProviders"),
                Times.Once);
        }

        private async Task<IActionResult> WhenTheCoreProviderDataIsFetched(string specificationId, string providerVersionId = null)
            => await _scopedProvidersService.FetchCoreProviderData(specificationId, providerVersionId);

        private async Task<IActionResult> WhenTheProviderSummariesPopulated(string specificationId, bool setCachedProviders)
            => await _scopedProvidersService.PopulateProviderSummariesForSpecification(specificationId, null, null , setCachedProviders);

        private ProviderSummary[] MapProvidersToSummaries(params Provider[] providers)
            => providers.Select(MapProviderToSummary).ToArray();
        
        private ProviderSummary MapProviderToSummary(Provider provider) =>
            new ProviderSummary
            {
                Name = provider.Name,
                Id = provider.ProviderId,
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
                LAOrg = provider.LAOrg,
                CrmAccountId = provider.CrmAccountId,
                LegalName = provider.LegalName,
                NavVendorNo = provider.NavVendorNo,
                DfeEstablishmentNumber = provider.DfeEstablishmentNumber,
                Status = provider.Status,
                PhaseOfEducation = provider.PhaseOfEducation,
                ReasonEstablishmentClosed = provider.ReasonEstablishmentClosed,
                ReasonEstablishmentOpened = provider.ReasonEstablishmentOpened,
                Successor = provider.Successor,
                TrustStatus = provider.TrustStatus,
                TrustName = provider.TrustName,
                TrustCode = provider.TrustCode,
                Town = provider.Town,
                Postcode = provider.Postcode,
                CompaniesHouseNumber = provider.CompaniesHouseNumber,
                GroupIdNumber = provider.GroupIdNumber,
                RscRegionName = provider.RscRegionName,
                RscRegionCode = provider.RscRegionCode,
                GovernmentOfficeRegionName = provider.GovernmentOfficeRegionName,
                GovernmentOfficeRegionCode = provider.GovernmentOfficeRegionCode,
                DistrictCode = provider.DistrictCode,
                DistrictName = provider.DistrictName,
                WardName = provider.WardName,
                WardCode = provider.WardCode,
                CensusWardCode = provider.CensusWardCode,
                CensusWardName = provider.CensusWardName,
                MiddleSuperOutputAreaCode = provider.MiddleSuperOutputAreaCode,
                MiddleSuperOutputAreaName = provider.MiddleSuperOutputAreaName,
                LowerSuperOutputAreaCode = provider.LowerSuperOutputAreaCode,
                LowerSuperOutputAreaName = provider.LowerSuperOutputAreaName,
                ParliamentaryConstituencyCode = provider.ParliamentaryConstituencyCode,
                ParliamentaryConstituencyName = provider.ParliamentaryConstituencyName,
                CountryCode = provider.CountryCode,
                CountryName = provider.CountryName,
                LocalGovernmentGroupTypeCode = provider.LocalGovernmentGroupTypeCode,
                LocalGovernmentGroupTypeName = provider.LocalGovernmentGroupTypeName,
                Street = provider.Street,
                Locality = provider.Locality,
                Address3 = provider.Address3,
                PaymentOrganisationIdentifier = provider.PaymentOrganisationIdentifier,
                PaymentOrganisationName = provider.PaymentOrganisationName,
                ProviderTypeCode = provider.ProviderTypeCode,
                ProviderSubTypeCode = provider .ProviderSubTypeCode,
                PreviousLaCode = provider.PreviousLACode,
                PreviousLaName = provider.PreviousLAName,
                PreviousEstablishmentNumber = provider.PreviousEstablishmentNumber,
                FurtherEducationTypeCode = provider.FurtherEducationTypeCode,
                FurtherEducationTypeName = provider.FurtherEducationTypeName,
                Predecessors = provider.Predecessors,
                Successors = provider.Successors
            };
        
        private Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);
            
            return providerBuilder.Build();
        }

        private SpecificationSummary NewSpecificationSummary(Action<ApiSpecificationSummaryBuilder> setUp = null)
        {
            ApiSpecificationSummaryBuilder specificationSummaryBuilder = new ApiSpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);
            
            return specificationSummaryBuilder.Build();
        }

        private ProviderVersion NewProviderVersion(Action<ProviderVersionBuilder> setUp = null)
        {
            ProviderVersionBuilder providerVersionBuilder = new ProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);
            
            return providerVersionBuilder.Build();
        }
        
        private string NewRandomString() => new RandomString();
    }
}
