using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Specifications;
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
            RandomString specificationId = new RandomString();
            ApiSpecificationSummary expectedSpecificationSummary = new ApiSpecificationSummary();

            GivenTheApiResponseContentForTheSpecificationId(expectedSpecificationSummary, specificationId);

            ApiSpecificationSummary specificationSummary = await WhenTheSpecificationSummaryIsQueried(specificationId);

            specificationSummary
                .Should()
                .BeSameAs(expectedSpecificationSummary);
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