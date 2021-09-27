﻿using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Publishing.FundingManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ReleaseProvidersToChannelsServiceTests
    {
        private ReleaseProvidersToChannelsService _releaseProvidersToChannelsService;
        private Mock<ISpecificationService> _specificationService;
        private Mock<IPoliciesService> _policiesService;
        private Mock<IChannelsService> _channelsService;
        private Mock<IPublishedProvidersLoadContext> _publishedProvidersLoadContext;
        private Mock<IReleaseApprovedProvidersService> _releaseApprovedProvidersService;
        private Mock<ILogger> _logger;
        private Mock<IPrerequisiteCheckerLocator> _prerequisiteCheckerLocator;
        private Mock<IPrerequisiteChecker> _prerequisiteChecker;
        private Mock<IJobManagement> _jobManagement;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;

        [TestInitialize]
        public void SetUp()
        {
            _specificationService = new Mock<ISpecificationService>();
            _policiesService = new Mock<IPoliciesService>();
            _channelsService = new Mock<IChannelsService>();
            _publishedProvidersLoadContext = new Mock<IPublishedProvidersLoadContext>();
            _releaseApprovedProvidersService = new Mock<IReleaseApprovedProvidersService>();
            _logger = new Mock<ILogger>();
            _prerequisiteCheckerLocator = new Mock<IPrerequisiteCheckerLocator>();
            _prerequisiteChecker = new Mock<IPrerequisiteChecker>();
            _jobManagement = new Mock<IJobManagement>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();

            _releaseProvidersToChannelsService = new ReleaseProvidersToChannelsService(
                _specificationService.Object,
                _policiesService.Object,
                _channelsService.Object,
                _publishedProvidersLoadContext.Object,
                _releaseApprovedProvidersService.Object,
                _jobManagement.Object,
                _logger.Object,
                _prerequisiteCheckerLocator.Object,
                _releaseManagementRepository.Object
                );
        }

        [TestMethod]
        public void QueueReleaseProviderVersions_ThrowsArgumentNullException_WhenSpecificationIdSetAsNull()
        {
            // Arrange

            // Act
            Func<Task> invocation
                = () => WhenQueueReleaseProviderVersionsCalled(null, null);

            // Assert
            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null. (Parameter 'specificationId')");
        }

        [TestMethod]
        public void QueueReleaseProviderVersions_ThrowsArgumentNullException_WhenReleaseProvidersToChannelRequestSetAsNull()
        {
            // Arrange
            string specificationId = NewRandomString();

            // Act
            Func<Task> invocation
                = () => WhenQueueReleaseProviderVersionsCalled(specificationId, null);

            // Assert
            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null. (Parameter 'releaseProvidersToChannelRequest')");
        }

        [TestMethod]
        public async Task QueueReleaseProviderVersions_QueuesNewJob()
        {
            // Arrange
            string specificationId = NewRandomString();
            string jobId = NewRandomString();

            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest = new ReleaseProvidersToChannelRequest();

            Job job = new Job {Id = jobId };

            _jobManagement
                .Setup(_ => _.QueueJob(It.Is<JobCreateModel>(j =>
                    j.JobDefinitionId == JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob &&
                    j.SpecificationId == specificationId &&
                    j.Properties.ContainsKey("specification-id") &&
                    j.Properties["specification-id"] == specificationId)))
                .ReturnsAsync(job);

            // Act
            IActionResult actionResult = await WhenQueueReleaseProviderVersionsCalled(specificationId, releaseProvidersToChannelRequest);

            // Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;
            okObjectResult
                .Should()
                .NotBeNull();

            JobCreationResponse jobCreationResponse = okObjectResult.Value as JobCreationResponse;
            jobCreationResponse.JobId.Should().Be(jobId);
        }

        [TestMethod]
        public async Task ProcessMessageBeginsTransactionAndCallsCommit()
        {
            string specificationId = Guid.NewGuid().ToString();

            _specificationService.Setup(s => s.GetSpecificationSummaryById(It.IsAny<string>()))
                .ReturnsAsync(new SpecificationSummary
                {
                    Id = specificationId,
                    IsSelectedForFunding = true,
                    FundingPeriod = new Reference {  Id = "FundingPeriod", Name = "FundingPeriod "},
                    FundingStreams = new List<Reference> { new Reference { Id = "FundingStream", Name = "FundingStream" } }
                });

            _prerequisiteChecker.Setup(s => s.PerformChecks(
                It.IsAny<SpecificationSummary>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<PublishedProvider>>(),
                It.IsAny<IEnumerable<Provider>>()))
            .Returns(Task.CompletedTask);

            _prerequisiteCheckerLocator.Setup(s => s.GetPreReqChecker(It.IsAny<PrerequisiteCheckerType>()))
                .Returns(_prerequisiteChecker.Object);

            _channelsService.Setup(s => s.GetAndVerifyChannels(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new List<KeyValuePair<string, Channel>>());

            _releaseApprovedProvidersService.Setup(s => s.ReleaseProvidersInApprovedState(
                It.IsAny<Reference>(), It.IsAny<string>(), It.IsAny<SpecificationSummary>()))
                .ReturnsAsync(new List<string>());

            Message message = new Message
            {
                Body = new ReleaseProvidersToChannelRequest
                {
                    Channels = new List<string>(),
                    ProviderIds = new List<string>()
                }.AsJsonBytes()
            };
            message.UserProperties.Add("user-id", Guid.NewGuid().ToString());
            message.UserProperties.Add("user-name", Guid.NewGuid().ToString());
            message.UserProperties.Add("specification-id", Guid.NewGuid().ToString());
            message.UserProperties.Add("jobId", Guid.NewGuid().ToString());
            message.UserProperties.Add("sfa-correlationId", Guid.NewGuid().ToString());

            await _releaseProvidersToChannelsService.Process(message);

            _releaseManagementRepository.Verify(r => r.InitialiseTransaction(), Times.Once);
            _releaseManagementRepository.Verify(r => r.Commit(), Times.Once);
        }

        private async Task<IActionResult> WhenQueueReleaseProviderVersionsCalled(
            string specificationId,
            ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
            Reference author = null,
            string correlationId = null) =>
                await _releaseProvidersToChannelsService.QueueReleaseProviderVersions(specificationId, releaseProvidersToChannelRequest, author, correlationId);

        private string NewRandomString() => new RandomString();
    }
}
