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
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.MappingProfiles;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;
using FundingDataZoneProvider = CalculateFunding.Common.ApiClient.FundingDataZone.Models.Provider;
using CalculateFunding.Common.JobManagement;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Tests.Common.Builders;
using Serilog;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionUpdateServiceTests
    {
        private const string JobId = "jobId";
        private const string FundingStreamId = "fundingstream-id";
        private const string ProviderSnapshotId = "providersnapshot-id";
        private const string FundingPeriodId = "fundingperiod-id";

        private ProviderVersionUpdateService _service;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<IProviderVersionsMetadataRepository> _providerVersionsMetadataRepository;
        private Mock<IProviderVersionService> _providerVersionService;
        private Mock<IFundingDataZoneApiClient> _fundingDataZoneApiClient;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private Mock<IPublishingJobClashCheck> _publishingJobClashCheck;
        private Mock<IJobManagement> _jobManagement;
        private Mock<ILogger> _logger;
        private IProviderSnapshotPersistService _providerSnapshotPersistService;

        private IMapper _mapper;
        private IEnumerable<FundingStream> _fundingStreams;
        private string _jobId;
        private string _fundingStreamId;
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
            _logger = new Mock<ILogger>();
            _jobManagement = new Mock<IJobManagement>();

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

            _service = new ProviderVersionUpdateService(
                _policiesApiClient.Object,
                _logger.Object,
                providersResiliencePolicies,
                _providerVersionsMetadataRepository.Object,
                _specificationsApiClient.Object,
                _publishingJobClashCheck.Object,
                _providerSnapshotPersistService,
                _fundingDataZoneApiClient.Object,
                _mapper,
                _jobManagement.Object
            );

            int previousProviderSnapshotId = NewRandomInteger();

            _fundingPeriodId = NewRandomString();
            _currentProviderSnapshotId = NewRandomInteger();
            _latestProviderSnapshotId = NewRandomInteger();
            _specificationName = NewRandomString();
            _specificationDescription = NewRandomString();
            _specificationId = NewRandomString();
            _fundingStreamId = NewRandomString();

            _fundingStreams = new List<FundingStream>
            {
                new FundingStream
                {
                    Id = _fundingStreamId
                }
            };

            _fundingConfigurations = new List<FundingConfiguration>
            {
                new FundingConfiguration
                {
                    FundingStreamId = _fundingStreamId,
                    FundingPeriodId = _fundingPeriodId,
                    UpdateCoreProviderVersion = UpdateCoreProviderVersion.ToLatest
                }
            };

            _currentProviderVersions = new List<CurrentProviderVersion>
            {
                new CurrentProviderVersion
                {
                    Id = $"Current_{_fundingStreamId}",
                    ProviderSnapshotId = _currentProviderSnapshotId,
                    FundingPeriod = new List<ProviderSnapShotByFundingPeriod>
                    {
                        new ProviderSnapShotByFundingPeriod
                        {
                            FundingPeriodName = NewRandomString(),
                            ProviderSnapshotId = _latestProviderSnapshotId,
                            ProviderVersionId =NewRandomString()
                        }
                    }
                }
            };           

            ProviderSnapshot latestProviderSnapshot = new ProviderSnapshot
            {
                TargetDate = DateTime.UtcNow.AddDays(2),
                Version = 3,
                ProviderSnapshotId = _latestProviderSnapshotId,
                FundingStreamCode = _fundingStreamId,
                FundingPeriodName = _fundingPeriodId,
            };

            _providerSnapshots = new List<ProviderSnapshot>
            {
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(1),
                    Version = 1,
                    ProviderSnapshotId = previousProviderSnapshotId,
                    FundingStreamCode = NewRandomString(),
                    FundingPeriodName = _fundingPeriodId,
                },
                new ProviderSnapshot
                {
                    TargetDate = DateTime.UtcNow.AddDays(2),
                    Version = 2,
                    ProviderSnapshotId = previousProviderSnapshotId,
                    FundingStreamCode = NewRandomString(),
                    FundingPeriodName = _fundingPeriodId,
                },
                latestProviderSnapshot
            };

            _specificationProviderVersionId = latestProviderSnapshot.ProviderVersionId;

            _specificationSummaries = new List<SpecificationSummary>
            {
                new SpecificationSummary
                {
                    Id = _specificationId,
                    FundingStreams = new List<FundingStream>
                    {
                        new FundingStream
                        {
                            Id = _fundingStreamId
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

            _jobId = NewRandomString();
        }

        [TestMethod]
        public async Task CheckProviderVersionUpdate_GivenFundingConfigurationsByFundingStreamIdNotFound_ThrowsError()
        {
            GivenErrorGetFundingConfigurationsByFundingStreamId();
            AndTheCurrentProviderVersionSaved();
            AndTheProviderVersionUploadSucceeds(null);
            AndTheFundingDataZoneProvidersForTheSnapshotForAllFundingStreamsWithFundingPeriod();
            AndTheMetadataFundingStreams();
            AndTheProviderSnapshotsForFundingStream();
            AndTheFundingDataZoneProvidersForTheSnapshot();
            AndTheSpecificationsUseTheLatestProviderSnapshot();

            await WhenTheProviderVersionUpdate(NewValidMessage());

            _logger.Verify(_ => _.Error($"Unable to retrieve funding configs for funding stream with ID: {_fundingStreamId}"), Times.Once);
        }

        [TestMethod]
        public void CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatestReturnsBadRequest_ThrowsError()
        {
            GivenTheFundingStreams();
            AndTheCurrentProviderVersions();
            AndTheCurrentProviderVersionSaved();
            AndTheFundingConfigurationsForTheFundingStreamId();
            AndTheFundingDataZoneProvidersForTheSnapshotForAllFundingStreamsWithFundingPeriod();
            AndTheMetadataFundingStreams();
            AndTheFundingDataZoneProvidersForTheSnapshot();
            AndTheProviderVersionUploadSucceeds(null);
            AndTheProviderSnapshotsForFundingStream();
            AndErrorGetSpecificationsWithProviderVersionUpdatesAsUseLatest();

            Func<Task> invocation = async () => await WhenTheProviderVersionUpdate(NewValidMessage());

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Unable to retrieve specification with provider version updates as use latest");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatest_UpdatesSpecification(bool currentVersionExists)
        {
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheFundingStreams();
            if (currentVersionExists)
            {
                AndTheCurrentProviderVersions();
            }
            AndTheCurrentProviderVersionSaved();
            AndTheCurrentProviderVersionsMetadata();
            AndTheFundingConfigurationsForTheFundingStreamId();
            AndTheFundingDataZoneProvidersForTheSnapshotForAllFundingStreamsWithFundingPeriod();
            AndTheMetadataFundingStreams();
            AndTheProviderSnapshotsForFundingStream();
            AndTheProviderSnapshotsForFundingStreamWithFundingPeriod();
            AndTheFundingDataZoneProvidersForTheSnapshot(expectedProviderOne, expectedProviderTwo);
            AndTheSpecificationsUseTheLatestProviderSnapshot();
            AndTheProviderVersionUploadSucceeds(null, expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            await WhenTheProviderVersionUpdate(NewValidMessage());

            ThenUpdatesSpecification(_specificationId);
        }

        [TestMethod]
        public async Task CheckProviderVersionUpdate_GivenGetSpecificationsWithProviderVersionUpdatesAsUseLatestNotFound_ThenDoesNotUpdatesSpecification()
        {
            GivenTheFundingStreams();
            AndTheCurrentProviderVersions();
            AndTheCurrentProviderVersionSaved();
            AndTheCurrentProviderVersionsMetadata();
            AndTheFundingConfigurationsForTheFundingStreamId();
            AndTheFundingDataZoneProvidersForTheSnapshotForAllFundingStreamsWithFundingPeriod();
            AndTheMetadataFundingStreams();
            AndTheProviderSnapshotsForFundingStream();
            AndTheProviderSnapshotsForFundingStreamWithFundingPeriod();
            AndTheFundingDataZoneProvidersForTheSnapshot();
            AndTheProviderVersionUploadSucceeds(null);
            AndNoSpecificationsUseTheLatestProviderVersion();

            await WhenTheProviderVersionUpdate(NewValidMessage());

            ThenDoesNotUpdatesSpecification(_specificationId);
        }

        [TestMethod]
        public async Task SkipsUpdatesForFundingStreamWithoutLatestProviderSnapshot()
        {
            IEnumerable<ProviderSnapshot> providerSnapshots = Array.Empty<ProviderSnapshot>();

            GivenTheFundingStreams();
            AndTheCurrentProviderVersions();
            AndTheCurrentProviderVersionSaved();
            AndTheFundingConfigurationsForTheFundingStreamId();
            AndTheFundingDataZoneProvidersForTheSnapshotForAllFundingStreamsWithFundingPeriod();
            AndTheMetadataFundingStreams();
            AndTheFundingDataZoneProvidersForTheSnapshot();
            AndTheProviderSnapshotsForFundingStream();
            AndTheSpecificationsUseTheLatestProviderSnapshot();
            AndTheProviderVersionUploadSucceeds(null);
            AndThereIsAPublishingJobClashingWithTheProviderSnapshotForTheSpecifications();

            await WhenTheProviderVersionUpdate(NewValidMessage());

            ThenDoesNotUpdatesSpecification(_specificationId);
        }

        [TestMethod]
        public async Task SkipsUpdatesForProviderSnapshotsInUseBySpecificationsWithRunningPublishingJobs()
        {
            GivenTheFundingStreams();
            AndTheCurrentProviderVersions();
            AndTheCurrentProviderVersionSaved();
            AndTheFundingConfigurationsForTheFundingStreamId();
            AndTheFundingDataZoneProvidersForTheSnapshotForAllFundingStreamsWithFundingPeriod();
            AndTheMetadataFundingStreams();
            AndTheProviderSnapshotsForFundingStream();
            AndTheFundingDataZoneProvidersForTheSnapshot();
            AndTheProviderVersionUploadSucceeds(null);
            AndTheSpecificationsUseTheLatestProviderSnapshot();
            AndThereIsAPublishingJobClashingWithTheProviderSnapshotForTheSpecifications();

            await WhenTheProviderVersionUpdate(NewValidMessage());

            ThenDoesNotUpdatesSpecification(_specificationId);
        }

        private void ThenUpdatesSpecification(string specificationId) =>
            _specificationsApiClient
                .Verify(_ => _.UpdateSpecification(specificationId, It.IsAny<EditSpecificationModel>()), Times.Once);

        private void ThenDoesNotUpdatesSpecification(string specificationId) =>
            _specificationsApiClient
                .Verify(_ => _.UpdateSpecification(specificationId, It.IsAny<EditSpecificationModel>()), Times.Never);

        private void GivenErrorGetFundingConfigurationsByFundingStreamId() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfigurationsByFundingStreamId(_fundingStreamId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingConfiguration>>(HttpStatusCode.BadRequest));

        private void GivenTheFundingStreams() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingStream>>(HttpStatusCode.OK, _fundingStreams));

        private void AndTheFundingConfigurationsForTheFundingStreamId() =>
            _policiesApiClient
                .Setup(_ => _.GetFundingConfigurationsByFundingStreamId(_fundingStreamId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingConfiguration>>(HttpStatusCode.OK, _fundingConfigurations));

        private void AndTheCurrentProviderVersions() =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.GetAllCurrentProviderVersions())
                .ReturnsAsync(new List<CurrentProviderVersion>(_currentProviderVersions));

        private void AndTheCurrentProviderVersionsMetadata() =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.GetCurrentProviderVersion(_fundingStreamId))
                .ReturnsAsync(new CurrentProviderVersion() { ProviderSnapshotId = _latestProviderSnapshotId, Id = $"Current_{_fundingStreamId}"});

        private void AndTheCurrentProviderVersionSaved() =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.UpsertCurrentProviderVersion(It.Is<CurrentProviderVersion>(cpv => cpv.Id == $"Current_{_fundingStreamId}" &&
                                                                                                cpv.ProviderVersionId == _specificationProviderVersionId &&
                                                                                                cpv.ProviderSnapshotId == _latestProviderSnapshotId)))
                .ReturnsAsync(HttpStatusCode.OK);

        private void AndTheProviderSnapshotsForFundingStream() =>
            _fundingDataZoneApiClient
                .Setup(_ => _.GetLatestProviderSnapshotsForAllFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, _providerSnapshots));
        private void AndTheProviderSnapshotsForFundingStreamWithFundingPeriod() =>
            _fundingDataZoneApiClient
                .Setup(_ => _.GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod())
                .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, _providerSnapshots));

        private void AndTheFundingDataZoneProvidersForTheSnapshot(params FundingDataZoneProvider[] providers)
            => _fundingDataZoneApiClient.Setup(_ => _.GetProvidersInSnapshot(_latestProviderSnapshotId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingDataZoneProvider>>(HttpStatusCode.OK, providers));

        private void AndTheSpecificationsUseTheLatestProviderSnapshot() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.OK, _specificationSummaries));

        private void AndErrorGetSpecificationsWithProviderVersionUpdatesAsUseLatest() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.BadRequest));

        private void AndNoSpecificationsUseTheLatestProviderVersion() =>
            _specificationsApiClient
                .Setup(_ => _.GetSpecificationsWithProviderVersionUpdatesAsUseLatest())
                .ReturnsAsync(new ApiResponse<IEnumerable<SpecificationSummary>>(HttpStatusCode.NotFound));

        private void AndThereIsAPublishingJobClashingWithTheProviderSnapshotForTheSpecifications()
            => _publishingJobClashCheck.Setup(_ => _.PublishingJobsClashWithFundingStreamCoreProviderUpdate(_specificationId))
                .ReturnsAsync(true);

        private void AndTheProviderVersionUploadSucceeds(IActionResult actionResult = null,
            params string[] providerIds)
            => SetupProviderVersionUpload(_specificationProviderVersionId, providerIds, true, actionResult);

        private void AndTheFundingDataZoneProvidersForTheSnapshotForAllFundingStreamsWithFundingPeriod() =>
           _fundingDataZoneApiClient
               .Setup(_ => _.GetLatestProviderSnapshotsForAllFundingStreamsWithFundingPeriod())
               .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, _providerSnapshots));

        private void AndTheMetadataFundingStreams() =>
            _providerVersionsMetadataRepository
                .Setup(_ => _.GetCurrentProviderVersion(_fundingStreamId))
                .ReturnsAsync( _currentProviderVersions.FirstOrDefault());

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

        private Message NewValidMessage(Action<MessageBuilder> overrides = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder()
                .WithUserProperty(JobId, _jobId)
                .WithUserProperty(FundingStreamId, _fundingStreamId)
                .WithUserProperty(ProviderSnapshotId, _latestProviderSnapshotId.ToString())
                .WithUserProperty(FundingPeriodId, _fundingPeriodId);

            overrides?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }

        private async Task WhenTheProviderVersionUpdate(Message message) =>
            await _service.Process(message);

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