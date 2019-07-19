using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ApiJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class ProviderFundingPublishingServiceTests : SpecificationPublishingServiceTestsBase<PublishProviderFundingJobDefinition>
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
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidation()
        {
            string[] expectedErrors = {NewRandomString(), NewRandomString()};

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

            ThenTheResponseShouldBe<CreatedResult>(_ => _.Location == $"api/jobs/{publishFundingJobId}" &&
                                                        ReferenceEquals(_.Value, publishFundingJob));
        }

        [TestMethod]
        public async Task ReturnsNotFoundResultIfNoPublishedProviderVersionLocatedWithTheSuppliedMetadata()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _.WithDefaults(version: 1));

            AndTheApiResponseDetailsForPublishedVersionMetaSupplied(null);

            await WhenGetPublishedProviderVersionIsCalled(publishedProviderVersion);

            ThenTheResponseShouldBe<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetsPublishedProviderVersionForSuppliedVersionMetadata()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _.WithDefaults(version: 1));

            AndTheApiResponseDetailsForPublishedVersionMetaSupplied(publishedProviderVersion);

            await WhenGetPublishedProviderVersionIsCalled(publishedProviderVersion);

            ThenTheResponseShouldBe<OkObjectResult>(_ => ReferenceEquals(_.Value, publishedProviderVersion));
        }

        protected void AndTheApiResponseDetailsForPublishedVersionMetaSupplied(PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFundingRepository.GetPublishedProviderVersion(publishedProviderVersion?.FundingStreamId, 
                publishedProviderVersion?.FundingPeriodId, 
                publishedProviderVersion?.ProviderId, 
                publishedProviderVersion?.Version.ToString())
                .Returns(publishedProviderVersion);
        }

        protected async Task WhenGetPublishedProviderVersionIsCalled(PublishedProviderVersion publishedProviderVersion)
        {
            ActionResult = await _service.GetPublishedProviderVersion(publishedProviderVersion.FundingStreamId,
                publishedProviderVersion.FundingPeriodId,
                publishedProviderVersion.ProviderId,
                publishedProviderVersion.Version.ToString());
        }

        protected PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private async Task WhenTheProviderFundingIsPublished()
        {
            ActionResult = await _service.PublishProviderFunding(SpecificationId, User, CorrelationId);
        }
    }
}