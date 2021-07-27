using CalculateFunding.Api.Datasets.IntegrationTests.Data;
using CalculateFunding.Api.Datasets.IntegrationTests.Datasets;
using CalculateFunding.Common.ApiClient.Datasets.Models;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Models;
using CalculateFunding.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading.Tasks;
using DatasetsSchema = CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class ConverterDataMergeServiceTests : IntegrationTestWithJobMonitoring
    {
        private static readonly Assembly ResourceAssembly = typeof(ConverterDataMergeServiceTests).Assembly;


        private DatasetDataContext _datasetDataContext;
        private DatasetDefinitionDataContext _datasetDefinitionDataContext;
        private SpecificationDatasetRelationshipContext _specificationDatasetRelationshipContext;
        private SpecificationDataContext _specificationDataContext;
        private FundingConfigurationDataContext _fundingConfigurationDataContext;


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
            _datasetDataContext = new DatasetDataContext(Configuration, ResourceAssembly);
            _datasetDefinitionDataContext = new DatasetDefinitionDataContext(Configuration, ResourceAssembly);
            _specificationDatasetRelationshipContext = new SpecificationDatasetRelationshipContext(Configuration, ResourceAssembly);
            _specificationDataContext = new SpecificationDataContext(Configuration, ResourceAssembly);
            _fundingConfigurationDataContext = new FundingConfigurationDataContext(Configuration, ResourceAssembly);

            TrackForTeardown(_datasetDataContext,
                _datasetDefinitionDataContext,
                _specificationDatasetRelationshipContext,
                _specificationDataContext,
                _fundingConfigurationDataContext);

            _datasets = GetService<IDatasetsApiClient>();
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfTheConverterMergeRequestDoesNotHaveTheProviderVersionIdSet()
        {
            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest());

            job?.JobId
                .Should()
                .BeNullOrWhiteSpace(
                    "Expected a job to have not been created for the queue ConverterDataMergeJob request as ProviderVersionId is null");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfTheConverterMergeRequestDoesNotHaveTheDatasetIdSet()
        {
            string providerVersionId = NewRandomString();

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)));

            job?.JobId
                .Should()
                .BeNullOrWhiteSpace(
                    "Expected a job to have not been created for the queue ConverterDataMergeJob request as DatasetId is null");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfTheConverterMergeRequestDoesNotHaveTheVersionSet()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)));

            job?.JobId
                .Should()
                .BeNullOrWhiteSpace(
                    "Expected a job to have not been created for the queue ConverterDataMergeJob request as Version is null");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfTheConverterMergeRequestDoesNotHaveTheDatasetRelationshipIdSet()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)));

            job?.JobId
                .Should()
                .BeNullOrWhiteSpace(
                    "Expected a job to have not been created for the queue ConverterDataMergeJob request as DatasetRelationshipId is null");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfTheDatasetDocumentDoesNotExists()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                "Dataset not found.",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfTheDatasetDefinitionDoesNotExists()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();

            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                $"Did not locate dataset definition {definitionId}",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfTheDatasetConverterWizardNotEnabled()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();

            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(false)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEnabled(false)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)
                                .WithIdentifierFieldType(DatasetsSchema.IdentifierFieldType.UKPRN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));


            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                "Dataset is not enabled for converters. Enable it in the dataset definition.",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfJobRequestedWithExistingTheDatasetProviderVersionId()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath)
                .WithProviderVersionId(providerVersionId));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)
                                .WithIdentifierFieldType(DatasetsSchema.IdentifierFieldType.UKPRN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                $"Converter wizard does not run a second time against a dataset with same ProviderVersionId={providerVersionId} as the existing one",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfDatasetDefinitionDoesNotHaveIdentifierField()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                "No identifier field was specified on this dataset definition.",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfDatasetDefinitionDoesNotHaveUKPRNIdentifierField()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)
                                .WithIdentifierFieldType(DatasetsSchema.IdentifierFieldType.UPIN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                "Converter data merge only supports schemas with UKPRN set as the identifier.",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfDatasetRelationshipDoesNotExists()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)
                                .WithIdentifierFieldType(DatasetsSchema.IdentifierFieldType.UKPRN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                $"Dataset relationship not found. Id = '{datasetRelationshipId}'",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfSpecificationSummaryDoesNotExists()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)
                                .WithIdentifierFieldType(DatasetsSchema.IdentifierFieldType.UKPRN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));

            await AndTheDefinitionSpecificationRelationships(
                NewDefinitionSpecificationRelationshipTemplateParameters(_ =>
                        _.WithId(datasetRelationshipId)
                        .WithDefinitionId(definitionId)
                        .WithDatasetId(datasetId)
                        .WithConverterEnabled(true)
                        .WithSpecificationId(specificationId)));


            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                $"Did not locate specification summary for id {specificationId}",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfFundingConfigurationDoesNotExists()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)
                                .WithIdentifierFieldType(DatasetsSchema.IdentifierFieldType.UKPRN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));

            await AndTheDefinitionSpecificationRelationships(
                NewDefinitionSpecificationRelationshipTemplateParameters(_ =>
                        _.WithId(datasetRelationshipId)
                        .WithDefinitionId(definitionId)
                        .WithDatasetId(datasetId)
                        .WithConverterEnabled(true)
                        .WithSpecificationId(specificationId)));

            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithId(specificationId)
                    .WithProviderVersionId(providerVersionId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId));
            await AndTheSpecification(specification);


            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                $"Did not locate funding configuration for {fundingStreamId} {fundingPeriodId}",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        [TestMethod]
        [Ignore]
        public async Task JobFailsIfConvertersAreNotEnabledForFundingConfiguration()
        {
            string providerVersionId = NewRandomString();
            string datasetId = NewRandomString();
            string version = NewRandomString();
            string datasetRelationshipId = NewRandomString();
            string datasetPath = $"{NewRandomString()}.xlsx";
            string definitionId = NewRandomString();
            int definitionVersion = NewRandomInteger();
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(datasetId)
                .WithDefinitionId(definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(datasetPath)
                .WithBlobName(datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(definitionId)
                .WithVersion(definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(fundingStreamId)
                .WithTableDefinitions(NewTableDefinition(tab =>
                    tab.WithFields(NewFieldDefinition(fld =>
                            fld.WithName("ukprn")
                                .WithFieldType(DatasetsSchema.FieldType.String)
                                .WithIdentifierFieldType(DatasetsSchema.IdentifierFieldType.UKPRN)),
                        NewFieldDefinition(fld =>
                            fld.WithName("name")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("providerSubType")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("status")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("predecessors")
                                .WithFieldType(DatasetsSchema.FieldType.String)),
                        NewFieldDefinition(fld =>
                            fld.WithName("successors")
                                .WithFieldType(DatasetsSchema.FieldType.String))
                    )))));

            await AndTheDefinitionSpecificationRelationships(
                NewDefinitionSpecificationRelationshipTemplateParameters(_ =>
                        _.WithId(datasetRelationshipId)
                        .WithDefinitionId(definitionId)
                        .WithDatasetId(datasetId)
                        .WithConverterEnabled(true)
                        .WithSpecificationId(specificationId)));

            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithId(specificationId)
                    .WithProviderVersionId(providerVersionId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId));
            await AndTheSpecification(specification);

            await AndTheFundingConfiguration(NewFundingConfiguration(_
                => _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithEnableConverterDataMerge(false)));

            JobCreationResponse job = await WhenTheConverterDataMergeJobIsQueued(NewConverterMergeRequest(_
                => _.WithProviderVersionId(providerVersionId)
                    .WithDatasetId(datasetId)
                    .WithVersion(version)
                    .WithDatasetRelationshipId(datasetRelationshipId)));

            job?.JobId
                .Should()
                .NotBeNullOrWhiteSpace(
                    "Expected a job to have been created for the queue ConverterDataMergeJob request");

            await ThenTheJobFails(
                job?.JobId,
                $"Converter data merge not enabled for funding stream {fundingStreamId} and funding period {fundingPeriodId}",
                "Expected ConverterDataMergeJob to complete and failed due to dataset document does not exists");
        }

        private async Task<JobCreationResponse> WhenTheConverterDataMergeJobIsQueued(ConverterMergeRequest request)
            => (await _datasets.QueueConverterMergeJob(request))?.Content;

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private async Task AndTheDatasetDocument(DatasetTemplateParameters parameters)
            => await _datasetDataContext.CreateContextData(parameters);

        private async Task AndTheDatasetDefinition(DatasetDefinitionTemplateParameters parameters)
            => await _datasetDefinitionDataContext.CreateContextData(parameters);

        private async Task AndTheDefinitionSpecificationRelationships(params dynamic[] parameters)
            => await _specificationDatasetRelationshipContext.CreateContextData(parameters);

        private async Task AndTheFundingConfiguration(FundingConfigurationTemplateParameters parameters)
            => await _fundingConfigurationDataContext.CreateContextData(parameters);

        private FundingConfigurationTemplateParameters NewFundingConfiguration(Action<FundingConfigurationTemplateParametersBuilder> setUp = null)
        {
            FundingConfigurationTemplateParametersBuilder fundingConfigurationTemplateParametersBuilder = new FundingConfigurationTemplateParametersBuilder();

            setUp?.Invoke(fundingConfigurationTemplateParametersBuilder);

            return fundingConfigurationTemplateParametersBuilder.Build();
        }

        private SpecificationTemplateParameters NewSpecification(Action<SpecificationTemplateParametersBuilder> setUp = null)
        {
            SpecificationTemplateParametersBuilder specificationTemplateParametersBuilder = new SpecificationTemplateParametersBuilder();

            setUp?.Invoke(specificationTemplateParametersBuilder);

            return specificationTemplateParametersBuilder.Build();
        }

        private ConverterMergeRequest NewConverterMergeRequest(Action<ConverterMergeRequestBuilder> setUp = null)
        {
            ConverterMergeRequestBuilder converterMergeRequestBuilder = new ConverterMergeRequestBuilder()
                .WithAuthor(NewReference());

            setUp?.Invoke(converterMergeRequestBuilder);

            return converterMergeRequestBuilder.Build();
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

        private DatasetsSchema.TableDefinition NewTableDefinition(Action<TableDefinitionBuilder> setUp = null)
        {
            TableDefinitionBuilder tableDefinitionBuilder = new TableDefinitionBuilder();

            setUp?.Invoke(tableDefinitionBuilder);

            return tableDefinitionBuilder.Build();
        }

        private DatasetsSchema.FieldDefinition NewFieldDefinition(Action<FieldDefinitionBuilder> setUp = null)
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

        private Reference NewReference() => new Reference
        {
            Id = NewRandomString(),
            Name = NewRandomString()
        };
    }
}
