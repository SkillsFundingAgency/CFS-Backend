using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Api.Datasets.IntegrationTests.Data;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Models;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.IntegrationTests.Common.Data;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using FieldDefinition = CalculateFunding.Models.Datasets.Schema.FieldDefinition;
using FieldType = CalculateFunding.Models.Datasets.Schema.FieldType;
using IdentifierFieldType = CalculateFunding.Models.Datasets.Schema.IdentifierFieldType;
using RowCopyOutcome = CalculateFunding.Models.Datasets.Converter.RowCopyOutcome;
using SpecificationConverterMergeRequest = CalculateFunding.Common.ApiClient.DataSets.Models.SpecificationConverterMergeRequest;
using TableDefinition = CalculateFunding.Models.Datasets.Schema.TableDefinition;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class ConverterWizardTests : IntegrationTestWithJobMonitoring
    {
        private const string ConverterWizardActivityCsvGenerationJob = JobConstants.DefinitionNames.ConverterWizardActivityCsvGenerationJob;
        private const string RunConverterDatasetMergeJob = JobConstants.DefinitionNames.RunConverterDatasetMergeJob;
        private static readonly Assembly ResourceAssembly = typeof(ConverterWizardTests).Assembly;

        private FundingConfigurationDataContext _fundingConfigurationDataContext;
        private SpecificationDataContext _specificationDataContext;
        private ProviderDatasetExcelBlobContext _providerDatasetExcelBlobContext;
        private ConverterWizardActivityExcelBlobContext _converterActivityReportExcelBlobContext;
        private ProviderVersionBlobContext _providerVersionBlobContext;
        private DatasetDataContext _datasetDataContext;
        private DatasetDefinitionDataContext _datasetDefinitionDataContext;
        private SpecificationDatasetRelationshipContext _specificationDatasetRelationshipContext;
        private ConverterWizardJobLogDataContext _converterWizardJobLogDataContext;

        private IDatasetsApiClient _datasets;

        [ClassInitialize]
        public static void FixtureSetUp(TestContext testContext)
        {
            SetUpConfiguration();
            SetUpServices((sc,
                        c)
                    => sc.AddDatasetsInterServiceClient(c),
                AddCacheProvider,
                AddNullLogger,
                AddUserProvider);
        }

        [TestInitialize]
        public void SetUp()
        {
            _fundingConfigurationDataContext = new FundingConfigurationDataContext(Configuration, ResourceAssembly);
            _specificationDataContext = new SpecificationDataContext(Configuration, ResourceAssembly);
            _providerDatasetExcelBlobContext = new ProviderDatasetExcelBlobContext(Configuration);
            _converterActivityReportExcelBlobContext = new ConverterWizardActivityExcelBlobContext(Configuration);
            _providerVersionBlobContext = new ProviderVersionBlobContext(Configuration, ResourceAssembly);
            _datasetDataContext = new DatasetDataContext(Configuration, ResourceAssembly);
            _datasetDefinitionDataContext = new DatasetDefinitionDataContext(Configuration, ResourceAssembly);
            _specificationDatasetRelationshipContext = new SpecificationDatasetRelationshipContext(Configuration, ResourceAssembly);
            _converterWizardJobLogDataContext = new ConverterWizardJobLogDataContext(Configuration, ResourceAssembly);

            TrackForTeardown(_fundingConfigurationDataContext,
                _specificationDataContext,
                _providerDatasetExcelBlobContext,
                _converterActivityReportExcelBlobContext,
                _providerVersionBlobContext,
                _datasetDataContext,
                _datasetDefinitionDataContext,
                _specificationDatasetRelationshipContext,
                _converterWizardJobLogDataContext);

            _datasets = GetService<IDatasetsApiClient>();
        }

        [TestMethod]
        public async Task DoesNotQueueAnyChildJobsIfTheFundingConfigurationDoesNotHaveTheConverterWizardEnabled()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithDataDefinitionRelationshipIds(NewRandomString(),
                        NewRandomString(),
                        NewRandomString())
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId));

            await GivenTheFundingConfiguration(NewFundingConfiguration(_
                => _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithEnableConverterDataMerge(false)));
            await AndTheSpecification(specification);

            string jobId = await WhenTheSpecificationConverterDataMergeJobIsQueued(NewSpecificationConverterMergeRequest(_
                => _.WithSpecificationId(specification.Id)));

            jobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue SpecificationConverterDataMergeJob request");

            await ThenTheJobSucceeds(jobId, "Expected SpecificationConverterDataMergeJob to complete and succeed.");
            await AndTheJobHasNoChildJobs(jobId, "Expected there to be no converter wizard jobs created as it is not enabled in the funding configuration");
        }

        [TestMethod]
        public async Task RunsConverterJobsForTheCoreProviderDataDataDefinitionRelationshipInSpecification()
        {
            string providerVersionId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string indicativeStatusOne = NewRandomString();
            string indicativeStatusTwo = NewRandomString();

            await GivenTheFundingConfiguration(NewFundingConfiguration(_
                => _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithEnableConverterDataMerge(true)
                    .WithIndicativeOpenerProviderStatus(indicativeStatusOne,
                        indicativeStatusTwo)));

            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithId(specificationId)
                    .WithProviderVersionId(providerVersionId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId));

            string ukprnOne = NewRandomString();
            string ukprnTwo = NewRandomString();
            string ukprnThree = NewRandomString();
            string ukprnFour = NewRandomString();
            string ukprnFive = NewRandomString();
            string ukprnSix = NewRandomString();
            string ukprnSeven = NewRandomString();

            ProviderDatasetRowParameters[] providerVersionRows =
            {
                NewProviderDatasetRowParameters(pr =>
                    pr.WithStatus(indicativeStatusOne)
                        .WithUkprn(ukprnOne)
                        .WithPredecessors(ukprnTwo)),
                NewProviderDatasetRowParameters(pr => pr.WithUkprn(ukprnTwo)),
                NewProviderDatasetRowParameters(pr => pr.WithUkprn(ukprnThree)
                    .WithStatus(indicativeStatusTwo)
                    .WithPredecessors(ukprnFour)),
                NewProviderDatasetRowParameters(pr => pr.WithUkprn(ukprnFour)),
                NewProviderDatasetRowParameters(pr => pr.WithUkprn(ukprnFive)),
                NewProviderDatasetRowParameters(pr => pr.WithUkprn(ukprnSix)),
                NewProviderDatasetRowParameters(pr => pr.WithUkprn(ukprnSeven))
            };

            ConverterActivityReportRowParameters[] converterActivityReportRowParameters =
            {
                NewConverterActivityReportRowParameters(pr =>
                    pr.WithStatus(indicativeStatusOne)
                        .WithUkprn(ukprnOne)
                        .WithSourceProviderUKPRN(ukprnTwo)),
                NewConverterActivityReportRowParameters(pr =>
                    pr.WithUkprn(ukprnThree)
                        .WithStatus(indicativeStatusTwo)
                        .WithSourceProviderUKPRN(ukprnFour))
            };

            HashSet<string> ukprnsToConvert = new HashSet<string>
            {
                ukprnOne,
                ukprnThree
            };

            ProviderDatasetRowParameters[] datasetRows = providerVersionRows.Where(_ => !ukprnsToConvert.Contains(_.Ukprn)).ToArray();

            await AndTheSpecification(specification);
            await AndTheProviderVersionDocument(NewProviderVersionTemplateParameters(_ =>
                _.WithId(providerVersionId)
                    .WithProviders(providerVersionRows)));

            string datasetPath = $"{NewRandomString()}.xlsx";
            string datasetId = NewRandomString();
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();

            string converterReportName = $"converter-wizard-activity-{specificationId}";

            await AndTheDefinitionSpecificationRelationships(NewDefinitionSpecificationRelationshipTemplateParameters(_ =>
                    _.WithSpecificationId(specificationId)
                        .WithConverterEnabled(false)),
                NewDefinitionSpecificationRelationshipTemplateParameters(_ =>
                    _.WithSpecificationId(specificationId)
                        .WithDefinitionId(definitionId)
                        .WithDatasetId(datasetId)
                        .WithConverterEnabled(true)));

            await AndTheProviderDatasetExcelDocument(NewProviderDatasetParameters(_ => _.WithPath(datasetPath)
                .WithRows(datasetRows)));

            ConverterActivityReportParameters converterActivityReportParameters = NewConverterActivityReportParameters(_ => _.WithName(converterReportName)
                .WithRows(converterActivityReportRowParameters));

            await AndTheConverterActivityReportExcelDocument(converterActivityReportParameters);

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            AndTheNextVersionOfTheExcelDocumentIsTrackedForCleanup(datasetTemplateParameters, datasetPath);

            AndTheNextVersionOfTheConverterActivityReportExcelDocumentIsTrackedForCleanup(converterActivityReportParameters.Path);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(FieldType.String)
                                .WithIdentifierFieldType(IdentifierFieldType.UKPRN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(FieldType.String))
                    )))));

            string jobId = await WhenTheSpecificationConverterDataMergeJobIsQueued(NewSpecificationConverterMergeRequest(_
                => _.WithSpecificationId(specification.Id)));

            jobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue SpecificationConverterDataMergeJob request");

            await AndTheJobAndAllItsChildJobsSucceed(jobId, "Expected SpecificationConverterDataMergeJob to complete and succeed.");

            string convertActivityReportJobId = await AndTheConvertActivityJobWasQueud(jobId);

            convertActivityReportJobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    $"Expected a {ConverterWizardActivityCsvGenerationJob} to have been created for the queue");

            await AndConverterWizardLogsShouldHaveBeenCreatedForJob(jobId,
                (ukprnOne, RowCopyOutcome.Copied),
                (ukprnThree, RowCopyOutcome.Copied));

            await AndANewDatasetVersionWasCreatedFor(datasetTemplateParameters,
                datasetPath,
                providerVersionId,
                ukprnTwo,
                ukprnFour,
                ukprnFive,
                ukprnSix,
                ukprnSeven,
                ukprnOne,
                ukprnThree);

            await AndAConverterActivityReportWasCreatedFor(converterActivityReportParameters.Path,
                ukprnOne,
                ukprnThree);
        }

        private async Task GivenTheFundingConfiguration(FundingConfigurationTemplateParameters parameters)
            => await _fundingConfigurationDataContext.CreateContextData(parameters);

        private async Task AndANewDatasetVersionWasCreatedFor(DatasetTemplateParameters parameters,
            string blobPath,
            string expectedProviderVersionId,
            params string[] expectedUkprns)
        {
            DatasetViewModel dataset = (await _datasets.GetDatasetByDatasetId(parameters.Id))?.Content;

            dataset
                .Should()
                .NotBeNull();

            dataset
                .History
                ?.Where(_ => _.Version == parameters.Version && _.ProviderVersionId == expectedProviderVersionId)
                .Should()
                .NotBeNull();

            string expectedNewVersionBlobPath = GetExpectedBlobPath(parameters, blobPath);

            ExcelPackage newDatasetVersion = await _providerDatasetExcelBlobContext.GetExcelDocument(expectedNewVersionBlobPath);

            newDatasetVersion
                .Should()
                .NotBeNull();

            ExcelWorksheet worksheet = newDatasetVersion.Workbook.Worksheets[1];

            int expectedUkprnsLength = expectedUkprns.Length;

            string[] actualUkprns = new string[expectedUkprnsLength];

            for (int ukprnRow = 0; ukprnRow < expectedUkprnsLength; ukprnRow++)
            {
                actualUkprns[ukprnRow] = worksheet.Cells[ukprnRow + 2, 1]
                    .Value.ToString();
            }

            actualUkprns
                .Should()
                .BeEquivalentTo(expectedUkprns,
                    opt => opt.WithoutStrictOrdering(),
                    "Expected new dataset version excel document to contain all expected ukprns");
        }

        private async Task AndAConverterActivityReportWasCreatedFor(string blobPath, params string[] expectedUkprns)
        {
            ExcelPackage newConverterActivityReport = await _converterActivityReportExcelBlobContext.GetExcelDocument(blobPath, true);

            newConverterActivityReport
                .Should()
                .NotBeNull();

            ExcelWorksheet worksheet = newConverterActivityReport.Workbook.Worksheets[1];

            int expectedUkprnsLength = expectedUkprns.Length;

            string[] actualUkprns = new string[expectedUkprnsLength];

            for (int ukprnRow = 0; ukprnRow < expectedUkprnsLength; ukprnRow++)
            {
                actualUkprns[ukprnRow] = worksheet.Cells[ukprnRow + 2, 1]
                    .Value.ToString().Trim('"');
            }

            actualUkprns
                .Should()
                .BeEquivalentTo(expectedUkprns,
                    opt => opt.WithoutStrictOrdering(),
                    "Expected new dataset version excel document to contain all expected ukprns");
        }

        private async Task AndConverterWizardLogsShouldHaveBeenCreatedForJob(string parentJobId,
            params (string ukprn, RowCopyOutcome outcome)[] outcomes)
        {
            string childJobId = (await GetChildJobIds(parentJobId, RunConverterDatasetMergeJob)).SingleOrDefault();

            _converterWizardJobLogDataContext.TrackDocumentIdentity(new CosmosIdentity(childJobId, null));

            ConverterDataMergeLog dataMergeLog = (await _datasets.GetConverterDataMergeLog(childJobId))?.Content;

            dataMergeLog
                .Should()
                .NotBeNull();

            dataMergeLog
                .Results
                .Select(_ => new
                {
                    ukprn = _.EligibleConverter.TargetProviderId,
                    outcome = _.Outcome
                })
                .ToArray()
                .Should()
                .BeEquivalentTo(outcomes.Select(_ =>
                        new
                        {
                            _.ukprn,
                            _.outcome
                        }).ToArray(),
                    opt => opt.WithoutStrictOrdering(),
                    "Converter wizard log results rows differ from expected");
        }

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private async Task AndTheProviderDatasetExcelDocument(ProviderDatasetParameters parameters)
            => await _providerDatasetExcelBlobContext.CreateContextData(parameters);

        private async Task AndTheConverterActivityReportExcelDocument(ConverterActivityReportParameters parameters)
            => await _converterActivityReportExcelBlobContext.CreateContextData(parameters);

        private void AndTheNextVersionOfTheExcelDocumentIsTrackedForCleanup(DatasetTemplateParameters datasetTemplateParameters,
            string path)
            => _providerDatasetExcelBlobContext.TrackDocumentIdentity(
                new BlobIdentity(GetExpectedBlobPath(datasetTemplateParameters, path)));

        private void AndTheNextVersionOfTheConverterActivityReportExcelDocumentIsTrackedForCleanup(string path)
            => _converterActivityReportExcelBlobContext.TrackDocumentIdentity(
                new BlobIdentity(path));

        private static string GetExpectedBlobPath(DatasetTemplateParameters datasetTemplateParameters,
            string path) =>
            $"{datasetTemplateParameters.Id}/v{datasetTemplateParameters.Version + 1}/{Path.GetFileName(path)}";

        private async Task AndTheProviderVersionDocument(ProviderVersionTemplateParameters parameters)
            => await _providerVersionBlobContext.CreateContextData(parameters);

        private async Task AndTheDatasetDocument(DatasetTemplateParameters parameters)
            => await _datasetDataContext.CreateContextData(parameters);

        private async Task AndTheDatasetDefinition(DatasetDefinitionTemplateParameters parameters)
            => await _datasetDefinitionDataContext.CreateContextData(parameters);

        private async Task AndTheDefinitionSpecificationRelationships(params dynamic[] parameters)
            => await _specificationDatasetRelationshipContext.CreateContextData(parameters);

        private async Task<string> WhenTheSpecificationConverterDataMergeJobIsQueued(SpecificationConverterMergeRequest request)
            => (await _datasets.QueueSpecificationConverterMergeJob(request))?.Content?.JobId;

        private async Task<string> AndTheConvertActivityJobWasQueud(string JobId)
            => (await GetChildJobIds(JobId, ConverterWizardActivityCsvGenerationJob)).Single();

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setUp = null)
        {
            SpecificationTemplateParametersBuilder specificationTemplateParametersBuilder = new SpecificationTemplateParametersBuilder();

            setUp?.Invoke(specificationTemplateParametersBuilder);

            return specificationTemplateParametersBuilder.Build();
        }

        private FundingConfigurationTemplateParameters NewFundingConfiguration(Action<FundingConfigurationTemplateParametersBuilder> setUp = null)
        {
            FundingConfigurationTemplateParametersBuilder fundingConfigurationTemplateParametersBuilder = new FundingConfigurationTemplateParametersBuilder();

            setUp?.Invoke(fundingConfigurationTemplateParametersBuilder);

            return fundingConfigurationTemplateParametersBuilder.Build();
        }

        private SpecificationConverterMergeRequest NewSpecificationConverterMergeRequest(Action<SpecificationConverterMergeRequestBuilder> setUp = null)
        {
            SpecificationConverterMergeRequestBuilder specificationConverterMergeRequestBuilder = new SpecificationConverterMergeRequestBuilder()
                .WithAuthor(NewReference());

            setUp?.Invoke(specificationConverterMergeRequestBuilder);

            return specificationConverterMergeRequestBuilder.Build();
        }

        private Reference NewReference() => new Reference
        {
            Id = NewRandomString(),
            Name = NewRandomString()
        };

        private ProviderDatasetRowParameters NewProviderDatasetRowParameters(Action<ProviderDatasetRowParametersBuilder> setUp = null)
        {
            ProviderDatasetRowParametersBuilder providerDatasetRowParametersBuilder = new ProviderDatasetRowParametersBuilder();

            setUp?.Invoke(providerDatasetRowParametersBuilder);

            return providerDatasetRowParametersBuilder.Build();
        }

        private ProviderDatasetParameters NewProviderDatasetParameters(Action<ProviderDatasetParametersBuilder> setUp = null)
        {
            ProviderDatasetParametersBuilder providerDatasetParameterBuilder = new ProviderDatasetParametersBuilder();

            setUp?.Invoke(providerDatasetParameterBuilder);

            return providerDatasetParameterBuilder.Build();
        }

        private ConverterActivityReportRowParameters NewConverterActivityReportRowParameters(Action<ConverterActivityReportRowParametersBuilder> setUp = null)
        {
            ConverterActivityReportRowParametersBuilder converterActivityReportRowParametersBuilder = new ConverterActivityReportRowParametersBuilder();

            setUp?.Invoke(converterActivityReportRowParametersBuilder);

            return converterActivityReportRowParametersBuilder.Build();
        }

        private ConverterActivityReportParameters NewConverterActivityReportParameters(Action<ConverterActivityReportParametersBuilder> setUp = null)
        {
            ConverterActivityReportParametersBuilder converterActivityReportParametersBuilder = new ConverterActivityReportParametersBuilder();

            setUp?.Invoke(converterActivityReportParametersBuilder);

            return converterActivityReportParametersBuilder.Build();
        }

        private ProviderVersionTemplateParameters NewProviderVersionTemplateParameters(Action<ProviderVersionTemplateParametersBuilder> setUp = null)
        {
            ProviderVersionTemplateParametersBuilder providerVersionTemplateParametersBuilder = new ProviderVersionTemplateParametersBuilder();

            setUp?.Invoke(providerVersionTemplateParametersBuilder);

            return providerVersionTemplateParametersBuilder.Build();
        }

        private DatasetTemplateParameters NewDatasetTemplateParameters(Action<DatasetTemplateParametersBuilder> setUp = null)
        {
            DatasetTemplateParametersBuilder datasetTemplateParametersBuilder = new DatasetTemplateParametersBuilder();

            setUp?.Invoke(datasetTemplateParametersBuilder);

            return datasetTemplateParametersBuilder.Build();
        }

        private DatasetDefinitionTemplateParameters NewDatasetDefinitionTemplateParameters(Action<DatasetDefinitionTemplateParametersBuilder> setUp = null)
        {
            DatasetDefinitionTemplateParametersBuilder datasetDefinitionTemplateParametersBuilder = new DatasetDefinitionTemplateParametersBuilder();

            setUp?.Invoke(datasetDefinitionTemplateParametersBuilder);

            return datasetDefinitionTemplateParametersBuilder.Build();
        }

        private TableDefinition NewTableDefinition(Action<TableDefinitionBuilder> setUp = null)
        {
            TableDefinitionBuilder tableDefinitionBuilder = new TableDefinitionBuilder();

            setUp?.Invoke(tableDefinitionBuilder);

            return tableDefinitionBuilder.Build();
        }

        private FieldDefinition NewFieldDefinition(Action<FieldDefinitionBuilder> setUp = null)
        {
            FieldDefinitionBuilder fieldDefinitionBuilder = new FieldDefinitionBuilder();

            setUp?.Invoke(fieldDefinitionBuilder);

            return fieldDefinitionBuilder.Build();
        }

        private DefinitionSpecificationRelationshipTemplateParameters NewDefinitionSpecificationRelationshipTemplateParameters(
            Action<DefinitionSpecificationRelationshipTemplateParametersBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipTemplateParametersBuilder definitionSpecificationRelationshipTemplateParametersBuilder =
                new DefinitionSpecificationRelationshipTemplateParametersBuilder();

            setUp?.Invoke(definitionSpecificationRelationshipTemplateParametersBuilder);

            return definitionSpecificationRelationshipTemplateParametersBuilder.Build();
        }
    }
}