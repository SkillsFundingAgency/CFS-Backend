using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class CalculationPrerequisiteCheckerServiceTests
    {
        private ICalculationsApiClient _calculationsApiClient;

        private CalculationPrerequisiteCheckerService _prerequisites;
        private IEnumerable<string> _validationErrors;

        [TestInitialize]
        public void SetUp()
        {
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();

            _prerequisites = new CalculationPrerequisiteCheckerService(_calculationsApiClient,
                new ResiliencePolicies
                {
                    CalculationsApiClient = Policy.NoOpAsync()
                },
                Substitute.For<ILogger>());
        }

        [TestMethod]
        public void ThrowsExceptionIfCantRetrieveCalculationsToCheck()
        {
            string specificationId = NewRandomString();

            Func<Task> invocation = () => WhenThePreRequisitesAreChecked(specificationId);

            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage(
                    $"Did locate any calculation metadata for specification {specificationId}. Unable to complete prerequisite checks");
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        [TestMethod]
        public async Task CollectsDetailsOfUnapprovedCalculationsAsValidationErrors()
        {
            CalculationMetadata calculation1 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Archived));
            CalculationMetadata calculation2 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Approved));
            CalculationMetadata calculation3 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Draft));
            CalculationMetadata calculation4 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Approved));
            CalculationMetadata calculation5 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Approved));
            CalculationMetadata calculation6 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Updated));

            string specificationId = NewRandomString();

            GivenTheCalculationsForTheSpecificationId(specificationId,
                calculation1,
                calculation2,
                calculation3,
                calculation4,
                calculation5,
                calculation6);

            await WhenThePreRequisitesAreChecked(specificationId);

            _validationErrors
                .Should()
                .Contain(new[]
                {
                    $"Calculation {calculation1.Name} must be approved but is {calculation1.PublishStatus}",
                    $"Calculation {calculation3.Name} must be approved but is {calculation3.PublishStatus}",
                    $"Calculation {calculation6.Name} must be approved but is {calculation6.PublishStatus}"
                });
        }

        private void GivenTheCalculationsForTheSpecificationId(string specificationId, params CalculationMetadata[] calculations)
        {
            _calculationsApiClient.GetCalculations(specificationId)
                .Returns(new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.OK, calculations));
        }

        private async Task WhenThePreRequisitesAreChecked(string specificationId)
        {
            _validationErrors = await _prerequisites.VerifyCalculationPrerequisites(new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new FundingStream[0]
            });
        }

        private CalculationMetadata NewApiCalculation(Action<ApiCalculationMetadataBuilder> setUp = null)
        {
            ApiCalculationMetadataBuilder calculationMetadataBuilder = new ApiCalculationMetadataBuilder();

            setUp?.Invoke(calculationMetadataBuilder);

            return calculationMetadataBuilder.Build();
        }
    }
}