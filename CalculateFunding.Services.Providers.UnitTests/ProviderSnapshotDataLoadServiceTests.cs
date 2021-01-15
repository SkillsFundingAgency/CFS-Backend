using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.MappingProfiles;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;
using FundingDataZoneProvider = CalculateFunding.Common.ApiClient.FundingDataZone.Models.Provider;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderSnapshotDataLoadServiceTests
    {
        private const string JobId = "jobId";
        private const string SpecificationId = "specification-id";
        private const string FundingStreamId = "fundingstream-id";
        private const string ProviderSnapshotId = "providerSanpshot-id"; //TODO; check this spelling
        private const string DisableQueueCalculationJobKey = "disableQueueCalculationJob";

        private Mock<ISpecificationsApiClient> _specifications;
        private Mock<IProviderVersionService> _providerVersionService;
        private Mock<IFundingDataZoneApiClient> _fundingDataZone;
        private Mock<IJobManagement> _jobs;

        private ProviderSnapshotDataLoadService _service;
        private string _specificationId;
        private string _fundingStreamId;
        private string _jobId;
        private int _providerSnapshotId;
        private string _disableQueueCalculationJobKey;

        [TestInitialize]
        public void Setup()
        {
            _specifications = new Mock<ISpecificationsApiClient>();
            _providerVersionService = new Mock<IProviderVersionService>();
            _fundingDataZone = new Mock<IFundingDataZoneApiClient>();
            _jobs = new Mock<IJobManagement>();

            _service = new ProviderSnapshotDataLoadService(
                Logger.None,
                _specifications.Object,
                _providerVersionService.Object,
                new ProvidersResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    FundingDataZoneApiClient = Policy.NoOpAsync()
                },
                _fundingDataZone.Object,
                new MapperConfiguration(c => { c.AddProfile<ProviderVersionsMappingProfile>(); }).CreateMapper(),
                _jobs.Object);

            _specificationId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _jobId = NewRandomString();
            _providerSnapshotId = NewRandomInt();
            _disableQueueCalculationJobKey = NewRandomString();
        }

        [TestMethod]
        public async Task ShouldDownloadAndSaveProviderVersionFDZProvidersWhenProviderVersionNotExistsForGivenSpecificationAndProviderSnapshot()
        {
            ProviderSnapshot providerSnapshot = NewProviderSnapshotForFundingStream();
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheProviderSnapshotForTheFundingStream(providerSnapshot);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);

            string providerVersionId = GetProviderVersionIdFromSnapshot(providerSnapshot);

            AndSettingTheProviderVersionForTheSpecificationSucceeds(providerVersionId);
            AndTheProviderVersionUploadSucceeds(providerVersionId, null, expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            await WhenTheProviderSnapshotDataIsLoaded(NewValidMessage());

            ThenTheProviderVersionWasSetOnTheSpecification();
            AndTheProviderVersionsWereUploaded();
            AndAMapFdzDatasetsJobWasQueued();
        }


        [TestMethod]
        public void ShouldProviderVersionFDZProvidersThrowsExceptionWhenUploadProviderVersionReturnsError()
        {
            ProviderSnapshot providerSnapshot = NewProviderSnapshotForFundingStream();
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheProviderSnapshotForTheFundingStream(providerSnapshot);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);

            string providerVersionId = GetProviderVersionIdFromSnapshot(providerSnapshot);

            AndSettingTheProviderVersionForTheSpecificationSucceeds(providerVersionId);
            AndTheProviderVersionUploadFails(providerVersionId, null, expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            Func<Task> invocation = async () => await WhenTheProviderSnapshotDataIsLoaded(NewValidMessage());

            invocation
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to upload provider version {providerVersionId}. ");
        }


        [TestMethod]
        public void ShouldProviderVersionFDZProvidersThrowsExceptionWhenUploadProviderVersionReturnsValidationError()
        {
            ProviderSnapshot providerSnapshot = NewProviderSnapshotForFundingStream();
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheProviderSnapshotForTheFundingStream(providerSnapshot);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);

            string providerVersionId = GetProviderVersionIdFromSnapshot(providerSnapshot);

            AndSettingTheProviderVersionForTheSpecificationSucceeds(providerVersionId);
            AndTheProviderVersionUploadFails(providerVersionId, new ConflictResult(), expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            Func<Task> invocation = async () => await WhenTheProviderSnapshotDataIsLoaded(NewValidMessage());

            invocation
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to upload provider version {providerVersionId}. ProviderVersion alreay exists for - {providerVersionId}");
        }

        [TestMethod]
        public void ShouldProviderVersionFDZProvidersThrowsExceptionWhenSetProviderVersionReturnsError()
        {
            ProviderSnapshot providerSnapshot = NewProviderSnapshotForFundingStream();
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheProviderSnapshotForTheFundingStream(providerSnapshot);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);

            string providerVersionId = GetProviderVersionIdFromSnapshot(providerSnapshot);

            AndSettingTheProviderVersionForTheSpecificationFails(providerVersionId);
            AndTheProviderVersionUploadSucceeds(providerVersionId, null, expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            Func<Task> invocation = async () => await WhenTheProviderSnapshotDataIsLoaded(NewValidMessage());

            invocation
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Unable to update the specification - {_specificationId}, with provider version id  - {providerVersionId}. HttpStatusCode - {HttpStatusCode.BadRequest}");
        }

        [TestMethod]
        public void ShouldThrowAnExceptionIfProviderSnapShotIdNotSet()
        {
            ProviderSnapshot providerSnapshot = NewProviderSnapshotForFundingStream();
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheProviderSnapshotForTheFundingStream(providerSnapshot);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);

            string providerVersionId = GetProviderVersionIdFromSnapshot(providerSnapshot);

            AndSettingTheProviderVersionForTheSpecificationSucceeds(providerVersionId);
            AndTheProviderVersionUploadSucceeds(providerVersionId, null, expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            string expectedExceptionMessage = "Invalid provider snapshot id";

            AndTheJobCreationThrowsAnException(expectedExceptionMessage);

            Func<Task> invocation = async () => await WhenTheProviderSnapshotDataIsLoaded(NewValidMessage(_ => _.WithoutUserProperty(ProviderSnapshotId)));

            invocation
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be(expectedExceptionMessage);
        }

        [TestMethod]
        public void ShouldThrowAnExceptionIfAnyMapFdzDatasetsJobFailedToQueue()
        {
            ProviderSnapshot providerSnapshot = NewProviderSnapshotForFundingStream();
            FundingDataZoneProvider expectedProviderOne = NewFundingDataZoneProvider();
            FundingDataZoneProvider expectedProviderTwo = NewFundingDataZoneProvider();

            GivenTheProviderSnapshotForTheFundingStream(providerSnapshot);
            AndTheFundingDataZoneProvidersForTheSnapshot(providerSnapshot.ProviderSnapshotId, expectedProviderOne, expectedProviderTwo);

            string providerVersionId = GetProviderVersionIdFromSnapshot(providerSnapshot);

            AndSettingTheProviderVersionForTheSpecificationSucceeds(providerVersionId);
            AndTheProviderVersionUploadSucceeds(providerVersionId, null, expectedProviderOne.ProviderId, expectedProviderTwo.ProviderId);

            string expectedExceptionMessage = NewRandomString();

            AndTheJobCreationThrowsAnException(expectedExceptionMessage);

            Func<Task> invocation = async () => await WhenTheProviderSnapshotDataIsLoaded(NewValidMessage());

            invocation
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be(expectedExceptionMessage);
        }

        private async Task WhenTheProviderSnapshotDataIsLoaded(Message message)
            => await _service.Process(message);

        private void GivenTheProviderSnapshotForTheFundingStream(ProviderSnapshot providerSnapshot)
            => _fundingDataZone.Setup(_ => _.GetProviderSnapshotsForFundingStream(_fundingStreamId))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK,
                    new[]
                    {
                        providerSnapshot
                    }));

        private void AndTheFundingDataZoneProvidersForTheSnapshot(int providerSnapshotId,
            params FundingDataZoneProvider[] providers)
            => _fundingDataZone.Setup(_ => _.GetProvidersInSnapshot(providerSnapshotId))
                .ReturnsAsync(new ApiResponse<IEnumerable<FundingDataZoneProvider>>(HttpStatusCode.OK, providers));

        private void AndSettingTheProviderVersionForTheSpecificationSucceeds(string providerVersionId)
            => SetUpSetProviderVersion(providerVersionId, HttpStatusCode.OK);

        private void AndSettingTheProviderVersionForTheSpecificationFails(string providerVersionId)
            => SetUpSetProviderVersion(providerVersionId, HttpStatusCode.BadRequest);

        private void SetUpSetProviderVersion(string providerVersionId,
            HttpStatusCode statusCode)
        {
            _specifications.Setup(_ => _.SetProviderVersion(_specificationId, providerVersionId))
                .ReturnsAsync(statusCode)
                .Verifiable();
        }


        private void AndTheProviderVersionUploadSucceeds(string providerVersionId,
            IActionResult actionResult = null,
            params string[] providerIds)
            => SetupProviderVersionUpload(providerVersionId, providerIds, true, actionResult);

        private void AndTheProviderVersionUploadFails(string providerVersionId,
            IActionResult actionResult = null,
            params string[] providerIds)
            => SetupProviderVersionUpload(providerVersionId, providerIds, false, actionResult);

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

        private void AndTheJobCreationThrowsAnException(string expectedMessage)
            => _jobs.Setup(_ => _.QueueJob(It.IsAny<JobCreateModel>()))
                .Throws(new Exception(expectedMessage));

        private void ThenTheProviderVersionWasSetOnTheSpecification()
            => _specifications
                .Verify();

        private void AndTheProviderVersionsWereUploaded()
            => _providerVersionService
                .Verify();

        private void AndAMapFdzDatasetsJobWasQueued()
            => _jobs.Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job => job.JobDefinitionId == JobConstants.DefinitionNames.MapFdzDatasetsJob
                                                                         && job.SpecificationId == _specificationId
                                                                         && job.Properties.ContainsKey(SpecificationId)
                                                                         && job.Properties[SpecificationId] == _specificationId
                                                                         && job.Properties[DisableQueueCalculationJobKey] == _disableQueueCalculationJobKey)),
                Times.Once);

        private static string GetProviderVersionIdFromSnapshot(ProviderSnapshot providerSnapshot) =>
            $"{providerSnapshot.FundingStreamCode}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshot.ProviderSnapshotId}";

        private Message NewValidMessage(Action<MessageBuilder> overrides = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder()
                .WithUserProperty(JobId, _jobId)
                .WithUserProperty(SpecificationId, _specificationId)
                .WithUserProperty(FundingStreamId, _fundingStreamId)
                .WithUserProperty(ProviderSnapshotId, _providerSnapshotId.ToString())
                .WithUserProperty(DisableQueueCalculationJobKey, _disableQueueCalculationJobKey);

            overrides?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }

        private ProviderSnapshot NewProviderSnapshotForFundingStream(Action<ProviderSnapshotBuilder> overrides = null)
        {
            ProviderSnapshotBuilder providerSnapshotBuilder = new ProviderSnapshotBuilder()
                .WithId(_providerSnapshotId)
                .WithFundingStreamCode(_fundingStreamId);

            overrides?.Invoke(providerSnapshotBuilder);

            return providerSnapshotBuilder.Build();
        }

        private FundingDataZoneProvider NewFundingDataZoneProvider(Action<FundingDataZoneProviderBuilder> setUp = null)
        {
            FundingDataZoneProviderBuilder fundingDataZoneProviderBuilder = new FundingDataZoneProviderBuilder();

            setUp?.Invoke(fundingDataZoneProviderBuilder);

            return fundingDataZoneProviderBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private static int NewRandomInt() => new RandomNumberBetween(1, int.MaxValue);
    }
}