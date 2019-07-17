using System.Net;
using System.Threading.Tasks;
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
    public class ProviderFundingPublishingServiceTests : SpecificationPublishingServiceTestsBase<ICreatePublishFundingJobs>
    {
        private ProviderFundingPublishingService _service;

        [TestInitialize]
        public void SetUp()
        {
            Jobs = Substitute.For<ICreatePublishFundingJobs>();

            _service = new ProviderFundingPublishingService(Validator,
                Specifications,
                ResiliencePolicies,
                Jobs);
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

        private async Task WhenTheProviderFundingIsPublished()
        {
            ActionResult = await _service.PublishProviderFunding(SpecificationId, User, CorrelationId);
        }
    }
}