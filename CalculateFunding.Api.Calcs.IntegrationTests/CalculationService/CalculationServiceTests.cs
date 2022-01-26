using CalculateFunding.Api.Calcs.IntegrationTests.Data;
using CalculateFunding.Api.Calcs.IntegrationTests.SqlExport;
using CalculateFunding.Common.ApiClient;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.Config.ApiClient;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Calcs.IntegrationTests.CalculationService
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class CalculationServiceTests : IntegrationTest
    {
        private ICalculationsApiClient _calculationsApiClient;
        private CalculationDataContext _calculationsDataContext;
        private SpecificationDataContext _specificationDataContext;
        private FundingTemplateDataContext _fundingTemplateDataContext;
        private TemplateMappingsContext _templateMappingsContext;

        private static readonly Assembly ResourceAssembly = typeof(CalculationServiceTests).Assembly;


        [ClassInitialize]
        public static void FixtureSetUp(TestContext testContext)
        {
            SetUpConfiguration();
            SetUpServices((sc,
                        c)
                    => sc.AddHttpClient(HttpClientKeys.Calculations,
                        c =>
                        {
                            ApiOptions opts = GetConfigurationOptions<ApiOptions>("calcsClient");

                            Common.Config.ApiClient.ApiClientConfigurationOptions.SetDefaultApiClientConfigurationOptions(c, opts);
                        })
                    .ConfigurePrimaryHttpMessageHandler(() => new ApiClientHandler())
                    .AddTransientHttpErrorPolicy(c => c.WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5) }))
                    .AddTransientHttpErrorPolicy(c => c.CircuitBreakerAsync(100, default)),
                AddNullLogger,
                AddUserProvider,
                AddCalculationsApiClient);
        }

        [TestInitialize]
        public void SetUp()
        {
            _specificationDataContext = new SpecificationDataContext(Configuration);
            _calculationsDataContext = new CalculationDataContext(Configuration, ResourceAssembly);
            _fundingTemplateDataContext = new FundingTemplateDataContext(Configuration);
            _templateMappingsContext = new TemplateMappingsContext(Configuration);

            TrackForTeardown(
                _calculationsDataContext,
                _specificationDataContext,
                _fundingTemplateDataContext,
                _templateMappingsContext);

            _calculationsApiClient = GetService<ICalculationsApiClient>();
        }

        [TestMethod]
        [Ignore]
        public async Task EditCalculationUpdatesAdditionalCalculationName()
        {
            string specificationId = "spOne";
            string fundingPeriodId = "fpOne";
            string fundingStreamId = "fsOne";
            string templateId = "1.1";
            string fundingVersion = NewRandomString();

            string calculationOneId = "calculationOneId";
            string calculationOneName = "calculationOne";
            string calculationOneNameUpdated = "calculationOneUpdated";
            string calculationOneSourceCode = "Return 0";

            string calculationTwoId = "calculationTwoId";
            string calculationTwoName = "calculationTwo";

            SpecificationTemplateParameters specification = NewSpecification(_
            => _.WithId(specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamId(fundingStreamId)
                .WithTemplateIds((fundingStreamId, templateId)));
            await AndTheSpecification(specification);


            FundingTemplateParameters fundingTemplate = NewFundingTemplatePatameters(_ =>
                                                _.WithId($"{fundingStreamId}-{fundingPeriodId}-RA-{fundingVersion}")
                                                 .WithFundingStreamId(fundingStreamId)
                                                 .WithFundingPeriodId(fundingPeriodId)
                                                 .WithFundingStreamName(fundingStreamId)
                                                 .WithFundingVersion(fundingVersion)
                                                 .WithTemplateVersion(templateId));
            await AndFundingTemplate(fundingTemplate);

            TemplateMappingsParameters templateMappings = NewTemplateMappingsParameters(_ =>
                                                 _.WithId($"templatemapping-{specificationId}-{fundingStreamId}")
                                                .WithFundingStreamId(fundingStreamId)
                                                .WithSpecificationId(specificationId));
            await AndTemplateMappings(templateMappings);

            CalculationParameters calculationOne = NewCalculation(_
            => _.WithId(calculationOneId)
                .WithSpecificationId(specificationId)
                .WithName(calculationOneName)
                .WithSourceCodeName(new VisualBasicTypeIdentifierGenerator().GenerateIdentifier(calculationOneName))
                .WithCalculationDataType(CalculationDataType.Decimal.ToString())
                .WithCalculationValueType(CalculationValueType.Number.ToString())
                .WithCalculationType(CalculationType.Additional.ToString()));

            await AndTheCalculation(calculationOne);

            CalculationParameters calculationTwo = NewCalculation(_
            => _.WithId(calculationTwoId)
                .WithSpecificationId(specificationId)
                .WithName(calculationTwoName)
                .WithSourceCodeName(new VisualBasicTypeIdentifierGenerator().GenerateIdentifier(calculationTwoName))
                .WithCalculationDataType(CalculationDataType.Decimal.ToString())
                .WithCalculationValueType(CalculationValueType.Number.ToString())
                .WithCalculationType(CalculationType.Template.ToString()));

            await AndTheCalculation(calculationTwo);

            CalculationEditModel calculationEditModel = new CalculationEditModel
            {
                CalculationId = calculationOneId,
                DataType = CalculationDataType.String,
                SpecificationId = specificationId,
                ValueType = CalculationValueType.String,
                Name = calculationOneNameUpdated,
                SourceCode = calculationOneSourceCode,
            };

            Calculation updatedCalculation = await WhenEditCalculation(specificationId, calculationOneId, calculationEditModel);

            updatedCalculation.Name.Should().Be(calculationOneNameUpdated);
        }

        private async Task<Calculation> WhenEditCalculation(
            string specificationId, 
            string calculationId, 
            CalculationEditModel calculationEditModel)
        => (await _calculationsApiClient.EditCalculationWithSkipInstruct(specificationId, calculationId, calculationEditModel))?.Content;

        private CalculationParameters NewCalculation(Action<CalculationParametersBuilder> setUp = null)
            => BuildNewModel<CalculationParameters, CalculationParametersBuilder>(setUp);

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setUp = null)
            => BuildNewModel<SpecificationTemplateParameters, SpecificationTemplateParametersBuilder>(setUp);

        private FundingTemplateParameters NewFundingTemplatePatameters(Action<FundingTemplateParametersBuilder> setUp = null)
        => BuildNewModel<FundingTemplateParameters, FundingTemplateParametersBuilder>(setUp);

        private TemplateMappingsParameters NewTemplateMappingsParameters(Action<TemplateMappingsParametersBuilder> setUp = null)
        => BuildNewModel<TemplateMappingsParameters, TemplateMappingsParametersBuilder>(setUp);

        private async Task AndTheCalculation(CalculationParameters calculationParameters)
            => await _calculationsDataContext.CreateContextData(calculationParameters);

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private async Task AndFundingTemplate(FundingTemplateParameters fundingTemplatePatameters)
            => await _fundingTemplateDataContext.CreateContextData(fundingTemplatePatameters);

        private async Task AndTemplateMappings(TemplateMappingsParameters templateMappingsParameters)
            => await _templateMappingsContext.CreateContextData(templateMappingsParameters);

        protected static void AddCalculationsApiClient(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<ICalculationsApiClient, CalculationsApiClient>();

        private T BuildNewModel<T, TB>(Action<TB> setup) where TB : TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }
    }
}
