using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using CalcsApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using TemplateMetadataCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    public abstract class QaSchemaTemplateTest
    {
        protected Mock<IPoliciesApiClient> Policies;
        protected Mock<IJobsApiClient> Jobs;
        protected Mock<ISpecificationsApiClient> Specifications;
        protected Mock<ICalculationsApiClient> Calculations;
        protected Mock<ITemplateMetadataResolver> TemplateMetadataResolver;
        protected Mock<ITemplateMetadataGenerator> TemplateMetadataGenerator;

        [TestInitialize]
        public void QaSchemaTemplateTestSetUp()
        {
            Policies = new Mock<IPoliciesApiClient>();
            Specifications = new Mock<ISpecificationsApiClient>();
            Calculations = new Mock<ICalculationsApiClient>();
            Jobs = new Mock<IJobsApiClient>();
            TemplateMetadataResolver = new Mock<ITemplateMetadataResolver>();
            TemplateMetadataGenerator = new Mock<ITemplateMetadataGenerator>();
        }

        protected void GivenTheSpecification(string specificationId,
            SpecificationSummary specification)
            => Specifications.Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specification));

        protected void AndTheCalculationsForSpecification(
            string specificationId,
            IEnumerable<CalcsApiCalculation> calculations)
            => Calculations.Setup(_ => _.GetCalculationsForSpecification(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<CalcsApiCalculation>>(HttpStatusCode.OK, calculations));

        protected void AndTheGetLatestSuccessfulJobForSpecification(
            string specificationId,
            string jobDefinitionId,
            JobSummary jobSummary)
            => Jobs.Setup(_ => _.GetLatestSuccessfulJobForSpecification(specificationId, jobDefinitionId))
                .ReturnsAsync(new ApiResponse<JobSummary>(HttpStatusCode.OK, jobSummary));

        protected void AndTheFundingTemplate(string fundingStreamId,
            string fundingPeriodId,
            string templateVersion,
            FundingTemplateContents fundingTemplate)
            => Policies.Setup(_ => _.GetFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(new ApiResponse<FundingTemplateContents>(HttpStatusCode.OK, fundingTemplate));

        protected void AndTheTemplateMetadataContents(string schemaVersion,
            string templateContents,
            TemplateMetadataContents templateMetadataContents)
        {
            TemplateMetadataResolver.Setup(_ => _.GetService(schemaVersion))
                .Returns(TemplateMetadataGenerator.Object);
            TemplateMetadataGenerator.Setup(_ => _.GetMetadata(templateContents))
                .Returns(templateMetadataContents);
        }

        protected TemplateMetadataContents NewTemplateMetadataContents(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder templateMetadataContentsBuilder = new TemplateMetadataContentsBuilder();

            setUp?.Invoke(templateMetadataContentsBuilder);

            return templateMetadataContentsBuilder.Build();
        }

        protected SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        protected FundingLine NewFundingLine(Action<TemplateFundingLineBuilder> setUp = null)
        {
            TemplateFundingLineBuilder templateFundingLineBuilder = new TemplateFundingLineBuilder();

            setUp?.Invoke(templateFundingLineBuilder);

            return templateFundingLineBuilder.Build();
        }

        protected Calculation NewCalculation(Action<TemplateCalculationBuilder> setUp = null)
        {
            TemplateCalculationBuilder templateCalculationBuilder = new TemplateCalculationBuilder()
                .WithName(NewRandomString());

            setUp?.Invoke(templateCalculationBuilder);

            return templateCalculationBuilder.Build();
        }

        protected CalcsApiCalculation NewApiCalculation(Action<ApiCalculationBuilder> setUp = null)
        {
            ApiCalculationBuilder apiCalculationBuilder = new ApiCalculationBuilder()
                .WithName(NewRandomString());

            setUp?.Invoke(apiCalculationBuilder);

            return apiCalculationBuilder.Build();
        }

        protected FundingTemplateContents NewFundingTemplateContents(Action<FundingTemplateContentsBuilder> setUp = null)
        {
            FundingTemplateContentsBuilder fundingTemplateContentsBuilder = new FundingTemplateContentsBuilder();

            setUp?.Invoke(fundingTemplateContentsBuilder);

            return fundingTemplateContentsBuilder.Build();
        }

        protected JobSummary NewJobSummary(Action<JobSummaryBuilder> setUp = null)
        {
            JobSummaryBuilder jobSummaryBuilder = new JobSummaryBuilder();

            setUp?.Invoke(jobSummaryBuilder);

            return jobSummaryBuilder.Build();
        }

        protected TemplateMetadataCalculation NewTemplateMetadataCalculation(Action<TemplateMetadataCalculationBuilder> setUp = null)
        {
            TemplateMetadataCalculationBuilder templateMetadataCalculationBuilder = new TemplateMetadataCalculationBuilder();

            setUp?.Invoke(templateMetadataCalculationBuilder);

            return templateMetadataCalculationBuilder.Build();
        }

        protected static string NewRandomString() => new RandomString();
        protected static int NewRandomNumber() => new RandomNumberBetween(0, int.MaxValue);
        protected static uint NewRandomUInt() => Convert.ToUInt32(new RandomNumberBetween(0, int.MaxValue));

    }
}
