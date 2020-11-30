using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class ProviderFundingPublishingServiceTests : SpecificationPublishingServiceTestsBase<ICreateAllPublishProviderFundingJobs>
    {
        private ProviderFundingPublishingService _service;
        private IPublishedFundingRepository _publishedFundingRepository;
        private ICreateBatchPublishProviderFundingJobs _createBatchPublishProviderFundingJobs;
        private ICreatePublishIntegrityJob _createPublishIntegrityJob;
        private PublishedProviderIdsRequest _publishProvidersRequest;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _createBatchPublishProviderFundingJobs = Substitute.For<ICreateBatchPublishProviderFundingJobs>();
            _createPublishIntegrityJob = Substitute.For<ICreatePublishIntegrityJob>();

            _service = new ProviderFundingPublishingService(
                SpecificationIdValidator,
                ProviderIdsValidator,
                Specifications,
                ResiliencePolicies,
                Jobs,
                _createBatchPublishProviderFundingJobs,
                _publishedFundingRepository,
                FundingConfigurationService,
                _createPublishIntegrityJob);

            _publishProvidersRequest = BuildPublishProvidersRequest(_ => _.WithProviders(ProviderIds));
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
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidationForPublishAllProvidersFunding()
        {
            string[] expectedErrors =
            {
                NewRandomString(), NewRandomString()
            };

            GivenTheValidationErrors(expectedErrors);

            await WhenAllProvidersFundingIsPublished();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidationForPublishBatchProvidersFunding()
        {
            string[] expectedErrors =
            {
                NewRandomString(), NewRandomString()
            };

            GivenTheValidationErrors(expectedErrors);

            await WhenBatchProvidersFundingIsPublished();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoSpecificationLocatedWithTheSuppliedIdForPublishAllProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            await WhenAllProvidersFundingIsPublished();

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoSpecificationLocatedWithTheSuppliedIdForPublishBatchProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(null, HttpStatusCode.NotFound);

            await WhenBatchProvidersFundingIsPublished();

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsStatusCode412IfTheSpecificationIsNotSelectedForPublishingForPublishAllProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(false)));

            await WhenAllProvidersFundingIsPublished();

            ThenTheResponseShouldBe<PreconditionFailedResult>(_ =>
                _.Value.Equals($"Specification with id : {SpecificationId} has not been selected for funding"));
        }

        [TestMethod]
        public async Task ReturnsStatusCode412IfTheSpecificationIsNotSelectedForPublishingForPublishBatchProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(false)));

            await WhenBatchProvidersFundingIsPublished();

            ThenTheResponseShouldBe<PreconditionFailedResult>(_ =>
                _.Value.Equals($"Specification with id : {SpecificationId} has not been selected for funding"));
        }

        [TestMethod]
        public async Task ReturnsStatusCode412IfTheFundingConfigurationDoesNotAllowAllApprovaModeForPublishAllProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true)
                    .WithId(SpecificationId)));
            AndTheFundingConfigurationsForSpecificationSummary(NewApiFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.Batches)));

            await WhenAllProvidersFundingIsPublished();

            ThenTheResponseShouldBe<PreconditionFailedResult>(_ =>
                _.Value.Equals($"Specification with id : {SpecificationId} has funding configurations which does not match required approval mode={ApprovalMode.All}"));
        }

        [TestMethod]
        public async Task ReturnsStatusCode412IfTheFundingConfigurationDoesNotAllowBatchesApprovaModeForPublishBatchProvidersFunding()
        {
            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true)
                    .WithId(SpecificationId)));
            AndTheFundingConfigurationsForSpecificationSummary(NewApiFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.All)));

            await WhenBatchProvidersFundingIsPublished();

            ThenTheResponseShouldBe<PreconditionFailedResult>(_ =>
                _.Value.Equals($"Specification with id : {SpecificationId} has funding configurations which does not match required approval mode={ApprovalMode.Batches}"));
        }

        [TestMethod]
        public async Task CreatesPublishAllFundingJobForSpecificationWithSuppliedId()
        {
            string publishFundingJobId = NewRandomString();
            ApiJob publishFundingJob = NewJob(_ => _.WithId(publishFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true)));
            AndTheApiResponseDetailsForSpecificationsJob(publishFundingJob);

            await WhenAllProvidersFundingIsPublished();

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
        public async Task CreatesPublishBatchFundingJobForSpecificationWithSuppliedId()
        {
            string publishFundingJobId = NewRandomString();
            ApiJob publishFundingJob = NewJob(_ => _.WithId(publishFundingJobId));

            GivenTheApiResponseDetailsForTheSuppliedId(NewApiSpecificationSummary(_ =>
                _.WithIsSelectedForFunding(true)));

            Dictionary<string, string> messageProperties = new Dictionary<string, string>
            {
                {
                    JobConstants.MessagePropertyNames.PublishedProviderIdsRequest, _publishProvidersRequest.AsJson()
                }
            };

            AndTheApiResponseDetailsForSpecificationsBatchProvidersJob(publishFundingJob, messageProperties);

            await WhenBatchProvidersFundingIsPublished();

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
        public async Task GetsCurrentPublishedProviderVersionForSuppliedVersionMetadata()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion();

            AndTheApiResponseDetailsForCurrentPublishedVersionMetaSupplied(publishedProviderVersion);

            await WhenGetCurrentPublishedProviderVersionIsCalled(publishedProviderVersion);

            ThenTheResponseShouldBe<OkObjectResult>(_ => ReferenceEquals(_.Value, publishedProviderVersion));
        }

        [TestMethod]
        public async Task GetPublishedProviderErrorSummariesForSpecificationId()
        {
            string specificationId = NewRandomString();

            IEnumerable<string> errorSummaries = new List<string>
            {
                "summary 1",
                "summary 2"
            };

            AndTheApiResponseDetailsForPublishedProviderErrorSummaries(specificationId, errorSummaries);

            await WhenGetPublishedProviderErrorSummariesIsCalled(specificationId);

            ActionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = ActionResult as OkObjectResult;

            objectResult
                .Value
                .Should()
                .NotBeNull()
                .And
                .BeOfType<List<string>>();

            List<string> result = objectResult.Value as List<string>;

            result
                .Count()
                .Should()
                .Be(2);

            result
                .FirstOrDefault()
                .Should()
                .Be("summary 1");

            result
                .LastOrDefault()
                .Should()
                .Be("summary 2");
        }

        [TestMethod]
        public async Task GetsPublishedProviderTransactionForSuppliedSpecificationAndProvider()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithAuthor(new Reference
                    {
                        Id = Guid.NewGuid().ToString()
                    })
                    .WithFundingLines(NewFundingLine()));

            AndThePublishedFundingRepositoryReturnsPublishedProviderVersions(publishedProviderVersion);

            await WhenGetPublishedProviderTransactionsIsCalled(publishedProviderVersion);

            PublishedProviderTransaction[] expectedPublishedProviderTransaction =
            {
                NewPublishedProviderTransaction(_ => _.WithAuthor(publishedProviderVersion.Author)
                    .WithPublishedProviderId(publishedProviderVersion.PublishedProviderId)
                    .WithDate(publishedProviderVersion.Date)
                    .WithPublishedProviderStatus(publishedProviderVersion.Status)
                    .WithTotalFunding(publishedProviderVersion.TotalFunding)
                    .WithFundingLines(publishedProviderVersion.FundingLines.ToArray()))
            };

            ActionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(expectedPublishedProviderTransaction);
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

        private void AndTheApiResponseDetailsForCurrentPublishedVersionMetaSupplied(
            PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFundingRepository.GetLatestPublishedProviderVersionBySpecificationId(publishedProviderVersion?.SpecificationId,
                    publishedProviderVersion?.FundingStreamId,
                    publishedProviderVersion?.ProviderId)
                .Returns(publishedProviderVersion);
        }

        private void AndTheApiResponseDetailsForPublishedProviderErrorSummaries(
            string specificationId,
            IEnumerable<string> errorSummaries)
        {
            _publishedFundingRepository.GetPublishedProviderErrorSummaries(specificationId)
                .Returns(errorSummaries);
        }

        private void AndThePublishedFundingRepositoryReturnsPublishedProviderVersions(PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFundingRepository.GetPublishedProviderVersions(publishedProviderVersion?.SpecificationId,
                    publishedProviderVersion?.ProviderId)
                .Returns(new[]
                {
                    publishedProviderVersion
                });
        }

        private async Task WhenGetPublishedProviderVersionIsCalled(PublishedProviderVersion publishedProviderVersion)
        {
            ActionResult = await _service.GetPublishedProviderVersion(publishedProviderVersion.FundingStreamId,
                publishedProviderVersion.FundingPeriodId,
                publishedProviderVersion.ProviderId,
                publishedProviderVersion.Version.ToString());
        }

        private async Task WhenGetCurrentPublishedProviderVersionIsCalled(PublishedProviderVersion publishedProviderVersion)
        {
            ActionResult = await _service.GetCurrentPublishedProviderVersion(publishedProviderVersion.FundingStreamId,
                publishedProviderVersion.ProviderId,
                publishedProviderVersion.SpecificationId);
        }

        private async Task WhenGetPublishedProviderErrorSummariesIsCalled(string specificationId)
        {
            ActionResult = await _service.GetPublishedProviderErrorSummaries(specificationId);
        }

        private async Task WhenGetPublishedProviderTransactionsIsCalled(PublishedProviderVersion publishedProviderVersion)
        {
            ActionResult = await _service.GetPublishedProviderTransactions(publishedProviderVersion.SpecificationId,
                publishedProviderVersion.ProviderId);
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
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

        private async Task WhenAllProvidersFundingIsPublished()
        {
            ActionResult = await _service.PublishAllProvidersFunding(SpecificationId, User, CorrelationId);
        }

        private async Task WhenBatchProvidersFundingIsPublished()
        {
            ActionResult = await _service.PublishBatchProvidersFunding(SpecificationId, _publishProvidersRequest, User, CorrelationId);
        }

        private void GivenTheRepositoryServiceHealth(params DependencyHealth[] dependencies)
        {
            ServiceHealth serviceHealth = new ServiceHealth();

            serviceHealth.Dependencies.AddRange(dependencies);

            _publishedFundingRepository.IsHealthOk().Returns(serviceHealth);
        }

        private PublishedProviderIdsRequest BuildPublishProvidersRequest(Action<PublishedProviderIdsRequestBuilder> setUp = null)
        {
            PublishedProviderIdsRequestBuilder publishProvidersRequestBuilder = new PublishedProviderIdsRequestBuilder();

            setUp?.Invoke(publishProvidersRequestBuilder);

            return publishProvidersRequestBuilder.Build();
        }

        protected void AndTheApiResponseDetailsForSpecificationsBatchProvidersJob(ApiJob job,
            Dictionary<string, string> messageProperties)
        {
            _createBatchPublishProviderFundingJobs.CreateJob(SpecificationId, User, CorrelationId, Arg.Is<Dictionary<string, string>>(_ => _.SequenceEqual(messageProperties)))
                .Returns(job);
        }
    }
}