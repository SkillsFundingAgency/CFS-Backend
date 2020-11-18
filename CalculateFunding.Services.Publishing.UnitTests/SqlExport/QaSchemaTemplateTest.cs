using System;
using System.Net;
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

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    public abstract class QaSchemaTemplateTest
    {
        protected Mock<IPoliciesApiClient> Policies;
        protected Mock<ISpecificationsApiClient> Specifications;
        protected Mock<ITemplateMetadataResolver> TemplateMetadataResolver;
        protected Mock<ITemplateMetadataGenerator> TemplateMetadataGenerator;

        [TestInitialize]
        public void QaSchemaTemplateTestSetUp()
        {
            Policies = new Mock<IPoliciesApiClient>();
            Specifications = new Mock<ISpecificationsApiClient>();
            TemplateMetadataResolver = new Mock<ITemplateMetadataResolver>();
            TemplateMetadataGenerator = new Mock<ITemplateMetadataGenerator>();   
        }

        protected void GivenTheSpecification(string specificationId,
            SpecificationSummary specification)
            => Specifications.Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specification));

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

        protected FundingTemplateContents NewFundingTemplateContents(Action<FundingTemplateContentsBuilder> setUp = null)
        {
            FundingTemplateContentsBuilder fundingTemplateContentsBuilder = new FundingTemplateContentsBuilder();

            setUp?.Invoke(fundingTemplateContentsBuilder);

            return fundingTemplateContentsBuilder.Build();
        }

        protected static string NewRandomString() => new RandomString();
    }
}