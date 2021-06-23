using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using ApiSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class SpecificationServiceTests
    {
        private ISpecificationsApiClient _specifications;
        private SpecificationService _service;

        [TestInitialize]
        public void SetUp()
        {
            _specifications = Substitute.For<ISpecificationsApiClient>();

            _service = new SpecificationService(_specifications,
                new ResiliencePolicies
                {
                    SpecificationsRepositoryPolicy = Policy.NoOpAsync()
                });
        }

        [DynamicData(nameof(EmptyIdExamples), DynamicDataSourceType.Method)]
        [TestMethod]
        public void SummaryQueryMethodThrowsExceptionIfSupppliedSpecificatioIdMissing(
            string specificationId)
        {
            Func<Task<ApiSpecificationSummary>> invocation = () => WhenTheSpecificationSummaryIsQueried(specificationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task QueryMethodDelegatesToApiClientAndReturnsContentFromResponse()
        {
            string specificationId = new RandomString();
            ApiSpecificationSummary expectedSpecificationSummary = new ApiSpecificationSummary();

            GivenTheApiResponseContentForTheSpecificationId(expectedSpecificationSummary, specificationId);

            ApiSpecificationSummary specificationSummary = await WhenTheSpecificationSummaryIsQueried(specificationId);

            specificationSummary
                .Should()
                .BeSameAs(expectedSpecificationSummary);
        }

        [TestMethod]
        public void SelectSpecificationForFunding__GivenNonSuccessResponse_ThrowsException()
        {
            //Arrange
            string specificationId = new RandomString();

            _specifications.SelectSpecificationForFunding(Arg.Is(specificationId))
                .Returns(HttpStatusCode.NotFound);

            //Act
            Func<Task> test = async () => await _service.SelectSpecificationForFunding(specificationId);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to select specification with id '{specificationId}' for funding.");
        }

        [TestMethod]
        public async Task GetProfileVariationPointers__GivenNotFoundResponse_ReturnsNull()
        {
            string specificationId = new RandomString();
            _specifications.GetProfileVariationPointers(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.NotFound, null, null));

            IEnumerable<ProfileVariationPointer> profileVariationPointers =
                await _service.GetProfileVariationPointers(specificationId);

            profileVariationPointers
                .Should()
                .BeNull();
        }

        [TestMethod]
        public void GetProfileVariationPointers__GivenNonSuccessResponse_ThrowsException()
        {
            //Arrange
            string specificationId = new RandomString();

            _specifications.GetProfileVariationPointers(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.InternalServerError, null, null));

            //Act
            Func<Task> test = async () => await _service.GetProfileVariationPointers(specificationId);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to select get profile variation prointer with specification id '{specificationId}'");
        }

        [TestMethod]
        public async Task GetProfileVariationPointers__GivenSuccessResponse_ReturnsProfileVariationPointers()
        {
            string specificationId = new RandomString();
            IEnumerable<ProfileVariationPointer> expectedProfileVariationPointers = new List<ProfileVariationPointer>();

            _specifications.GetProfileVariationPointers(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, expectedProfileVariationPointers));

            IEnumerable<ProfileVariationPointer> profileVariationPointers =
                await _service.GetProfileVariationPointers(specificationId);

            profileVariationPointers
                .Should()
                .BeSameAs(expectedProfileVariationPointers);
        }

        private void GivenTheApiResponseContentForTheSpecificationId(ApiSpecificationSummary specificationSummary,
            string specificationId)
        {
            _specifications.GetSpecificationSummaryById(specificationId)
                .Returns(new ApiResponse<ApiSpecificationSummary>(HttpStatusCode.OK, specificationSummary));
        }

        private async Task<ApiSpecificationSummary> WhenTheSpecificationSummaryIsQueried(string specificationId)
        {
            return await _service.GetSpecificationSummaryById(specificationId);
        }

        public static IEnumerable<object[]> EmptyIdExamples()
        {
            yield return new object[] {""};
            yield return new object[] {string.Empty};
        }
    }
}