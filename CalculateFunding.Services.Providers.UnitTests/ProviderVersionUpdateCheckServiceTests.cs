using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Polly;
using Serilog.Core;
using FundingDataZoneProvider = CalculateFunding.Common.ApiClient.FundingDataZone.Models.Provider;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionUpdateCheckServiceTests
    {
        private ProviderVersionUpdateCheckService _service;
        private Mock<ICacheProvider> _cachProvider;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<IProviderVersionsMetadataRepository> _providerVersionsMetadataRepository;
        private Mock<IProviderVersionService> _providerVersionService;
        private Mock<IFundingDataZoneApiClient> _fundingDataZoneApiClient;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private Mock<IPublishingJobClashCheck> _publishingJobClashCheck;
        private IProviderSnapshotPersistService _providerSnapshotPersistService;

        private IMapper _mapper;
        private IEnumerable<FundingStream> _fundingStreams;
        private string _fundingStreamOneId;
        private string _fundingPeriodId;
        private int _currentProviderSnapshotId;
        private int _latestProviderSnapshotId;
        private string _specificationId;
        private string _specificationName;
        private string _specificationDescription;
        private string _specificationProviderVersionId;

        private IEnumerable<FundingConfiguration> _fundingConfigurations;
        private IEnumerable<CurrentProviderVersion> _currentProviderVersions;
        private IEnumerable<ProviderSnapshot> _providerSnapshots;
        private IEnumerable<SpecificationSummary> _specificationSummaries;

        [TestInitialize]
        public void SetUp()
        {
            MapperConfiguration mappingConfig = new MapperConfiguration(c => { c.AddProfile<ProviderVersionsMappingProfile>(); });
            _mapper = mappingConfig.CreateMapper();

            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _providerVersionsMetadataRepository = new Mock<IProviderVersionsMetadataRepository>();
            _fundingDataZoneApiClient = new Mock<IFundingDataZoneApiClient>();
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _publishingJobClashCheck = new Mock<IPublishingJobClashCheck>();
            _providerVersionService = new Mock<IProviderVersionService>();
            _cachProvider = new Mock<ICacheProvider>();

            ProvidersResiliencePolicies providersResiliencePolicies = new ProvidersResiliencePolicies
            {
                PoliciesApiClient = Policy.NoOpAsync(),
                ProviderVersionMetadataRepository = Policy.NoOpAsync(),
                FundingDataZoneApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync()
            };

            _providerSnapshotPersistService = new ProviderSnapshotPersistService(_providerVersionService.Object,
                _fundingDataZoneApiClient.Object,
                Logger.None,
                _mapper,
                providersResiliencePolicies);

            _service = new ProviderVersionUpdateCheckService(
                _policiesApiClient.Object,
                _cachProvider.Object,
                Logger.None,
                providersResiliencePolicies,
                _providerVersionsMetadataRepository.Object,
                _fundingDataZoneApiClient.Object,
                _specificationsApiClient.Object,
                _mapper,
                _publishingJobClashCheck.Object,
                _providerSnapshotPersistService
            );

            int previousProviderSnapshotId = NewRandomInteger();

            _fundingStreamOneId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _currentProviderSnapshotId = NewRandomInteger();
            _latestProviderSnapshotId = NewRandomInteger();
            _specificationName = NewRandomString();
            _specificationDescription = NewRandomString();
            _specificationProviderVersionId = NewRandomString();
            _specificationId = NewRandomString();

            _fundingStreams = new List<FundingStream>
            {
                new FundingStream
                {
                    Id = _fundingStreamOneId
                }
            };

            _fundingConfigurations = new List<FundingConfiguration>
            {
                new FundingConfiguration
                {
                    FundingStreamId = _fundingStreamOneId,
                    FundingPeriodId = _fundingPeriodId,
                    UpdateCoreProviderVersion = UpdateCoreProviderVersion.ToLatest
                }
            };

            _currentProviderVersions = new List<CurrentProviderVersion>
            {
                new CurrentProviderVersion
                {
                    Id = $"Current_{_fundingStreamOneId}",
                    ProviderSnapshotId = _currentProviderSnapshotId
                }
            };

            _providerSnapshots = new List<ProviderSnapshot>
            {
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(1),
                    Version = 1,
                    ProviderSnapshotId = previousProviderSnapshotId,
                    FundingStreamCode = _fundingStreamOneId
                },
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(2),
                    Version = 2,
                    ProviderSnapshotId = previousProviderSnapshotId,
                    FundingStreamCode = NewRandomString()
                },
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(2),
                    Version = 3,
                    ProviderSnapshotId = _latestProviderSnapshotId,
                    FundingStreamCode = NewRandomString()
                }
            };

            _specificationSummaries = new List<SpecificationSummary>
            {
                new SpecificationSummary
                {
                    Id = _specificationId,
                    FundingStreams = new List<FundingStream>
                    {
                        new FundingStream
                        {
                            Id = _fundingStreamOneId
                        }
                    },
                    FundingPeriod = new Reference
                    {
                        Id = _fundingPeriodId
                    },
                    Name = _specificationName,
                    Description = _specificationDescription,
                    ProviderVersionId = _specificationProviderVersionId,
                    ProviderSnapshotId = 1
                }
            };
        }

        [TestMethod]
        public void CheckProviderVersionUpdate_GivenFundingStreamsNotFound_ThrowsError()
        {
            GivenErrorGetFundingStreams();

            Func<Task> invocation = WhenTheProviderVersionUpdateIsChecked;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Unable to retrieve funding streams");
        }

        [TestMethod]
        public void CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatestReturnsBadRequest_ThrowsError()
        {
            ProviderSnapshot providerSnapshot = _providerSnapshots.Single(_ => _.Version == 1);
            string providerVersionId = $"{providerSnapshot.FundingStreamCode}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshot.ProviderSnapshotId}";
            string currentProviderId = $"Current_{providerSnapshot.FundingStreamCode}";

            GivenTheFundingStreams(_fundingStreams);
            AndTheCurrentProviderVersions(_currentProviderVersions);
            AndTheCurrentProviderVersionSaved(currentProviderId, providerVersionId, providerSnapshot.ProviderSnapshotId);
            AndTheFundingConfigurationsForTheFundingStreamId(_fundingConfigurations, _fundingStreamOneId);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId);
            AndTheProviderVersionUploadSucceeds(providerSnapshot.ProviderVersionId, null);
            AndTheProviderSnapshotsForFundingStream(_providerSnapshots);
            AndErrorGetSpecificationsWithProviderVersionUpdatesAsUseLatest();

            Func<Task> invocation = WhenTheProviderVersionUpdateIsChecked;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Unable to retrieve specification with provider version updates as use latest");
        }

        [TestMethod]
        public async Task CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatest_UpdatesSpecification()
        {
            ProviderSnapshot providerSnapshot = _providerSnapshots.Single(_ => _.Version == 1);
            string providerVersionId = $"{providerSnapshot.FundingStreamCode}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshot.ProviderSnapshotId}";
            string currentProviderId = $"Current_{providerSnapshot.FundingStreamCode}";
            
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheFundingStreams(_fundingStreams);
            AndTheCurrentProviderVersions(_currentProviderVersions);
            AndTheCurrentProviderVersionSaved(currentProviderId, providerVersionId, providerSnapshot.ProviderSnapshotId);
            AndTheFundingConfigurationsForTheFundingStreamId(_fundingConfigurations, _fundingStreamOneId);
            AndTheProviderSnapshotsForFundingStream(_providerSnapshots);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);
            AndTheSpecificationsUseTheLatestProviderSnapshot(_specificationSummaries);
            AndTheProviderVersionUploadSucceeds(providerVersionId, null, expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            await WhenTheProviderVersionUpdateIsChecked();

            ThenUpdatesSpecification(_specificationId);
        }

        [TestMethod]
        public async Task CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatestNotFound_ThenDoesNotUpdatesSpecification()
        {
            ProviderSnapshot providerSnapshot = _providerSnapshots.FirstOrDefault();
            GivenTheFundingStreams(_fundingStreams);
            AndTheCurrentProviderVersions(_currentProviderVersions);
            AndTheFundingConfigurationsForTheFundingStreamId(_fundingConfigurations, _fundingStreamOneId);
            AndTheProviderSnapshotsForFundingStream(_providerSnapshots);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId);
            AndTheProviderVersionUploadSucceeds(providerSnapshot.ProviderVersionId, null);
            AndNoSpecificationsUseTheLatestProviderVersion();

            await WhenTheProviderVersionUpdateIsChecked();

            ThenDoesNotUpdatesSpecification(_specificationId);
        }

        [TestMethod]
        public async Task SkipsUpdatesIfTrackingDisabled()
        {
            GivenDisableTrackLatest(true);

            await WhenTheProviderVersionUpdateIsChecked();

            ThenDoesNotUpdatesSpecification(_specificationId);
        }

        [TestMethod]
        public async Task SkipsUpdatesForProviderSnapshotsInUseBySpecificationsWithRunningPublishingJobs()
        {
            ProviderSnapshot providerSnapshot = _providerSnapshots.FirstOrDefault();
            GivenTheFundingStreams(_fundingStreams);
            AndTheCurrentProviderVersions(_currentProviderVersions);
            AndTheFundingConfigurationsForTheFundingStreamId(_fundingConfigurations, _fundingStreamOneId);
            AndTheProviderSnapshotsForFundingStream(_providerSnapshots);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId);
            AndTheProviderVersionUploadSucceeds(providerSnapshot.ProviderVersionId, null);
            AndTheSpecificationsUseTheLatestProviderSnapshot(_specificationSummaries);
            AndThereIsAPublishingJobClashingWithTheProviderSnapshotForTheSpecifications(_specificationId);

            await WhenTheProviderVersionUpdateIsChecked();

            ThenDoesNotUpdatesSpecification(_specificationId);
        }

        private void ThenUpdatesSpecification(string specificationId) =>
            _specificationsApiClient
                .Verify(_ => _.UpdateSpecification(specificationId, It.IsAny<EditSpecificationModel>()), Times.Once);

        private void ThenDoesNotUpdatesSpecification(string specificationId) =>
            _specificationsApiClient
                .Verify(_ => _.UpdateSpecification(specificationId, It.IsAny<EditSpecificationModel>()), Times.Never);

        private void GivenErrorGetFundingStreams() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStream>>(HttpStatusCode.BadRequest));

        private void GivenTheFundingStreams(IEnumerable<FundingStream> fundingStreams) =>
            _policiesApiClient
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStream>>(HttpStatusCode.OK, fundingStreams));

        private void GivenDisableTrackLatest(bool trackLatest)
        {
            _cachProvider
                .Setup(_ => _.GetAsync<bool>(CacheKeys.DisableTrackLatest, It.IsAny<JsonSerializerSettings>()))
                .ReturnsAsync(trackLatest);
        }

        private void AndTheFundingConfigurationsForTheFundingStreamId(IEnumerable<FundingConfiguration> fundingConfigurations,
            string fundingStreamOneId) =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfigurationsByFundingStreamId(fundingStreamOneId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingConfiguration>>(HttpStatusCode.OK, fundingConfigurations));

        private void AndErrorGetFundingConfigurationsByFundingStreamId() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfigurationsByFundingStreamId(_fundingStreamOneId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingConfiguration>>(HttpStatusCode.BadRequest));

        private void AndTheCurrentProviderVersions(IEnumerable<CurrentProviderVersion> currentProviderVersions) =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.GetAllCurrentProviderVersions())
                .ReturnsAsync(new List<CurrentProviderVersion>(currentProviderVersions));

        private void AndTheCurrentProviderVersionSaved(string id, string providerVersionId, int providerSnapshotId) =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.UpsertCurrentProviderVersion(It.Is<CurrentProviderVersion>(cpv => cpv.Id == id && 
                                                                                                cpv.ProviderVersionId == providerVersionId &&
                                                                                                cpv.ProviderSnapshotId == providerSnapshotId)))
                .ReturnsAsync(HttpStatusCode.OK);

        private void AndTheProviderSnapshotsForFundingStream(IEnumerable<ProviderSnapshot> providerSnapshots) =>
            _fundingDataZoneApiClient
                .Setup(_ => _.GetLatestProviderSnapshotsForAllFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, providerSnapshots));

        private void AndTheFundingDataZoneProvidersForTheSnapshot(int providerSnapshotId,
            params FundingDataZoneProvider[] providers)
            => _fundingDataZoneApiClient.Setup(_ => _.GetProvidersInSnapshot(providerSnapshotId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingDataZoneProvider>>(HttpStatusCode.OK, providers));

        private void AndTheSpecificationsUseTheLatestProviderSnapshot(IEnumerable<SpecificationSummary> specificationSummaries) =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.OK, specificationSummaries));

        private void AndErrorGetSpecificationsWithProviderVersionUpdatesAsUseLatest() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.BadRequest));

        private void AndNoSpecificationsUseTheLatestProviderVersion() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.NotFound));

        private void AndThereIsAPublishingJobClashingWithTheProviderSnapshotForTheSpecifications(string specificationId)
            => _publishingJobClashCheck.Setup(_ => _.PublishingJobsClashWithFundingStreamCoreProviderUpdate(specificationId))
                .ReturnsAsync(true);

        private void AndTheProviderVersionUploadSucceeds(string providerVersionId,
            IActionResult actionResult = null,
            params string[] providerIds)
            => SetupProviderVersionUpload(providerVersionId, providerIds, true, actionResult);

        private void SetupProviderVersionUpload(string providerVersionId,
            string[] providerIds,
            bool success,
            IActionResult actionResult)
        {
            _providerVersionService.Setup(_ => _.UploadProviderVersion(providerVersionId,
                    It.Is<ProviderVersionViewModel>(pv
                        => pv.Providers.Select(p => p.ProviderId).SequenceEqual(providerIds) &&
                        pv.Providers.All(p => p.ProviderVersionId == providerVersionId && p.ProviderVersionIdProviderId == $"{providerVersionId}_{p.ProviderId}"))))
                .ReturnsAsync((success, actionResult))
                .Verifiable();
        }

        private async Task WhenTheProviderVersionUpdateIsChecked() =>
            await _service.CheckProviderVersionUpdate();

        private FundingDataZoneProvider NewFundingDataZoneProvider(Action<FundingDataZoneProviderBuilder> setUp = null)
        {
            FundingDataZoneProviderBuilder fundingDataZoneProviderBuilder = new FundingDataZoneProviderBuilder();

            setUp?.Invoke(fundingDataZoneProviderBuilder);

            return fundingDataZoneProviderBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private static int NewRandomInteger() => new RandomNumberBetween(0, int.MaxValue);
    }
}