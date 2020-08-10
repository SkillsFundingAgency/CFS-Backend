using AutoMapper;
using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderSnapshotDataLoadServiceTests
    {
        private ILogger _logger;
        private ISpecificationsApiClient _specificationsApiClient;
        private IProviderVersionService _providerVersionService;
        private IFundingDataZoneApiClient _fundingDataZoneApiClient;
        private IMapper _mapper;
        private ProvidersResiliencePolicies _providerResiliencePolicies;
        private ProviderSnapshotDataLoadService _service;

        [TestInitialize]
        public void Setup()
        {
            _logger = Substitute.For<ILogger>();
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            _providerVersionService = Substitute.For<IProviderVersionService>();
            _fundingDataZoneApiClient = Substitute.For<IFundingDataZoneApiClient>();
            _mapper = new MapperConfiguration(c =>
            {
                c.AddProfile<ProviderVersionsMappingProfile>();
            }).CreateMapper();
            _providerResiliencePolicies = new ProvidersResiliencePolicies()
            {
                SpecificationsApiClient = Policy.NoOpAsync(),
                FundingDataZoneApiClient = Policy.NoOpAsync(),
            };

            _service = new ProviderSnapshotDataLoadService(
                _logger,
                _specificationsApiClient,
                _providerVersionService,
                _providerResiliencePolicies,
                _fundingDataZoneApiClient,
                _mapper);
        }

        [TestMethod]
        public async Task ShouldDownloadAndSaveProviderVersionFDZProvidersWhenProviderVersionNotExistsForGivenSpecificationAndProviderSnapshot()
        {
            // Arrange
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string jobId = NewRandomString();
            int providerSnapshotId = NewRandomInt();

            Message message = CreateMessage(jobId, specificationId, fundingStreamId, providerSnapshotId);
            ProviderSnapshot providerSnapshot = CreateProviderSnapshot(fundingStreamId, providerSnapshotId);
            string providerVersionId = $"{fundingStreamId}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshotId}";

            _fundingDataZoneApiClient.GetProviderSnapshotsForFundingStream(Arg.Is(fundingStreamId))
                .Returns(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, new[] { providerSnapshot }));
            _providerVersionService.Exists(providerVersionId)
                .Returns(false);
            _fundingDataZoneApiClient.GetProvidersInSnapshot(Arg.Is(providerSnapshotId))
                .Returns(new ApiResponse<IEnumerable<Provider>>(HttpStatusCode.OK, new[] { CreateFdzProvider(), CreateFdzProvider() }));
            _providerVersionService.UploadProviderVersion(Arg.Is(providerVersionId), Arg.Is<ProviderVersionViewModel>(_ => _.Name == providerSnapshot.Name))
                .Returns((true, null));

            _specificationsApiClient.SetProviderVersion(Arg.Is(specificationId), Arg.Is(providerVersionId))
                .Returns(HttpStatusCode.OK);

            // Act
            await _service.LoadProviderSnapshotData(message);

            // Assert
            await _fundingDataZoneApiClient
                .Received(1)
                .GetProviderSnapshotsForFundingStream(Arg.Is(fundingStreamId));

            await _providerVersionService
                .Received(1)
                .Exists(providerVersionId);

            await _fundingDataZoneApiClient
                .Received(1)
                .GetProvidersInSnapshot(Arg.Is(providerSnapshotId));

            await _providerVersionService
                 .Received(1)
                 .UploadProviderVersion(Arg.Is(providerVersionId), Arg.Is<ProviderVersionViewModel>(_ => _.Name == providerSnapshot.Name));

            await _specificationsApiClient
                .Received(1)
                .SetProviderVersion(Arg.Is(specificationId), Arg.Is(providerVersionId));
        }

        [TestMethod]
        public void ShouldProviderVersionFDZProvidersThrowsExceptionWhenUploadProviderVersionReturnsError()
        {
            // Arrange
            ILogger logger = CreateLogger();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string jobId = NewRandomString();
            int providerSnapshotId = NewRandomInt();
           

            Message message = CreateMessage(jobId, specificationId, fundingStreamId, providerSnapshotId);
            ProviderSnapshot providerSnapshot = CreateProviderSnapshot(fundingStreamId, providerSnapshotId);
            string providerVersionId = $"{fundingStreamId}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshotId}";
            string errorMessage = $"Failed to upload provider version {providerVersionId}. ";

            _fundingDataZoneApiClient.GetProviderSnapshotsForFundingStream(Arg.Is(fundingStreamId))
                .Returns(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, new[] { providerSnapshot }));
            _providerVersionService.Exists(providerVersionId)
                .Returns(false);
            _fundingDataZoneApiClient.GetProvidersInSnapshot(Arg.Is(providerSnapshotId))
                .Returns(new ApiResponse<IEnumerable<Provider>>(HttpStatusCode.OK, new[] { CreateFdzProvider(), CreateFdzProvider() }));
            _providerVersionService.UploadProviderVersion(Arg.Is(providerVersionId), Arg.Is<ProviderVersionViewModel>(_ => _.Name == providerSnapshot.Name))
                .Returns((false, null));

            _specificationsApiClient.SetProviderVersion(Arg.Is(specificationId), Arg.Is(providerVersionId))
                .Returns(HttpStatusCode.OK);

            // Act
            Func<Task> invocation = async () => await _service.LoadProviderSnapshotData(message);

            // Assert
            invocation
               .Should()
               .ThrowExactly<Exception>()
               .Which
               .Message
               .Should()
               .Be(errorMessage);

          
        }

        [TestMethod]
        public void ShouldProviderVersionFDZProvidersThrowsExceptionWhenUploadProviderVersionReturnsValidationError()
        {
            // Arrange
            ILogger logger = CreateLogger();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string jobId = NewRandomString();
            int providerSnapshotId = NewRandomInt();


            Message message = CreateMessage(jobId, specificationId, fundingStreamId, providerSnapshotId);
            ProviderSnapshot providerSnapshot = CreateProviderSnapshot(fundingStreamId, providerSnapshotId);
            string providerVersionId = $"{fundingStreamId}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshotId}";
            string errorMessage = $"Failed to upload provider version {providerVersionId}. ProviderVersion alreay exists for - {providerVersionId}";

            _fundingDataZoneApiClient.GetProviderSnapshotsForFundingStream(Arg.Is(fundingStreamId))
                .Returns(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, new[] { providerSnapshot }));
            _providerVersionService.Exists(providerVersionId)
                .Returns(false);
            _fundingDataZoneApiClient.GetProvidersInSnapshot(Arg.Is(providerSnapshotId))
                .Returns(new ApiResponse<IEnumerable<Provider>>(HttpStatusCode.OK, new[] { CreateFdzProvider(), CreateFdzProvider() }));
            _providerVersionService.UploadProviderVersion(Arg.Is(providerVersionId), Arg.Is<ProviderVersionViewModel>(_ => _.Name == providerSnapshot.Name))
                .Returns((false, new ConflictResult()));

            _specificationsApiClient.SetProviderVersion(Arg.Is(specificationId), Arg.Is(providerVersionId))
                .Returns(HttpStatusCode.OK);

            // Act
            Func<Task> invocation = async () => await _service.LoadProviderSnapshotData(message);

            // Assert
            invocation
               .Should()
               .ThrowExactly<Exception>()
               .Which
               .Message
               .Should()
               .Be(errorMessage);


        }

        [TestMethod]
        public void ShouldProviderVersionFDZProvidersThrowsExceptionWhenSetProviderVersionReturnsError()
        {
            // Arrange
            ILogger logger = CreateLogger();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string jobId = NewRandomString();
            int providerSnapshotId = NewRandomInt();


            Message message = CreateMessage(jobId, specificationId, fundingStreamId, providerSnapshotId);
            ProviderSnapshot providerSnapshot = CreateProviderSnapshot(fundingStreamId, providerSnapshotId);
            string providerVersionId = $"{fundingStreamId}-{providerSnapshot.TargetDate:yyyy}-{providerSnapshot.TargetDate:MM}-{providerSnapshot.TargetDate:dd}-{providerSnapshotId}";
            string errorMessage = $"Unable to update the specification - {specificationId}, with provider version id  - {providerVersionId}. HttpStatusCode - {HttpStatusCode.BadRequest}";

            _fundingDataZoneApiClient.GetProviderSnapshotsForFundingStream(Arg.Is(fundingStreamId))
                .Returns(new ApiResponse<IEnumerable<ProviderSnapshot>>(HttpStatusCode.OK, new[] { providerSnapshot }));
            _providerVersionService.Exists(providerVersionId)
                .Returns(false);
            _fundingDataZoneApiClient.GetProvidersInSnapshot(Arg.Is(providerSnapshotId))
                .Returns(new ApiResponse<IEnumerable<Provider>>(HttpStatusCode.OK, new[] { CreateFdzProvider(), CreateFdzProvider() }));
            _providerVersionService.UploadProviderVersion(Arg.Is(providerVersionId), Arg.Is<ProviderVersionViewModel>(_ => _.Name == providerSnapshot.Name))
                .Returns((true, null));

            _specificationsApiClient.SetProviderVersion(Arg.Is(specificationId), Arg.Is(providerVersionId))
                .Returns(HttpStatusCode.BadRequest);

            // Act
            Func<Task> invocation = async () => await _service.LoadProviderSnapshotData(message);

            // Assert
            invocation
               .Should()
               .ThrowExactly<Exception>()
               .Which
               .Message
               .Should()
               .Be(errorMessage);


        }

        private static Message CreateMessage(string jobId, string specificationId, string fundingStreamId, int providerSnapshotId)
        {
            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("fundingstream-id", fundingStreamId);
            message.UserProperties.Add("providerSanpshot-id", providerSnapshotId.ToString());

            return message;
        }

        private static ProviderSnapshot CreateProviderSnapshot(string fundingStreamId, int providerSnapshotId)
        {
            return new ProviderSnapshot()
            {
                Name = NewRandomString(),
                TargetDate = NewRandomDate(),
                FundingStreamCode = fundingStreamId,
                ProviderSnapshotId = providerSnapshotId
            };
        }

        private static Provider CreateFdzProvider()
        {
            return new Provider() { Name = NewRandomString() };
        }

        private async Task WhenLoadProviderSnapshotDataAreApplied(Message message)
        {
            await _service.LoadProviderSnapshotData(message);
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static string NewRandomString() => new RandomString();
        private static int NewRandomInt() => new RandomNumberBetween(1, 100000);
        private static DateTime NewRandomDate() => new RandomDateTime();
    }
}
