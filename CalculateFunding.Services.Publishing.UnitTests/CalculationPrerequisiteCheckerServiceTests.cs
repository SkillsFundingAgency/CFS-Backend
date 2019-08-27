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

        private static TEnum NewRandomEnum<TEnum>() where TEnum : struct
        {
            return new RandomEnum<TEnum>();
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

        [TestMethod]
        public async Task CollectDetailsOfNotMappedTemplateCalculationsAsValidationErrors()
        {
            CalculationMetadata calculation2 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Approved));
            CalculationMetadata calculation4 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Approved));
            CalculationMetadata calculation5 = NewApiCalculation(_ => _.WithPublishStatus(PublishStatus.Approved));

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string templateMappingItemName1 = NewRandomString();

            TemplateMappingEntityType templateMappingEntityType1 = NewRandomEnum<TemplateMappingEntityType>();

            GivenTheCalculationsForTheSpecificationId(specificationId,
                calculation2,
                calculation4,
                calculation5);

            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(
                NewTemplateMappingItem(mi => mi.WithCalculationId(string.Empty).WithEntityType(templateMappingEntityType1).WithName(templateMappingItemName1)),
                NewTemplateMappingItem(mi => mi.WithCalculationId(NewRandomString()))));

            GivenTheTemplateMappingForTheSpecificationIdAndFundingStreamId(specificationId, fundingStreamId, templateMapping);

            FundingStream[] fundingStreams = new FundingStream[]
            {
                new FundingStream{Id = fundingStreamId}
            };

            await WhenThePreRequisitesAreChecked(specificationId, fundingStreams);

            _validationErrors
                .Should()
                .Contain(new[]
                {
                    $"{templateMappingEntityType1} {templateMappingItemName1} is not mapped to a calculation in CFS"
                });
        }

        private void GivenTheCalculationsForTheSpecificationId(string specificationId, params CalculationMetadata[] calculations)
        {
            _calculationsApiClient.GetCalculations(specificationId)
                .Returns(new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.OK, calculations));
        }

        private void GivenTheTemplateMappingForTheSpecificationIdAndFundingStreamId(string specificationId, string fundingStreamId, TemplateMapping templateMapping)
        {
            _calculationsApiClient.GetTemplateMapping(specificationId, fundingStreamId)
                .Returns(new ApiResponse<TemplateMapping>(HttpStatusCode.OK, templateMapping));
        }

        private async Task WhenThePreRequisitesAreChecked(string specificationId, FundingStream[] fundingStreams = null)
        {
            _validationErrors = await _prerequisites.VerifyCalculationPrerequisites(new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = fundingStreams ?? new FundingStream[0]
            });
        }

        private CalculationMetadata NewApiCalculation(Action<ApiCalculationMetadataBuilder> setUp = null)
        {
            ApiCalculationMetadataBuilder calculationMetadataBuilder = new ApiCalculationMetadataBuilder();

            setUp?.Invoke(calculationMetadataBuilder);

            return calculationMetadataBuilder.Build();
        }

        protected static TemplateMapping NewTemplateMapping(Action<TemplateMappingBuilder> setUp = null)
        {
            TemplateMappingBuilder templateMappingBuilder = new TemplateMappingBuilder();

            setUp?.Invoke(templateMappingBuilder);

            return templateMappingBuilder.Build();
        }

        protected static TemplateMappingItem NewTemplateMappingItem(Action<TemplateMappingItemBuilder> setUp = null)
        {
            TemplateMappingItemBuilder templateMappingItemBuilder = new TemplateMappingItemBuilder();

            setUp?.Invoke(templateMappingItemBuilder);

            return templateMappingItemBuilder.Build();
        }


    }
}