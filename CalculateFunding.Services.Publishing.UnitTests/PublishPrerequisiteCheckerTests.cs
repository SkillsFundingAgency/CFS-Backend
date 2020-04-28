using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishPrerequisiteCheckerTests
    {
        private PublishAllPrerequisiteChecker _publishPrerequisiteChecker;
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private IJobsRunning _jobsRunning;
        private IJobManagement _jobManagement;
        private SpecificationSummary _specification;
        private PublishedProvider _publishedProvider;
        private ILogger _logger;
        private PublishedFundingPeriod _publishedFundingPeriod;

        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _jobsRunning = Substitute.For<IJobsRunning>();
            _jobManagement = Substitute.For<IJobManagement>();

            _publishPrerequisiteChecker = new PublishAllPrerequisiteChecker(_specificationFundingStatusService, _jobsRunning, _jobManagement, _logger);

            _publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };

            _specification = new SpecificationSummary { Id = "specification" };
        }

        [TestMethod]
        public void PerformPrerequisiteChecks_GivenASpecificationAndPublishedProvidersNotAllApproved_ReturnsPreReqs()
        {
            // Arrange
            _publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(_publishedFundingPeriod.Id)
            .WithFundingStreamId("stream")
            .WithProviderId("provider"))));

            _specificationFundingStatusService.CheckChooseForFundingStatus(_specification)
                .Returns(SpecificationFundingStatus.AlreadyChosen);

            _publishedProvider.Current.Status = PublishedProviderStatus.Draft;

            // Act
            Func<Task> invocation
                = () => _publishPrerequisiteChecker.PerformChecks(_specification, null, new List<PublishedProvider> { _publishedProvider });

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{_specification.Id} has prerequisites which aren't complete.");

            // Assert
            _logger
                .Received(1)
                .Error($"Provider with id:{_publishedProvider.Id} has current status:{_publishedProvider.Current.Status} so cannot be published.");
        }

        [TestMethod]
        public void PerformPrerequisiteChecks_GivenASpecificationNotAbleToChooseAndPublishedProviders_ReturnsPreReqs()
        {
            // Arrange
            _publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(_publishedFundingPeriod.Id)
            .WithFundingStreamId("stream")
            .WithProviderId("provider"))));

            _specificationFundingStatusService.CheckChooseForFundingStatus(_specification)
                .Returns(SpecificationFundingStatus.CanChoose);

            _publishedProvider.Current.Status = PublishedProviderStatus.Approved;

            // Act
            Func<Task> invocation
                = () => _publishPrerequisiteChecker.PerformChecks(_specification, null, new List<PublishedProvider> { _publishedProvider });

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{_specification.Id} has prerequisites which aren't complete.");

            // Assert
            _logger
                .Received(1)
                .Error($"Specification with id: '{_specification.Id}' is not chosen for funding");
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }
    }
}
