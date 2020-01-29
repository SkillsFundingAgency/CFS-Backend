using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Builders;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using AggregatedType = CalculateFunding.Models.Datasets.AggregatedTypes;
using ApiProviderSummary = CalculateFunding.Common.ApiClient.Providers.Models.ProviderSummary;
using ApiProviderVersion = CalculateFunding.Common.ApiClient.Providers.Models.ProviderVersion;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using FieldDefinition = CalculateFunding.Models.Datasets.Schema.FieldDefinition;
using VersionReference = CalculateFunding.Models.VersionReference;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class ProcessDatasetsServiceProcessDatasetTests : ProcessDatasetServiceTestsBase
    {
        private ProcessDatasetService _service;
        private IDatasetRepository _datasetRepository;
        private ICalcsRepository _calculationsRepository;
        private IBlobClient _blobClient;
        private ICacheProvider _cacheProvider;
        private IExcelDatasetReader _excelDatasetReader;
        private IProviderSourceDatasetsRepository _providerResultsRepository;
        private IProviderSourceDatasetVersionKeyProvider _versionKeyProvider;
        private IProvidersApiClient _providersApiClient;
        private ISpecificationsApiClient _specificationsApiClient;
        private IMessengerService _messengerService;
        private IDatasetsAggregationsRepository _datasetsAggregationsRepository;
        private IVersionRepository<ProviderSourceDatasetVersion> _versionRepository;
        private IFeatureToggle _featureToggle;
        private IJobsApiClient _jobsApiClient;
        private IJobManagement _jobManagement;
        private ILogger _logger;

        private Message _message;

        private string _datasetCacheKey = $"ds-table-rows:{ProcessDatasetService.GetBlobNameCacheKey(BlobPath)}:{DataDefintionId}";
        private string _datasetAggregationsCacheKey = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

        private string _relationshipId;
        private string _relationshipName;
        private string _upin;
        private string _laCode;
        private string _providerId;
        private string _jobId;

        private const string Upin = nameof(Upin);
        private const string LaCode = nameof(LaCode);
        private const string BlobPath = "dataset-id/v1/ds.xlsx";
        private const string CreateInstructAllocationJob = JobConstants.DefinitionNames.CreateInstructAllocationJob;
        private const string CreateInstructGenerateAggregationsAllocationJob = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob;

        [TestInitialize]
        public void SetUp()
        {
            _datasetRepository = CreateDatasetsRepository();
            _calculationsRepository = CreateCalcsRepository();
            _blobClient = CreateBlobClient();
            _cacheProvider = CreateCacheProvider();
            _excelDatasetReader = CreateExcelDatasetReader();
            _providerResultsRepository = CreateProviderResultsRepository();
            _versionKeyProvider = CreateDatasetVersionKeyProvider();
            _providersApiClient = CreateProvidersApiClient();
            _specificationsApiClient = CreateSpecificationsApiClient();
            _messengerService = CreateMessengerService();
            _featureToggle = CreateFeatureToggle();
            _datasetsAggregationsRepository = CreateDatasetsAggregationsRepository();
            _versionRepository = CreateVersionRepository();
            _jobsApiClient = CreateJobsApiClient();
            _jobManagement = CreateJobManagement();
            _logger = CreateLogger();

            _service = CreateProcessDatasetService(datasetRepository: _datasetRepository,
                calcsRepository: _calculationsRepository,
                blobClient: _blobClient,
                cacheProvider: _cacheProvider,
                excelDatasetReader: _excelDatasetReader,
                providerResultsRepository: _providerResultsRepository,
                versionKeyProvider: _versionKeyProvider,
                providersApiClient: _providersApiClient,
                specificationsApiClient: _specificationsApiClient,
                messengerService: _messengerService,
                featureToggle: _featureToggle,
                datasetsAggregationsRepository: _datasetsAggregationsRepository,
                versionRepository: _versionRepository,
                jobsApiClient: _jobsApiClient,
                jobManagement: _jobManagement,
                logger: _logger);

            _message = new Message();
            _relationshipId = NewRandomString();
            _relationshipName = NewRandomString();
            _upin = NewRandomString();
            _providerId = NewRandomString();
            _laCode = NewRandomString();
            _jobId = NewRandomString();
        }

        [TestMethod]
        public void ProcessDataset_GivenNullMessage_ThrowsArgumentNullException()
        {
            _message = null;

            Func<Task> invocation = WhenTheProcessDatasetMessageIsProcessed;

            invocation
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenNullPayload_DoesNoProcessing()
        {
            GivenTheMessageProperties(("jobId", "job1"));

            await WhenTheProcessDatasetMessageIsProcessed();

            await _datasetRepository
                .DidNotReceive()
                .GetDefinitionSpecificationRelationshipById(Arg.Any<string>());

            ThenTheErrorWasLogged("A null dataset was provided to ProcessData");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButNoSpecificationIdKeyInProperties_DoesNoProcessing()
        {
            GivenTheMessageProperties(("jobId", "job1"));
            AndTheMessageBody(new Dataset());

            await WhenTheProcessDatasetMessageIsProcessed();

            await _datasetRepository
                .DidNotReceive()
                .GetDefinitionSpecificationRelationshipById(Arg.Any<string>());

            ThenTheErrorWasLogged("Specification Id key is missing in ProcessDataset message properties");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButNoSpecificationIdValueInProperties_DoesNoProcessing()
        {
            GivenTheMessageProperties(("specification-id", ""), ("jobId", "job1"));
            AndTheMessageBody(new Dataset());

            await WhenTheProcessDatasetMessageIsProcessed();

            await _datasetRepository
                .DidNotReceive()
                .GetDefinitionSpecificationRelationshipById(Arg.Any<string>());

            ThenTheErrorWasLogged("A null or empty specification id was provided to ProcessData");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButDatasetDefinitionCouldNotBeFound_DoesNotProcess()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference())
                .WithDatasetVersion(NewRelationshipVersion())));

            await WhenTheProcessDatasetMessageIsProcessed();

            ThenTheErrorWasLogged($"Unable to find a data definition for id: {DataDefintionId}, for blob: {BlobPath}");
            AndAnExceptionWasLogged();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButBuildProjectCouldNotBeFound_DoesNotProcess()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference())
                .WithDatasetVersion(NewRelationshipVersion())));
            AndTheDatasetDefinitions(NewDatasetDefinition());

            await WhenTheProcessDatasetMessageIsProcessed();

            ThenTheErrorWasLogged($"Unable to find a build project for specification id: {SpecificationId}");
            AndAnExceptionWasLogged();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButBlobNotFound_DoesNotProcess()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference())
                .WithDatasetVersion(NewRelationshipVersion())));
            AndTheDatasetDefinitions(NewDatasetDefinition());
            AndTheBuildProject(SpecificationId, NewBuildProject());
            AndTheCloudBlob(BlobPath, null);

            await WhenTheProcessDatasetMessageIsProcessed();

            ThenTheErrorWasLogged($"Failed to find blob with path: {BlobPath}");
            AndAnExceptionWasLogged();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndBlobFoundButEmptyFile_DoesNotProcess()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference())
                .WithDatasetVersion(NewRelationshipVersion())));
            AndTheDatasetDefinitions(NewDatasetDefinition());
            AndTheBuildProject(SpecificationId, NewBuildProject());

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);
            AndTheCloudStream(cloudBlob, NewStream());

            await WhenTheProcessDatasetMessageIsProcessed();

            ThenTheErrorWasLogged($"Invalid blob returned: {BlobPath}");
            AndAnExceptionWasLogged();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndBlobFoundButNoTableResultsReturned_DoesNotProcess()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference())
                .WithDatasetVersion(NewRelationshipVersion())));
            AndTheDatasetDefinitions(NewDatasetDefinition());
            AndTheBuildProject(SpecificationId, NewBuildProject());

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);
            AndTheCloudStream(cloudBlob, NewStream(new byte[100]));

            await WhenTheProcessDatasetMessageIsProcessed();

            ThenTheErrorWasLogged("Failed to load table result");
            AndAnExceptionWasLogged();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsButNoDatasetRelationshipSummaries_DoesNotProcess()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference())
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition();

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject());

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);
            AndTheCachedTableLoadResults(_datasetCacheKey, NewTableLoadResult());
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, NewTableLoadResult());

            await WhenTheProcessDatasetMessageIsProcessed();

            ThenTheErrorWasLogged($"No dataset relationships found for build project with id : '{BuildProjectId}' for specification '{SpecificationId}'");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsButNoDatasetRelationshipSummaryCouldBeFound_DoesNotProcess()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition();

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary())));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);
            AndTheCachedTableLoadResults(_datasetCacheKey, NewTableLoadResult());
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, NewTableLoadResult());

            await WhenTheProcessDatasetMessageIsProcessed();

            ThenTheErrorWasLogged(
                $"No dataset relationship found for build project with id : {BuildProjectId} with data definition id {DataDefintionId} and relationshipId '{_relationshipId}'");
            await AndNoResultsWereSaved();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsButNoIdentifiersFound_DoesNotSaveResults()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition();

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary())));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenNoResultsWereSaved();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsButNoProviderIds_DoesNotSaveResults()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary())));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenNoResultsWereSaved();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsButNotAggregateFields_SavesDatasetButDoesNotSaveAggregates()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin } })));

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenTheProviderSourceDatasetWasUpdated();
            await AndTheProviderDatasetVersionKeyWasInvalidated();
            await AndNoAggregationsWereCreated();
            await AndTheCachedAggregationsWereInvalidated();
        }

        [TestMethod]
        [DataRow(true, 1)]
        [DataRow(false, 0)]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsWithProviderMissing_SavesDataset(bool cleanup, int operations)
        {
            string newProviderId = NewRandomString();

            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(newProviderId)
                .WithUPIN(_upin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = newProviderId, UPIN = _upin } })));
            AndTheExistingProviderDatasets(NewProviderSourceDataset(_ => _.WithCurrent(NewProviderSourceDatasetVersion(ver =>
                ver.WithRows((Upin, _upin))
                    .WithDataset(NewVersionReference(ds => ds.WithId(DatasetId)
                        .WithName("ds-1")
                        .WithVersion(1)))))));
            AndIsUseFieldDefinitionIdsInSourceDatasetsEnabledIs(cleanup);

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenTheProviderSourceDatasetWasUpdated(newProviderId);
            await AndTheProviderSourceDatasetWasDeleted(_providerId, operations);
            await AndTheCleanUpDatasetTopicWasNotified(operations);
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndLaCodesAsIdentifiers_SavesDataset()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN)),
                    NewFieldDefinition(fld => fld.WithName(LaCode)
                        .WithIdentifierFieldType(IdentifierFieldType.LACode))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin), (LaCode, _laCode)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)
                .WithLACode(_laCode)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin } })));

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenTheProviderSourceDatasetWasUpdated(extraConstraints: ds => ds.Current.Rows[0][LaCode].ToString() == _laCode);
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndIsAggregatesFeatureToggleEnabledAnHasAggregableField_SavesDataset()
        {
            string aggregateFieldName = NewRandomString();
            decimal aggregateFieldValue = new RandomNumberBetween(1, 3000);

            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN)),
                    NewFieldDefinition(fld => fld.WithName(aggregateFieldName)
                        .WithIsAggregate(true)
                        .WithIdentifierFieldType(IdentifierFieldType.None)
                        .WithFieldType(FieldType.Decimal)
                    )))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin), (aggregateFieldName, aggregateFieldValue)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin } })));

            await WhenTheProcessDatasetMessageIsProcessed();

            string cleanRelationshipName = GenerateIdentifier(_relationshipName);
            string cleanFieldName = GenerateIdentifier(aggregateFieldName);

            await ThenTheDatasetAggregationsWereSaved(
                extraConstraints: agg =>
                    agg.Fields.Count() == 4 &&
                    agg.Fields.ElementAt(0).Value == aggregateFieldValue &&
                    agg.Fields.ElementAt(0).FieldType == AggregatedType.Sum &&
                    agg.Fields.ElementAt(0).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Sum" &&
                    agg.Fields.ElementAt(1).Value == aggregateFieldValue &&
                    agg.Fields.ElementAt(1).FieldType == AggregatedType.Average &&
                    agg.Fields.ElementAt(1).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Average" &&
                    agg.Fields.ElementAt(2).Value == aggregateFieldValue &&
                    agg.Fields.ElementAt(2).FieldType == AggregatedType.Min &&
                    agg.Fields.ElementAt(2).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Min" &&
                    agg.Fields.ElementAt(3).Value == aggregateFieldValue &&
                    agg.Fields.ElementAt(3).FieldType == AggregatedType.Max &&
                    agg.Fields.ElementAt(3).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Max");
            await AndTheCachedAggregationsWereInvalidated();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithMultipleRowsWithProviderIdsAndIsAggregatesFeatureToggleEnabledAnHasAggregableField_SavesDataset()
        {
            string aggregateFieldName = NewRandomString();

            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN)),
                    NewFieldDefinition(fld => fld.WithName(aggregateFieldName)
                        .WithIsAggregate(true)
                        .WithIdentifierFieldType(IdentifierFieldType.None)
                        .WithFieldType(FieldType.Decimal)
                    )))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin), (aggregateFieldName, 3000M))),
                NewRowLoadResult(row => row.WithFields((Upin, NewRandomString()), (aggregateFieldName, 120))),
                NewRowLoadResult(row => row.WithFields((Upin, NewRandomString()), (aggregateFieldName, 10))),
                NewRowLoadResult(row => row.WithFields((Upin, NewRandomString()), (aggregateFieldName, 567)))
                ));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin } })));

            await WhenTheProcessDatasetMessageIsProcessed();

            string cleanRelationshipName = GenerateIdentifier(_relationshipName);
            string cleanFieldName = GenerateIdentifier(aggregateFieldName);

            await ThenTheDatasetAggregationsWereSaved(
                extraConstraints: agg =>
                    agg.Fields.Count() == 4 &&
                    agg.Fields.ElementAt(0).Value == (decimal)3697 &&
                    agg.Fields.ElementAt(0).FieldType == AggregatedType.Sum &&
                    agg.Fields.ElementAt(0).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Sum" &&
                    agg.Fields.ElementAt(1).Value == (decimal)924.25 &&
                    agg.Fields.ElementAt(1).FieldType == AggregatedType.Average &&
                    agg.Fields.ElementAt(1).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Average" &&
                    agg.Fields.ElementAt(2).Value == (decimal)10 &&
                    agg.Fields.ElementAt(2).FieldType == AggregatedType.Min &&
                    agg.Fields.ElementAt(2).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Min" &&
                    agg.Fields.ElementAt(3).Value == (decimal)3000 &&
                    agg.Fields.ElementAt(3).FieldType == AggregatedType.Max &&
                    agg.Fields.ElementAt(3).FieldReference == $"Datasets.{cleanRelationshipName}.{cleanFieldName}_Max");
            await AndTheCachedAggregationsWereInvalidated();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithMultipleProviderIds_SavesDatasetForEachProvider()
        {
            string secondProviderUpin = NewRandomString();

            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
               ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin))),
                NewRowLoadResult(row => row.WithFields((Upin, secondProviderUpin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            string secondProviderId = NewRandomString();
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)),
                NewApiProviderSummary(_ => _.WithId(secondProviderId)
                    .WithUPIN(secondProviderUpin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin }, new ApiProvider { ProviderId = secondProviderId, UPIN = secondProviderUpin } })));

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenXProviderSourceDatasetsWereUpdate(2);
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsButNoExistingToCompare_SavesDatasetDoesntCallCreateVersionSavesVersion()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin } })));

            AndTheJob(NewJob(_ => _.WithId(_jobId)
                .WithDefinitionId(CreateInstructAllocationJob)), CreateInstructAllocationJob);

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenTheProviderSourceDatasetWasUpdated();
            await AndNoDatasetVersionsWereCreated();
            await AndTheDatasetVersionWasSaved(version: 1);
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndGetsExistingButNoChanges_DoesNotUpdate()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheExistingProviderDatasets(NewProviderSourceDataset(_ => _.WithCurrent(NewProviderSourceDatasetVersion(ver =>
                ver.WithRows((Upin, _upin))
                    .WithDataset(NewVersionReference(ds => ds.WithId(DatasetId)
                        .WithName("ds-1")
                        .WithVersion(1)))))));

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenNoResultsWereSaved();
            await AndNoDatasetVersionsWereCreated();
            AndNoLoggingStartingWith("Saving");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndChangesInData_CalsCreateNewVersionAndSaves()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin } })));

            ProviderSourceDatasetVersion currentVersion = NewProviderSourceDatasetVersion(ver =>
                ver.WithProviderId(_providerId)
                    .WithRows((Upin, NewRandomString()))
                    .WithDataset(NewVersionReference(ds => ds.WithId(DatasetId)
                        .WithName("ds-1")
                        .WithVersion(1))));

            AndTheExistingProviderDatasets(NewProviderSourceDataset(_ => _.WithCurrent(currentVersion)));

            ProviderSourceDatasetVersion newVersion = NewProviderSourceDatasetVersion(ver =>
                ver.WithProviderId(_providerId)
                    .WithRows((Upin, NewRandomString()))
                    .WithDataset(NewVersionReference(ds => ds.WithId(DatasetId)
                        .WithName("ds-1")
                        .WithVersion(2))));

            AndTheNewProviderDatasetVersion(currentVersion, newVersion);

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenTheNewVersionWasSaved(newVersion);
            AndTheLoggingWasSent("Saving 1 updated source datasets", "Saving 1 items to history");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIds_EnsuresCreatesNewJob()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithDefinesScope(true)
                    .WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));
            AndTheCompileResponse(HttpStatusCode.NoContent);

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheJob(NewJob(_ => _.WithId(_jobId)
                .WithDefinitionId(CreateInstructAllocationJob)), CreateInstructAllocationJob);

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenTheAllocationJobWasCreated(CreateInstructAllocationJob);
            AndTheLoggingWasSent($"New job of type '{CreateInstructAllocationJob}' created with id: '{_jobId}'");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndJobServiceFeatureIsOnAndCalcsIncludeAggregatedCals_EnsuresCreatesNewGenerateAggregationsJob()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithDefinesScope(true)
                    .WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));
            AndTheCompileResponse(HttpStatusCode.NoContent);
            AndTheCalculations(NewCalculation(_ => _.WithSourceCode("return Sum(Calc1)")));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheCoreProviderVersion(NewApiProviderVersion(_ => _.WithProviders(new ApiProvider[] { new ApiProvider { ProviderId = _providerId, UPIN = _upin } })));
            AndTheJob(NewJob(_ => _.WithId(_jobId)
                .WithDefinitionId(CreateInstructGenerateAggregationsAllocationJob)), CreateInstructGenerateAggregationsAllocationJob);

            await WhenTheProcessDatasetMessageIsProcessed();

            await ThenTheAllocationJobWasCreated(CreateInstructGenerateAggregationsAllocationJob);
            AndTheLoggingWasSent($"New job of type '{CreateInstructGenerateAggregationsAllocationJob}' created with id: '{_jobId}'");
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsButCreatingJobReturnsNull_LogsErrorAndThrowsException()
        {
            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", "job1"),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithDefinesScope(true)
                    .WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));
            AndTheCompileResponse(HttpStatusCode.NoContent);

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));

            Func<Task> invocation = WhenTheProcessDatasetMessageIsProcessed;

            invocation
                .Should().ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to create job of type '{CreateInstructAllocationJob}' on specification '{SpecificationId}'");

            AndTheErrorWasLogged($"Failed to create job of type '{CreateInstructAllocationJob}' on specification '{SpecificationId}'");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenRunningAsAJob_ThenUpdateJobStatus()
        {
            string invokedByJobId = "job1";

            GivenTheMessageProperties(("specification-id", SpecificationId), ("relationship-id", _relationshipId), ("jobId", invokedByJobId),
                ("user-id", UserId), ("user-name", Username));
            AndTheMessageBody(NewDataset(_ => _.WithCurrent(NewDatasetVersion())
                .WithDefinition(NewReference(rf => rf.WithId(DataDefintionId)))
                .WithHistory(NewDatasetVersion())));
            AndTheSpecification(SpecificationId, NewSpecification(_ =>
            _.WithId(SpecificationId)
            .WithProviderVersionId(ProviderVersionId)
            ));
            AndTheRelationship(_relationshipId, NewRelationship(_ => _.WithDatasetDefinition(NewReference(
                    rf => rf.WithId(DataDefintionId)))
                .WithDatasetVersion(NewRelationshipVersion())));

            DatasetDefinition datasetDefinition = NewDatasetDefinition(_ => _.WithTableDefinitions(NewTableDefinition(tb =>
                tb.WithFieldDefinitions(NewFieldDefinition(fld => fld.WithName(Upin)
                    .WithIdentifierFieldType(IdentifierFieldType.UPIN))))));

            AndTheDatasetDefinitions(datasetDefinition);
            AndTheBuildProject(SpecificationId, NewBuildProject(_ => _.WithRelationships(NewRelationshipSummary(summary =>
                summary.WithDefinesScope(true)
                    .WithRelationship(NewReference(rf => rf.WithId(_relationshipId)
                    .WithName(_relationshipName)))
                    .WithDatasetDefinition(NewDatasetDefinition())))));
            AndTheCompileResponse(HttpStatusCode.NoContent);
            AndTheCalculations(NewCalculation(_ => _.WithSourceCode("return Sum(Calc1)")));

            ICloudBlob cloudBlob = NewCloudBlob();

            AndTheCloudBlob(BlobPath, cloudBlob);

            Stream tableStream = NewStream(new byte[1]);

            AndTheCloudStream(cloudBlob, tableStream);

            TableLoadResult tableLoadResult = NewTableLoadResult(_ => _.WithRows(NewRowLoadResult(
                row => row.WithFields((Upin, _upin)))));

            AndTheCachedTableLoadResults(_datasetCacheKey, tableLoadResult);
            AndTheTableLoadResultsFromExcel(tableStream, datasetDefinition, tableLoadResult);
            AndTheCoreProviderData(NewApiProviderSummary(_ => _.WithId(_providerId)
                .WithUPIN(_upin)));
            AndTheJob(NewJob(_ => _.WithId(_jobId)
                .WithDefinitionId(CreateInstructGenerateAggregationsAllocationJob)), CreateInstructGenerateAggregationsAllocationJob);

            await WhenTheProcessDatasetMessageIsProcessed();

            await _jobManagement
                .Received(1)
                .UpdateJobStatus(Arg.Is(invokedByJobId), 0, null, null);

            await _jobManagement
                .Received(1)
                .UpdateJobStatus(Arg.Is(invokedByJobId), 100, true, "Processed Dataset");
        }

        private CalculationResponseModel NewCalculation(Action<CalculationResponseBuilder> setUp = null)
        {
            CalculationResponseBuilder calculationResponseBuilder = new CalculationResponseBuilder();

            setUp?.Invoke(calculationResponseBuilder);

            return calculationResponseBuilder.Build();
        }

        private string NewRandomString() => new RandomString();

        private async Task WhenTheProcessDatasetMessageIsProcessed()
        {
            await _service.ProcessDataset(_message);
        }

        private void GivenTheMessageProperties(params (string, string)[] properties)
        {
            _message.AddUserProperties(properties);
        }

        private void AndTheMessageBody<TBody>(TBody body)
            where TBody : class
        {
            _message.Body = body.AsJsonBytes();
        }

        private void AndTheCloudBlob(string blobName, ICloudBlob cloudBlob)
        {
            _blobClient
                .GetBlobReferenceFromServerAsync(blobName)
                .Returns(cloudBlob);
        }

        private void AndTheTableLoadResultsFromExcel(Stream stream, DatasetDefinition datasetDefinition, params TableLoadResult[] tableLoadResults)
        {
            _excelDatasetReader
                .Read(Arg.Is(stream), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults);
        }

        private void AndTheCloudStream(ICloudBlob cloudBlob, Stream stream)
        {
            _blobClient
                .DownloadToStreamAsync(cloudBlob)
                .Returns(stream);
        }

        private TableLoadResult NewTableLoadResult(Action<TableLoadResultBuilder> setUp = null)
        {
            TableLoadResultBuilder loadResultBuilder = new TableLoadResultBuilder();

            setUp?.Invoke(loadResultBuilder);

            return loadResultBuilder.Build();
        }

        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetBuilder datasetBuilder = new DatasetBuilder();

            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
        }

        public DatasetRelationshipSummary NewRelationshipSummary(Action<DataRelationshipSummaryBuilder> setUp = null)
        {
            DataRelationshipSummaryBuilder relationshipSummaryBuilder = new DataRelationshipSummaryBuilder();

            setUp?.Invoke(relationshipSummaryBuilder);

            return relationshipSummaryBuilder.Build();
        }

        private DefinitionSpecificationRelationship NewRelationship(Action<DefinitionSpecificationRelationshipBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipBuilder relationshipBuilder = new DefinitionSpecificationRelationshipBuilder();

            setUp?.Invoke(relationshipBuilder);

            return relationshipBuilder.Build();
        }

        private SpecificationSummary NewSpecification(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder relationshipBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(relationshipBuilder);

            return relationshipBuilder.Build();
        }

        private DatasetRelationshipVersion NewRelationshipVersion(Action<DatasetRelationshipVersionBuilder> setUp = null)
        {
            DatasetRelationshipVersionBuilder relationshipVersionBuilder = new DatasetRelationshipVersionBuilder()
                .WithVersion(1);

            setUp?.Invoke(relationshipVersionBuilder);

            return relationshipVersionBuilder.Build();
        }

        private DatasetVersion NewDatasetVersion(Action<DatasetVersionBuilder> setUp = null)
        {
            DatasetVersionBuilder datasetVersionBuilder = new DatasetVersionBuilder()
                .WithBlobName(BlobPath)
                .WithVersion(1);

            setUp?.Invoke(datasetVersionBuilder);

            return datasetVersionBuilder.Build();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        public BuildProject NewBuildProject(Action<BuildProjectBuilder> setUp = null)
        {
            BuildProjectBuilder projectBuilder = new BuildProjectBuilder()
                .WithId(BuildProjectId);

            setUp?.Invoke(projectBuilder);

            return projectBuilder.Build();
        }

        private DatasetDefinition NewDatasetDefinition(Action<DatasetDefinitionBuilder> setUp = null)
        {
            DatasetDefinitionBuilder definitionBuilder = new DatasetDefinitionBuilder()
                .WithId(DataDefintionId);

            setUp?.Invoke(definitionBuilder);

            return definitionBuilder.Build();
        }

        private void AndTheRelationship(string id, DefinitionSpecificationRelationship relationship)
        {
            _datasetRepository
                .GetDefinitionSpecificationRelationshipById(id)
                .Returns(relationship);
        }

        private void AndTheSpecification(string id, Common.ApiClient.Specifications.Models.SpecificationSummary specificationSummary)
        {
            _specificationsApiClient
                .GetSpecificationSummaryById(id)
                .Returns(new ApiResponse<Common.ApiClient.Specifications.Models.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));
        }

        private void AndTheDatasetDefinitions(params DatasetDefinition[] datasetDefinitions)
        {
            _datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);
        }

        private void AndTheBuildProject(string specificationId, BuildProject buildProject)
        {
            _calculationsRepository
                .GetBuildProjectBySpecificationId(specificationId)
                .Returns(buildProject);
        }

        private void AndTheCompileResponse(HttpStatusCode statusCode)
        {
            _calculationsRepository
                .CompileAndSaveAssembly(SpecificationId)
                .Returns(statusCode);
        }

        private ICloudBlob NewCloudBlob(Action<ICloudBlob> setUp = null)
        {
            ICloudBlob cloudBlob = Substitute.For<ICloudBlob>();

            setUp?.Invoke(cloudBlob);

            return cloudBlob;
        }

        private RowLoadResult NewRowLoadResult(Action<RowLoadResultBuilder> setUp = null)
        {
            RowLoadResultBuilder loadResultBuilder = new RowLoadResultBuilder();

            setUp?.Invoke(loadResultBuilder);

            return loadResultBuilder.Build();
        }

        private TableDefinition NewTableDefinition(Action<TableDefinitionBuilder> setUp = null)
        {
            TableDefinitionBuilder definitionBuilder = new TableDefinitionBuilder();

            setUp?.Invoke(definitionBuilder);

            return definitionBuilder.Build();
        }

        private FieldDefinition NewFieldDefinition(Action<FieldDefinitionBuilder> setUp = null)
        {
            FieldDefinitionBuilder definitionBuilder = new FieldDefinitionBuilder();

            setUp?.Invoke(definitionBuilder);

            return definitionBuilder.Build();
        }

        private Stream NewStream(byte[] buffer = null)
        {
            return new MemoryStream(buffer ?? new byte[0]);
        }

        private ApiProviderSummary NewApiProviderSummary(Action<ApiProviderSummaryBuilder> setUp = null)
        {
            ApiProviderSummaryBuilder providerSummaryBuilder = new ApiProviderSummaryBuilder();

            setUp?.Invoke(providerSummaryBuilder);

            return providerSummaryBuilder.Build();
        }

        private ApiProviderVersion NewApiProviderVersion(Action<ApiProviderVersionBuilder> setUp = null)
        {
            ApiProviderVersionBuilder providerVersionBuilder = new ApiProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        private ProviderSourceDatasetVersion NewProviderSourceDatasetVersion(Action<ProviderSourceDatasetVersionBuilder> setUp = null)
        {
            ProviderSourceDatasetVersionBuilder sourceDatasetVersionBuilder = new ProviderSourceDatasetVersionBuilder();

            setUp?.Invoke(sourceDatasetVersionBuilder);

            return sourceDatasetVersionBuilder.Build();
        }

        private ProviderSourceDataset NewProviderSourceDataset(Action<ProviderSourceDatasetBuilder> setUp = null)
        {
            ProviderSourceDatasetBuilder sourceDatasetBuilder = new ProviderSourceDatasetBuilder()
                .WithProviderId(_providerId)
                .WithSpecificationId(SpecificationId)
                .WithDataDefinitionId(DataDefintionId);

            setUp?.Invoke(sourceDatasetBuilder);

            return sourceDatasetBuilder.Build();
        }

        private VersionReference NewVersionReference(Action<VersionReferenceBuilder> setUp = null)
        {
            VersionReferenceBuilder referenceBuilder = new VersionReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private Job NewJob(Action<ApiJobBuilder> setUp = null)
        {
            ApiJobBuilder jobBuilder = new ApiJobBuilder();

            setUp?.Invoke(jobBuilder);

            return jobBuilder.Build();
        }

        private void ThenTheErrorWasLogged(string errorMessage)
        {
            _logger
                .Received(1)
                .Error(errorMessage);
        }

        private void AndAnExceptionWasLogged()
        {
            _logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        private void AndTheCachedTableLoadResults(string cacheKey, params TableLoadResult[] tableLoadResults)
        {
            _cacheProvider
                .GetAsync<TableLoadResult[]>(cacheKey)
                .Returns(tableLoadResults);
        }

        private void AndIsUseFieldDefinitionIdsInSourceDatasetsEnabledIs(bool flag)
        {
            _featureToggle
                .IsProviderResultsSpecificationCleanupEnabled()
                .Returns(flag);
        }

        private void AndTheCalculations(params CalculationResponseModel[] calculations)
        {
            _calculationsRepository
                .GetCurrentCalculationsBySpecificationId(SpecificationId)
                .Returns(calculations);
        }

        private void AndTheCoreProviderData(params ApiProviderSummary[] providerSummaries)
        {
            _providersApiClient
                .FetchCoreProviderData(SpecificationId)
                .Returns(new ApiResponse<IEnumerable<ApiProviderSummary>>(HttpStatusCode.OK, providerSummaries));
        }

        private void AndTheCoreProviderVersion(ApiProviderVersion providerVersion)
        {
            _providersApiClient
                .GetProvidersByVersion(SpecificationId)
                .Returns(new ApiResponse<ApiProviderVersion>(HttpStatusCode.OK, providerVersion));
        }

        private void AndTheExistingProviderDatasets(params ProviderSourceDataset[] providerSourceDatasets)
        {
            _providerResultsRepository
                .GetCurrentProviderSourceDatasets(SpecificationId, _relationshipId)
                .Returns(providerSourceDatasets);
        }

        private async Task ThenNoResultsWereSaved()
        {
            await AndNoResultsWereSaved();
        }

        private async Task AndNoResultsWereSaved()
        {
            await
                _providerResultsRepository
                    .DidNotReceive()
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDataset>>());

            await
                _providerResultsRepository
                    .DidNotReceive()
                    .UpdateProviderSourceDatasetHistory(Arg.Any<IEnumerable<ProviderSourceDatasetHistory>>());

        }

        private async Task ThenXProviderSourceDatasetsWereUpdate(int expectedCount)
        {
            await
                _providerResultsRepository
                    .Received(1)
                    .UpdateCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDataset>>(_ =>
                        _.Count() == expectedCount));
        }

        private void AndTheNewProviderDatasetVersion(ProviderSourceDatasetVersion existingVersion, ProviderSourceDatasetVersion newVersion)
        {
            _versionRepository
                .CreateVersion(Arg.Any<ProviderSourceDatasetVersion>(),
                    Arg.Is(existingVersion),
                    Arg.Is(_providerId))
                .Returns(newVersion);
        }

        private async Task ThenTheProviderSourceDatasetWasUpdated(string expectedProviderId = null,
            int times = 1,
            Func<ProviderSourceDataset, bool> extraConstraints = null)
        {
            await
                _providerResultsRepository
                    .Received(times)
                    .UpdateCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDataset>>(
                        _ => FirstProviderSourceDatasetMatches(_, expectedProviderId, extraConstraints)));
        }

        private async Task AndTheProviderSourceDatasetWasDeleted(string expectedProviderId = null,
            int times = 1,
            Func<ProviderSourceDataset, bool> extraConstraints = null)
        {
            await
                _providerResultsRepository
                    .Received(times)
                    .DeleteCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDataset>>(
                        _ => FirstProviderSourceDatasetMatches(_, expectedProviderId, extraConstraints)));
        }

        private bool FirstProviderSourceDatasetMatches(IEnumerable<ProviderSourceDataset> providerSourceDataset,
            string expectedProviderId,
            Func<ProviderSourceDataset, bool> extraConstraints = null)
        {
            ProviderSourceDataset ds = providerSourceDataset?.FirstOrDefault();

            return ds != null &&
                   !string.IsNullOrEmpty(ds.Id) &&
                   ds.DataDefinition.Id == DataDefintionId &&
                   ds.DataGranularity == DataGranularity.SingleRowPerProvider &&
                   ds.DefinesScope == false &&
                   ds.SpecificationId == SpecificationId &&
                   ds.ProviderId == (expectedProviderId ?? _providerId) &&
                   (extraConstraints == null || extraConstraints(ds));
        }

        private async Task AndTheCleanUpDatasetTopicWasNotified(int times)
        {
            await _messengerService
                .Received(times)
                .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.ProviderSourceDatasetCleanup),
                    Arg.Any<SpecificationProviders>(),
                    Arg.Any<IDictionary<string, string>>(),
                    Arg.Is(true));
        }

        private async Task AndTheProviderDatasetVersionKeyWasInvalidated()
        {
            await
                _versionKeyProvider
                    .Received(1)
                    .AddOrUpdateProviderSourceDatasetVersionKey(_relationshipId, Arg.Is<Guid>(_ => _ != Guid.Empty));
        }

        private async Task AndNoAggregationsWereCreated()
        {
            await
                _datasetsAggregationsRepository
                    .DidNotReceive()
                    .CreateDatasetAggregations(Arg.Any<DatasetAggregations>());
        }

        private async Task AndTheCachedAggregationsWereInvalidated()
        {
            await
                _cacheProvider
                    .Received(1)
                    .RemoveAsync<List<CalculationAggregation>>(Arg.Is(_datasetAggregationsCacheKey));
        }

        private async Task ThenTheDatasetAggregationsWereSaved(string expectedRelationshipId = null,
            Func<DatasetAggregations, bool> extraConstraints = null)
        {
            await
                _datasetsAggregationsRepository
                    .Received(1)
                    .CreateDatasetAggregations(Arg.Is<DatasetAggregations>(
                        agg => agg.DatasetRelationshipId == (expectedRelationshipId ?? _relationshipId) &&
                             agg.SpecificationId == SpecificationId &&
                             (extraConstraints == null || extraConstraints(agg))));
        }

        private async Task ThenTheNewVersionWasSaved(ProviderSourceDatasetVersion newVersion)
        {
            await
                _versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<ProviderSourceDatasetVersion>>(
                        ver => ReferenceEquals(ver.First(), newVersion)));
        }

        private async Task AndNoDatasetVersionsWereCreated()
        {
            await
                _versionRepository
                    .DidNotReceive()
                    .CreateVersion(Arg.Any<ProviderSourceDatasetVersion>(), Arg.Any<ProviderSourceDatasetVersion>());
        }

        private async Task AndTheDatasetVersionWasSaved(int version = 1)
        {
            await
                _versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<ProviderSourceDatasetVersion>>(ver =>
                        ver.Count() == 1 &&
                        ver.First().Author != null &&
                        ver.First().Date.Date == DateTime.Now.Date &&
                        ver.First().EntityId == $"{SpecificationId}_{_relationshipId}_{_providerId}" &&
                        ver.First().ProviderSourceDatasetId == $"{SpecificationId}_{_relationshipId}_{_providerId}" &&
                        ver.First().Id == $"{SpecificationId}_{_relationshipId}_{_providerId}_version_{version}" &&
                        ver.First().Rows.Count() == 1 &&
                        ver.First().Version == 1
                    ));
        }

        private void AndNoLoggingStartingWith(string startsWith)
        {
            _logger
                .DidNotReceive()
                .Information(Arg.Is<string>(m => m.StartsWith(startsWith)));
        }

        private void AndTheErrorWasLogged(string expectedError)
        {
            _logger
                .Error(expectedError);
        }

        private void AndTheLoggingWasSent(params string[] expectedLogging)
        {
            foreach (string log in expectedLogging)
            {
                _logger
                    .Received(1)
                    .Information(log);
            }
        }

        private void AndTheJob(Job job, string definitionId)
        {
            _jobsApiClient
                .CreateJob(Arg.Is<JobCreateModel>(_ => _.JobDefinitionId == definitionId))
                .Returns(job);
        }

        private async Task ThenTheAllocationJobWasCreated(string definitionId, string expectedRelationshipId = null)
        {
            expectedRelationshipId = (expectedRelationshipId ?? _relationshipId);

            await
                _jobsApiClient
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        job =>
                            job.InvokerUserDisplayName == Username &&
                            job.InvokerUserId == UserId &&
                            job.JobDefinitionId == definitionId &&
                            job.Properties["specification-id"] == SpecificationId &&
                            job.Properties["provider-cache-key"] == $"{CacheKeys.ScopedProviderSummariesPrefix}{SpecificationId}" &&
                            job.Trigger.EntityId == expectedRelationshipId &&
                            job.Trigger.EntityType == nameof(DefinitionSpecificationRelationship) &&
                            job.Trigger.Message == $"Processed dataset relationship: '{expectedRelationshipId}' for specification: '{SpecificationId}'"
                    ));
        }

        private string GenerateIdentifier(string rawValue)
        {
            return DatasetTypeGenerator.GenerateIdentifier(rawValue);
        }
    }
}
