using CalculateFunding.IntegrationTests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Config.ApiClient.Calcs;
using System.Threading.Tasks;
using System;
using CalculateFunding.Api.Calcs.IntegrationTests.Data;
using System.Reflection;
using System.Linq;
using CalculateFunding.IntegrationTests.Common.Data;
using FluentAssertions;
using System.Collections.Generic;

namespace CalculateFunding.Api.Calcs.IntegrationTests.ProcessTemplateMappings
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class ProcessTemplateMappingsTests : IntegrationTest
    {
        private ICalculationsApiClient _calculations;
        private SpecificationDataContext _specificationDataContext;
        private FundingTemplateDataContext _fundingTemplateDataContext;
        private TemplateMappingsContext _templateMappingsContext;

        [ClassInitialize]
        public static void FixtureSetUp(TestContext testContext)
        {
            SetUpConfiguration();
            SetUpServices((sc,
                        c)
                    => sc.AddCalculationsInterServiceClient(c),
                AddNullLogger,
                AddUserProvider);
        }

        [TestInitialize]
        public void SetUp()
        {
            _fundingTemplateDataContext = new FundingTemplateDataContext(Configuration);
            _specificationDataContext = new SpecificationDataContext(Configuration);
            _templateMappingsContext = new TemplateMappingsContext(Configuration);

            TrackForTeardown(_fundingTemplateDataContext,
                _specificationDataContext,
                _templateMappingsContext);

            _calculations = GetService<ICalculationsApiClient>();
        }

        [TestMethod]
        public async Task CreatesTemplateCalculationsWhenProcessTemplateMappingsCalled()
        {
            string specificationId = NewRandomString();
            string templateVersion = NewRandomString();
            string fundingVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithId(specificationId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId));

            FundingTemplateParameters fundingTemplate = NewFundingTemplatePatameters(_ =>
                                                            _.WithId($"{fundingStreamId}-{fundingPeriodId}-RA-{fundingVersion}")
                                                             .WithFundingStreamId(fundingStreamId)
                                                             .WithFundingPeriodId(fundingPeriodId)
                                                             .WithFundingStreamName(fundingStreamId)
                                                             .WithFundingVersion(fundingVersion)
                                                             .WithTemplateVersion(templateVersion));

            TemplateMappingsParameters templateMappings = NewTemplateMappingsParameters(_ =>
                                                             _.WithId($"templatemapping-{specificationId}-{fundingStreamId}")
                                                            .WithFundingStreamId(fundingStreamId)
                                                            .WithSpecificationId(specificationId));

            await AndTheSpecification(specification);
            await AndFundingTemplate(fundingTemplate);
            await AndTemplateMappings(templateMappings);

            ApiResponse<TemplateMapping> apiResponse = await _calculations.ProcessTemplateMappings(specificationId, templateVersion, fundingStreamId);

            TemplateMapping templateMapping = apiResponse.Content;

            IEnumerable<uint> expectedTemplateIds = _templateMappingsContext.GetTemplate().TemplateMappingItems.Select(_ => _.TemplateId);

            templateMapping.TemplateMappingItems.Select(_ => _.TemplateId)
                .ToArray()
                .Should()
                .BeEquivalentTo(expectedTemplateIds,
                    opt => opt.WithoutStrictOrdering(),
                    "expected template mappings don't match");
        }

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);
        
        private async Task AndFundingTemplate(FundingTemplateParameters fundingTemplatePatameters)
            => await _fundingTemplateDataContext.CreateContextData(fundingTemplatePatameters);

        private async Task AndTemplateMappings(TemplateMappingsParameters templateMappingsParameters)
            => await _templateMappingsContext.CreateContextData(templateMappingsParameters);

        private FundingTemplateParameters NewFundingTemplatePatameters(Action<FundingTemplateParametersBuilder> setUp = null)
        {
            FundingTemplateParametersBuilder builder = new FundingTemplateParametersBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private TemplateMappingsParameters NewTemplateMappingsParameters(Action<TemplateMappingsParametersBuilder> setUp = null)
        {
            TemplateMappingsParametersBuilder builder = new TemplateMappingsParametersBuilder();
            setUp?.Invoke(builder);
            return builder.Build();
        }

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setUp = null)
        {
            SpecificationTemplateParametersBuilder specificationTemplateParametersBuilder = new SpecificationTemplateParametersBuilder();

            setUp?.Invoke(specificationTemplateParametersBuilder);

            return specificationTemplateParametersBuilder.Build();
        }
    }
}
