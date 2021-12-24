using CalculateFunding.Api.Results.IntegrationTests.Data;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.Config.ApiClient.Results;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.Core.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Results.IntegrationTests.SqlExport
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class CalculationResultQADatabasePopulationServiceTests : IntegrationTestWithJobMonitoring
    {
        private static readonly Assembly ResourceAssembly = typeof(CalculationResultQADatabasePopulationServiceTests).Assembly;

        private SpecificationDataContext _specificationDataContext;
        private FundingTemplateDataContext _fundingTemplateDataContext;
        private CalculationDataContext _calculationDataContext;

        private IResultsApiClient _results;

        [ClassInitialize]
        public static void FixtureSetUp(TestContext testContext)
        {
            SetUpConfiguration();
            SetUpServices((sc,
                        c)
                    => sc.AddResultsInterServiceClient(c),
                AddCacheProvider,
                AddNullLogger,
                AddUserProvider);
        }

        [TestInitialize]
        public void SetUp()
        {
            _specificationDataContext = new SpecificationDataContext(Configuration, ResourceAssembly);
            _fundingTemplateDataContext = new FundingTemplateDataContext(Configuration);
            _calculationDataContext = new CalculationDataContext(Configuration, ResourceAssembly);

            TrackForTeardown(_specificationDataContext,
                _fundingTemplateDataContext,
                _calculationDataContext);

            _results = GetService<IResultsApiClient>();
        }

        [TestMethod]
        [Ignore]
        public async Task QueueGenerateCalculationResultQADatabasePopulationJob()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateId = NewRandomString();

            string calculationOneId = NewRandomString();
            string calculationOneName = "CalcOne";

            string calculationTwoId = NewRandomString();
            string calculationTwoName = "CalcTwo";


            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithId(specificationId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithTemplateIds((fundingStreamId, templateId))
                    .WithFundingStreamId(fundingStreamId));
            await AndTheSpecification(specification);


            CalculationParameters calculationOne = NewCalculation(_
                => _.WithId(calculationOneId)
                    .WithSpecificationId(specificationId)
                    .WithName(calculationOneName)
                    .WithCalculationValueType(CalculationValueType.Number.ToString())
                    .WithCalculationType(CalculationType.Template.ToString()));
            CalculationParameters calculationTwo = NewCalculation(_
                => _.WithId(calculationOneId)
                    .WithSpecificationId(specificationId)
                    .WithName(calculationTwoName)
                    .WithCalculationValueType(CalculationValueType.Number.ToString())
                    .WithCalculationType(CalculationType.Additional.ToString()));

            await AndTheCalculation(calculationOne);
            await AndTheCalculation(calculationTwo);

            await AndTheTemplateMappingContents(NewFundingTemplate(_ => _.WithFundingPeriodId(fundingPeriodId)
                                                                    .WithFundingStreamId(fundingStreamId)
                                                                    .WithTemplateVersion(templateId)));

            Job job = await WhenQueueGenerateCalculationResultQADatabasePopulationJob(specificationId);

            await ThenTheJobSucceeds(job.Id, $"Expected {JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob} to complete and succeed.");
        }

        private async Task<Job> WhenQueueGenerateCalculationResultQADatabasePopulationJob(string specificationId)
                => (await _results.RunGenerateCalculationResultQADatabasePopulationJob(
                    new PopulateCalculationResultQADatabaseRequest {SpecificationId = specificationId }))?.Content;

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private async Task AndTheTemplateMappingContents(FundingTemplateParameters fundingTemplateParameters)
            => await _fundingTemplateDataContext.CreateContextData(fundingTemplateParameters);

        private async Task AndTheCalculation(CalculationParameters calculationParameters)
            => await _calculationDataContext.CreateContextData(calculationParameters);

        private CalculationParameters NewCalculation(Action<CalculationParametersBuilder> setUp = null)
        {
            CalculationParametersBuilder CalculationParametersBuilder = new CalculationParametersBuilder();

            setUp?.Invoke(CalculationParametersBuilder);

            return CalculationParametersBuilder.Build();
        }

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setUp = null)
        {
            SpecificationTemplateParametersBuilder specificationTemplateParametersBuilder = new SpecificationTemplateParametersBuilder();

            setUp?.Invoke(specificationTemplateParametersBuilder);

            return specificationTemplateParametersBuilder.Build();
        }

        private FundingTemplateParameters NewFundingTemplate(Action<FundingTemplateParametersBuilder> setUp = null)
        {
            FundingTemplateParametersBuilder fundingTemplateParametersBuilder = new FundingTemplateParametersBuilder();

            setUp?.Invoke(fundingTemplateParametersBuilder);

            return fundingTemplateParametersBuilder.Build();
        }


    }
}
