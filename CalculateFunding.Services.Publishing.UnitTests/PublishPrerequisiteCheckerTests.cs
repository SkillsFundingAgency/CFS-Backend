using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
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
        private PublishPrerequisiteChecker _publishPrerequisiteChecker;
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private SpecificationSummary _specification;
        private PublishedProvider _publishedProvider;
        private PublishedFundingPeriod _publishedFundingPeriod;

        [TestInitialize]
        public void SetUp()
        {
            ILogger logger = Substitute.For<ILogger>();
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();

            _publishPrerequisiteChecker = new PublishPrerequisiteChecker(_specificationFundingStatusService, logger);

            _publishedFundingPeriod = new PublishedFundingPeriod { Type = PublishedFundingPeriodType.AY, Period = "123" };

            _specification = new SpecificationSummary { Id = "specification" };
        }

        [TestMethod]
        public async Task PerformPrerequisiteChecks_GivenASpecificationAndPublishedProvidersAllApproved_ReturnsEmptyPreReqs()
        {
            // Arrange
            _publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(_publishedFundingPeriod.Id)
            .WithFundingStreamId("stream")
            .WithProviderId("provider"))));

            _specificationFundingStatusService.CheckChooseForFundingStatus(_specification)
                .Returns(SpecificationFundingStatus.AlreadyChosen);

            _publishedProvider.Current.Status = PublishedProviderStatus.Approved;

            // Act
            IEnumerable<string> outstandingPreReqs = await _publishPrerequisiteChecker.PerformPrerequisiteChecks(_specification, new List<PublishedProvider> { _publishedProvider });

            // Assert
            outstandingPreReqs.IsNullOrEmpty()
                .Should()
                .Be(true);
        }

        [TestMethod]
        public async Task PerformPrerequisiteChecks_GivenASpecificationAndPublishedProvidersNotAllApproved_ReturnsPreReqs()
        {
            // Arrange
            _publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(_publishedFundingPeriod.Id)
            .WithFundingStreamId("stream")
            .WithProviderId("provider"))));

            _specificationFundingStatusService.CheckChooseForFundingStatus(_specification)
                .Returns(SpecificationFundingStatus.AlreadyChosen);

            _publishedProvider.Current.Status = PublishedProviderStatus.Draft;

            // Act
            IEnumerable<string> outstandingPreReqs = await _publishPrerequisiteChecker.PerformPrerequisiteChecks(_specification, new List<PublishedProvider> { _publishedProvider });

            // Assert
            outstandingPreReqs.IsNullOrEmpty()
                .Should()
                .Be(false);

            outstandingPreReqs.First()
                .Should()
                .Be($"Provider with id:{_publishedProvider.Id} has current status:{_publishedProvider.Current.Status} so cannot be published.");
        }

        [TestMethod]
        public async Task PerformPrerequisiteChecks_GivenASpecificationNotAbleToChooseAndPublishedProviders_ReturnsPreReqs()
        {
            // Arrange
            _publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(version => version.WithFundingPeriodId(_publishedFundingPeriod.Id)
            .WithFundingStreamId("stream")
            .WithProviderId("provider"))));

            _specificationFundingStatusService.CheckChooseForFundingStatus(_specification)
                .Returns(SpecificationFundingStatus.CanChoose);

            _publishedProvider.Current.Status = PublishedProviderStatus.Approved;

            // Act
            IEnumerable<string> outstandingPreReqs = await _publishPrerequisiteChecker.PerformPrerequisiteChecks(_specification, new List<PublishedProvider> { _publishedProvider });

            // Assert
            outstandingPreReqs.IsNullOrEmpty()
                .Should()
                .Be(false);

            outstandingPreReqs.First()
                .Should()
                .Be($"Specification with id: '{_specification.Id}' is not chosen for funding");
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
