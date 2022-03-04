using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Api.Datasets.IntegrationTests.Data;
using CalculateFunding.Api.Datasets.IntegrationTests.Datasets;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.Config.ApiClient.Dataset;
using CalculateFunding.Common.Models;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.IntegrationTests.Common.Data;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FieldDefinition = CalculateFunding.Models.Datasets.Schema.FieldDefinition;
using FieldType = CalculateFunding.Models.Datasets.Schema.FieldType;
using IdentifierFieldType = CalculateFunding.Models.Datasets.Schema.IdentifierFieldType;
using TableDefinition = CalculateFunding.Models.Datasets.Schema.TableDefinition;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Datasets.Models;

namespace CalculateFunding.Api.Datasets.IntegrationTests.DatasetSpecificationRelationship
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class DatasetSpecificationRelationshipTests : IntegrationTestWithJobMonitoring
    {
        private const string PublishDatasetsDataJob = JobConstants.DefinitionNames.PublishDatasetsDataJob;
        private static readonly Assembly ResourceAssembly = typeof(DatasetSpecificationRelationshipTests).Assembly;

        string _providerVersionId;
        string _specificationId;
        string _fundingStreamId;
        string _fundingPeriodId;
        string _referencedFundingPeriodId;
        string _referencedspecificationId;
        string _datasetId;
        string _definitionId;
        string _datasetPath;
        int _definitionVersion;

        private SpecificationDataContext _specificationDataContext;
        private ProviderVersionBlobContext _providerVersionBlobContext;
        private DatasetDataContext _datasetDataContext;
        private DatasetDefinitionDataContext _datasetDefinitionDataContext;
        private SpecificationDatasetRelationshipContext _specificationDatasetRelationshipContext;
        private FundingTemplateDataContext _fundingTemplateDataContext;
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
            _specificationDataContext = new SpecificationDataContext(Configuration, ResourceAssembly);
            _providerVersionBlobContext = new ProviderVersionBlobContext(Configuration, ResourceAssembly);
            _datasetDataContext = new DatasetDataContext(Configuration, ResourceAssembly);
            _datasetDefinitionDataContext = new DatasetDefinitionDataContext(Configuration, ResourceAssembly);
            _specificationDatasetRelationshipContext = new SpecificationDatasetRelationshipContext(Configuration, ResourceAssembly);
            _fundingTemplateDataContext = new FundingTemplateDataContext(Configuration);
            _fundingConfigurationDataContext = new FundingConfigurationDataContext(Configuration, ResourceAssembly);

            TrackForTeardown(_specificationDataContext,
                _providerVersionBlobContext,
                _datasetDataContext,
                _datasetDefinitionDataContext,
                _specificationDatasetRelationshipContext,
                _fundingTemplateDataContext,
                _fundingConfigurationDataContext);

            _datasets = GetService<IDatasetsApiClient>();

            _providerVersionId = NewRandomString();
            _specificationId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _referencedFundingPeriodId = NewRandomString();
            _referencedspecificationId = NewRandomString();
            _datasetId = NewRandomString();
            _datasetPath = $"{NewRandomString()}.xlsx";
            _definitionId = NewRandomString();
            _definitionVersion = NewRandomInteger();

        }

        [TestMethod]
        public async Task RunsPublishDatasetsDataJobForTheSpecificationRelationship()
        {
            
            await SetupSpecifications();
            await SetupDataset();
            await SetupTemplateContents();
            await SetupFundingConfigurationContents();

            CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel = new CreateDefinitionSpecificationRelationshipModel
            {
                DatasetDefinitionId = _definitionId,
                RelationshipType = DatasetRelationshipType.ReleasedData,
                SpecificationId = _specificationId,
                TargetSpecificationId = _referencedspecificationId
            };

            IEnumerable<uint> calculationIds = new uint[] { 13, 14 };
            IEnumerable<uint> fundingLineIds = new uint[] { 239, 240 };

            DefinitionSpecificationRelationship definitionRelationship = await WhenTheRelationshipCreated(NewRelationship(_ => _.WithDatasetDefinitionId(_definitionId)
                                   .WithRelationshipType(DatasetRelationshipType.ReleasedData)
                                   .WithSpecificationId(_specificationId)
                                   .WithTargetSpecificationId(_referencedspecificationId)
                                   .WithCalculationIds(calculationIds)
                                   .WithFundingLineIds(fundingLineIds)));

            definitionRelationship.Should().NotBeNull();

            _specificationDatasetRelationshipContext.TrackDocumentIdentity(new CosmosIdentity(definitionRelationship.Id, null));
            _specificationDatasetRelationshipContext.TrackDocumentIdentity(new CosmosIdentity(definitionRelationship.RelationshipId, null));

            IDictionary<string, JobSummary> jobs = await GetLatestJob(_referencedspecificationId, PublishDatasetsDataJob);

            jobs.ContainsKey(PublishDatasetsDataJob).Should().BeTrue();

            await ThenTheJobSucceeds(jobs[PublishDatasetsDataJob].JobId, "Expected PublishDatasetsDataJob to complete and succeed.");
        }

        [TestMethod]
        public async Task RunsDatasetObsoleteItemsJobForTheSpecification()
        {
            await SetupSpecifications();
            await SetupDataset();
            await SetupTemplateContents();

            IEnumerable<uint> calculationIds = new uint[] { 13, 14 };
            IEnumerable<uint> fundingLineIds = new uint[] { 239, 240 };

            DefinitionSpecificationRelationshipTemplateParameters definitionSpecificationRelationship = NewDefinitionSpecificationRelationship(_ => _.WithDefinitionId(_definitionId)
                                   .WithRelationshipType(DatasetRelationshipType.ReleasedData)
                                   .WithSpecificationId(_specificationId)
                                   .WithTargetSpecificationId(_referencedspecificationId)
                                   .WithCalculationIds(calculationIds)
                                   .WithFundingLineIds(fundingLineIds));
            await AndTheDefinitionSpecificationRelationship(definitionSpecificationRelationship);

            JobCreationResponse job = await WhenQueueProcessDatasetObsoleteItems(_referencedspecificationId);

            await ThenTheJobSucceeds(job.JobId, "Expected ProcessDatasetObsoleteItemsJob to complete and succeed.");
        }


        private async Task SetupSpecifications()
        {
            SpecificationTemplateParameters specification = NewSpecification(_
                => _.WithId(_specificationId)
                    .WithProviderVersionId(_providerVersionId)
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithFundingStreamId(_fundingStreamId)
                    .WithTemplateIds((_fundingStreamId, "1.0")));

            SpecificationTemplateParameters referencedSpecification = NewSpecification(_
                => _.WithId(_referencedspecificationId)
                    .WithProviderVersionId(_providerVersionId)
                    .WithFundingPeriodId(_referencedFundingPeriodId)
                    .WithFundingStreamId(_fundingStreamId)
                    .WithTemplateIds((_fundingStreamId, "1.0")));

            await AndTheSpecification(specification);
            await AndTheSpecification(referencedSpecification);
        }

        private async Task SetupDataset()
        {
            DatasetTemplateParameters datasetTemplateParameters = NewDatasetTemplateParameters(_ => _.WithId(_datasetId)
                .WithDefinitionId(_definitionId)
                .WithVersion(1)
                .WithConverterWizard(true)
                .WithUploadedBlobPath(_datasetPath)
                .WithBlobName(_datasetPath));

            await AndTheDatasetDocument(datasetTemplateParameters);

            await AndTheDatasetDefinition(NewDatasetDefinitionTemplateParameters(_ => _.WithId(_definitionId)
                .WithVersion(_definitionVersion)
                .WithConverterEligible(true)
                .WithFundingStreamId(_fundingStreamId)
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
        }

        private async Task SetupTemplateContents()
        {
            await AndTheTemplateMappingContents(NewFundingTemplate(_ => _.WithFundingPeriodId(_referencedFundingPeriodId)
                                                                    .WithFundingStreamId(_fundingStreamId)
                                                                    .WithTemplateVersion("1.0")));
        }

        private async Task SetupFundingConfigurationContents()
        {
            await AndTheFundingConfigurationContents(NewFundingConfigurationTemplate(_ => _.WithFundingPeriodId(_referencedFundingPeriodId)
                                                                    .WithFundingStreamId(_fundingStreamId)));
        }

        private async Task<DefinitionSpecificationRelationship> WhenTheRelationshipCreated(CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel)
            => (await _datasets.CreateRelationship(createDefinitionSpecificationRelationshipModel))?.Content;

        private async Task<JobCreationResponse> WhenQueueProcessDatasetObsoleteItems(string referencedSpecificationId)
            => (await _datasets.QueueProcessDatasetObsoleteItemsJob(referencedSpecificationId))?.Content;

        private async Task AndTheSpecification(SpecificationTemplateParameters parameters)
            => await _specificationDataContext.CreateContextData(parameters);

        private async Task AndTheDefinitionSpecificationRelationship(DefinitionSpecificationRelationshipTemplateParameters parameters)
            => await _specificationDatasetRelationshipContext.CreateContextData(parameters);

        private static string GetExpectedBlobPath(DatasetTemplateParameters datasetTemplateParameters,
            string path) =>
            $"{datasetTemplateParameters.Id}/v{datasetTemplateParameters.Version + 1}/{Path.GetFileName(path)}";

        private async Task AndTheDatasetDocument(DatasetTemplateParameters parameters)
            => await _datasetDataContext.CreateContextData(parameters);

        private async Task AndTheDatasetDefinition(DatasetDefinitionTemplateParameters parameters)
            => await _datasetDefinitionDataContext.CreateContextData(parameters);

        private async Task AndTheTemplateMappingContents(FundingTemplateParameters fundingTemplateParameters)
            => await _fundingTemplateDataContext.CreateContextData(fundingTemplateParameters);

        private async Task AndTheFundingConfigurationContents(FundingConfigurationTemplateParameters fundingConfigurationTemplateParameters)
            => await _fundingConfigurationDataContext.CreateContextData(fundingConfigurationTemplateParameters);

        private CreateDefinitionSpecificationRelationshipModel NewRelationship(Action<CreateDefinitionSpecificationRelationshipModelBuilder> setUp = null)
        {
            CreateDefinitionSpecificationRelationshipModelBuilder createDefinitionSpecificationRelationshipModelBuilder = new CreateDefinitionSpecificationRelationshipModelBuilder();

            setUp?.Invoke(createDefinitionSpecificationRelationshipModelBuilder);

            return createDefinitionSpecificationRelationshipModelBuilder.Build();
        }

        private FundingTemplateParameters NewFundingTemplate(Action<FundingTemplateParametersBuilder> setUp = null)
        {
            FundingTemplateParametersBuilder fundingTemplateParametersBuilder = new FundingTemplateParametersBuilder();

            setUp?.Invoke(fundingTemplateParametersBuilder);

            return fundingTemplateParametersBuilder.Build();
        }

        private FundingConfigurationTemplateParameters NewFundingConfigurationTemplate(Action<FundingConfigurationTemplateParametersBuilder> setUp = null)
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

        private DefinitionSpecificationRelationshipTemplateParameters NewDefinitionSpecificationRelationship(Action<DefinitionSpecificationRelationshipTemplateParametersBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipTemplateParametersBuilder definitionSpecificationRelationshipTemplateParametersBuilder = new DefinitionSpecificationRelationshipTemplateParametersBuilder();

            setUp?.Invoke(definitionSpecificationRelationshipTemplateParametersBuilder);

            return definitionSpecificationRelationshipTemplateParametersBuilder.Build();
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