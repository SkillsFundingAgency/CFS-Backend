using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class ProviderFundingPublishingServiceTests : SpecificationPublishingServiceTestsBase<ICreatePublishProviderFundingJobs>
    {
        private ProviderFundingPublishingService _service;
        private IPublishedFundingRepository _publishedFundingRepository;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();

            _service = new ProviderFundingPublishingService(Validator,
                Specifications,
                ResiliencePolicies,
                Jobs,
                _publishedFundingRepository);
        }

        [TestMethod]
        public async Task HealthCheckCollectsStatusFromRepository()
        {
            DependencyHealth firstExpectedDependency = new DependencyHealth();
            DependencyHealth secondExpectedDependency = new DependencyHealth();
            DependencyHealth thirdExpectedDependency = new DependencyHealth();

            GivenTheRepositoryServiceHealth(firstExpectedDependency,
                secondExpectedDependency,
                thirdExpectedDependency);

            ServiceHealth isHealthOk = await _service.IsHealthOk();

            isHealthOk
                .Should()
                .NotBeNull();

            isHealthOk
                .Name
                .Should()
                .Be(nameof(ProviderFundingPublishingService));

            isHealthOk
                .Dependencies
                .Should()
                .BeEquivalentTo(firstExpectedDependency, secondExpectedDependency, thirdExpectedDependency);
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidation()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

            GivenTheValidationErrors(expectedErrors);

            await WhenTheProviderFundingIsPublished();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoSpecificationLocatedWithTheSuppliedId()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            await WhenTheProviderFundingIsPublished();

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsStatusCode412IfTheSpecificationIsNotSelectedForPublishing()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(false)));

            await WhenTheProviderFundingIsPublished();

            ThenTheResponseShouldBe<PreconditionFailedResult>(_ =>
                _.Value.Equals("The Specification must be selected for funding"));
        }

        [TestMethod]
        public async Task CreatesPublishFundingJobForSpecificationWithSuppliedId()
        {
            string publishFundingJobId = NewRandomString();
            ApiJob publishFundingJob = NewJob(_ => _.WithId(publishFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true)));
            AndTheApiResponseDetailsForSpecificationsJob(publishFundingJob);

            await WhenTheProviderFundingIsPublished();

            JobCreationResponse expectedJobCreationResponse = NewJobCreationResponse(_ => _.WithJobId(publishFundingJobId));

            ActionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedJobCreationResponse);
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoPublishedProviderVersionLocatedWithTheSuppliedMetadata()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion();

            AndTheApiResponseDetailsForPublishedVersionMetaSupplied(null);

            await WhenGetPublishedProviderVersionIsCalled(publishedProviderVersion);

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetsPublishedProviderVersionForSuppliedVersionMetadata()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion();

            AndTheApiResponseDetailsForPublishedVersionMetaSupplied(publishedProviderVersion);

            await WhenGetPublishedProviderVersionIsCalled(publishedProviderVersion);

            ThenTheResponseShouldBe<OkObjectResult>(_ => ReferenceEquals(_.Value, publishedProviderVersion));
        }

        [TestMethod]
        public async Task GetsPublishedProviderTransactionForSuppliedSpecificationAndProvider()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _.WithAuthor(new Reference { Id = Guid.NewGuid().ToString() }));

            AndThePublishedFundingRepositoryReturnsPublishedProviderVersions(publishedProviderVersion);

            await WhenGetPublishedProviderTransactionsIsCalled(publishedProviderVersion);

            PublishedProviderTransaction[] expectedPublishedProviderTransaction = new[] { NewPublishedProviderTransaction(_ => _.WithAuthor(publishedProviderVersion.Author)
            .WithDate(publishedProviderVersion.Date)
            .WithPublishedProviderStatus(publishedProviderVersion.Status)) };

            ActionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedPublishedProviderTransaction); ;
        }

        private void AndTheApiResponseDetailsForPublishedVersionMetaSupplied(
            PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFundingRepository.GetPublishedProviderVersion(publishedProviderVersion?.FundingStreamId,
                    publishedProviderVersion?.FundingPeriodId,
                    publishedProviderVersion?.ProviderId,
                    publishedProviderVersion?.Version.ToString())
                .Returns(publishedProviderVersion);
        }

        
        private void AndThePublishedFundingRepositoryReturnsPublishedProviderVersions(PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFundingRepository.GetPublishedProviderVersions(publishedProviderVersion?.SpecificationId,
                    publishedProviderVersion?.ProviderId)
                .Returns(new[] { publishedProviderVersion });
        }

        private async Task WhenGetPublishedProviderVersionIsCalled(PublishedProviderVersion publishedProviderVersion)
        {
            ActionResult = await _service.GetPublishedProviderVersion(publishedProviderVersion.FundingStreamId,
                publishedProviderVersion.FundingPeriodId,
                publishedProviderVersion.ProviderId,
                publishedProviderVersion.Version.ToString());
        }

        private async Task WhenGetPublishedProviderTransactionsIsCalled(PublishedProviderVersion publishedProviderVersion)
        {
            ActionResult = await _service.GetPublishedProviderTransactions(publishedProviderVersion.SpecificationId,
                publishedProviderVersion.ProviderId);
        }

        private PublishedProviderVersion NewPublishedProviderVersion(
            Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private PublishedProviderTransaction NewPublishedProviderTransaction(
            Action<PublishedProviderTransactionBuilder> setUp = null)
        {
            PublishedProviderTransactionBuilder publishedProviderTransactionBuilder = new PublishedProviderTransactionBuilder();

            setUp?.Invoke(publishedProviderTransactionBuilder);

            return publishedProviderTransactionBuilder.Build();
        }

        private async Task WhenTheProviderFundingIsPublished()
        {
            ActionResult = await _service.PublishProviderFunding(SpecificationId, User, CorrelationId);
        }

        private void GivenTheRepositoryServiceHealth(params DependencyHealth[] dependencies)
        {
            ServiceHealth serviceHealth = new ServiceHealth();

            serviceHealth.Dependencies.AddRange(dependencies);

            _publishedFundingRepository.IsHealthOk().Returns(serviceHealth);
        }
    }
}