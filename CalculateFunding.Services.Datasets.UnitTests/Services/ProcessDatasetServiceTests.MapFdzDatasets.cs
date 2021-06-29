using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class ProcessDatasetServiceMapFdzDatasetsTests : ProcessDatasetServiceTestsBase
    {
        private IDatasetRepository _datasetRepository;
        private IJobsApiClient _jobsApiClient;
        private IJobManagement _jobManagement;
        private ILogger _logger;
        private ProcessDatasetService _service;
        private IMessengerService _messengerService;

        private Message _message;

        private const string BlobPath = "dataset-id/v1/ds.xlsx";

        [TestInitialize]
        public void SetUp()
        {
            _datasetRepository = CreateDatasetsRepository();
            _jobsApiClient = CreateJobsApiClient();
            _messengerService = CreateMessengerService();
            _jobManagement = CreateJobManagement(_jobsApiClient, _logger, _messengerService);
            _logger = CreateLogger();

            _service = CreateProcessDatasetService(
               datasetRepository: _datasetRepository,
               jobManagement: _jobManagement,
               messengerService: _messengerService,
               logger: _logger);

            _message = new Message();
        }

        [TestMethod]
        public void MapFdzDatasets_GivenNullMessage_ThrowsArgumentNullException()
        {
            _message = null;

            Func<Task> invocation = WhenTheMapFdzDatasetsMessageIsProcessed;

            invocation
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void MapFdzDatasets_GivenNoSpecificationIdKeyInProperties_DoesNoProcessing()
        {
            string jobId = NewRandomString();
            GivenTheMessageProperties(("jobId", jobId));
            AndTheJobDetails(jobId, JobConstants.DefinitionNames.MapFdzDatasetsJob);

            Func<Task> invocation = WhenTheMapFdzDatasetsMessageIsProcessed;

            invocation
                .Should()
                .ThrowExactly<NonRetriableException>()
                .WithMessage("Failed to Process - specification id not provided");

            ThenTheErrorWasLogged("Specification Id key is missing in MapFdzDatasets message properties");
        }

        [TestMethod]
        public void MapFdzDatasets_GivenQueueMapDatasetJobFailed_NonRetriableExceptionThrown()
        {
            string jobId = NewRandomString();
            string relationshipId1 = NewRandomString();
            string datasetVersionId1 = NewRandomString();
            string datasetId1 = NewRandomString();

            IEnumerable<DefinitionSpecificationRelationship> relationships = new[]
            {
                NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithDatasetDefinition(NewReference())
                                    .WithRelationshipId(relationshipId1)
                                    .WithDatasetVersion(NewDatasetRelationshipVersion(v => v.WithId(datasetVersionId1))))))
            };

            Dataset dataset1 = NewDataset(_ => _.WithId(datasetId1));

            GivenTheMessageProperties(("jobId", jobId), ("specification-id", SpecificationId), ("user-id", UserId), ("user-name", Username));
            AndTheJobDetails(jobId, JobConstants.DefinitionNames.MapFdzDatasetsJob);
            AndRelationshipsForSpecification(relationships);
            AndDatasetForDatasetVersion(datasetVersionId1, dataset1);
            string exception = "Failed to queue map fdz dataset";
            AndTheMapDatasetFailsToQueue(exception, true);

            Func<Task> invocation = WhenTheMapFdzDatasetsMessageIsProcessed;

            invocation
                .Should()
                .ThrowExactly<NonRetriableException>()
                .WithMessage(exception);
        }

        [TestMethod]
        public void MapFdzDatasets_GivenQueueMapDatasetJobFailed_ExceptionThrown()
        {
            string jobId = NewRandomString();
            string relationshipId1 = NewRandomString();
            string datasetVersionId1 = NewRandomString();
            string datasetId1 = NewRandomString();

            IEnumerable<DefinitionSpecificationRelationship> relationships = new[]
            {
                NewDefinitionSpecificationRelationship(r => r.WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithDatasetDefinition(NewReference())
                                    .WithRelationshipId(relationshipId1)
                                    .WithDatasetVersion(NewDatasetRelationshipVersion(v => v.WithId(datasetVersionId1))))))
            };

            Dataset dataset1 = NewDataset(_ => _.WithId(datasetId1));

            GivenTheMessageProperties(("jobId", jobId), ("specification-id", SpecificationId), ("user-id", UserId), ("user-name", Username));
            AndTheJobDetails(jobId, JobConstants.DefinitionNames.MapFdzDatasetsJob);
            AndRelationshipsForSpecification(relationships);
            AndDatasetForDatasetVersion(datasetVersionId1, dataset1);
            string exception = "Failed to queue map fdz dataset";
            AndTheMapDatasetFailsToQueue(exception);

            Func<Task> invocation = WhenTheMapFdzDatasetsMessageIsProcessed;

            invocation
                .Should()
                .ThrowExactly<Exception>()
                .WithMessage(exception);
        }

        [TestMethod]
        public async Task MapFdzDatasets_GivenSpecificationId_QueueMapDatasetJobsForAllRelationships()
        {
            string jobId = NewRandomString();
            string relationshipId1 = NewRandomString();
            string relationshipId2 = NewRandomString();
            string datasetVersionId1 = NewRandomString();
            string datasetVersionId2 = NewRandomString();
            string datasetId1 = NewRandomString();
            string datasetId2 = NewRandomString();

            IEnumerable<DefinitionSpecificationRelationship> relationships = new[]
            {
                NewDefinitionSpecificationRelationship(r => r
                .WithId(relationshipId1)
                .WithCurrent(
                    NewDefinitionSpecificationRelationshipVersion(_ => _.WithDatasetDefinition(NewReference(d => d.WithId(datasetId1)))
                                    .WithRelationshipId(relationshipId1)
                                    .WithDatasetVersion(NewDatasetRelationshipVersion(v => v.WithId(datasetVersionId1)))))),
                NewDefinitionSpecificationRelationship(r => r
                .WithId(relationshipId2)
                .WithCurrent(
                    NewDefinitionSpecificationRelationshipVersion(_ => _.WithDatasetDefinition(NewReference(d => d.WithId(datasetId2)))
                                    .WithRelationshipId(relationshipId2)
                                    .WithDatasetVersion(NewDatasetRelationshipVersion(v => v.WithId(datasetVersionId2)))))),
            };

            Dataset dataset1 = NewDataset(_ => _.WithId(datasetId1));
            Dataset dataset2 = NewDataset(_ => _.WithId(datasetId2));

            GivenTheMessageProperties(("jobId", jobId), ("specification-id", SpecificationId), ("user-id", UserId), ("user-name", Username));
            AndTheJobDetails(jobId, JobConstants.DefinitionNames.MapFdzDatasetsJob);
            AndRelationshipsForSpecification(relationships);
            AndDatasetForDatasetVersion(datasetVersionId1, dataset1);
            AndDatasetForDatasetVersion(datasetVersionId2, dataset2);

            await WhenTheMapFdzDatasetsMessageIsProcessed();

            await ThenTheMapDatasetJobWasCreated(JobConstants.DefinitionNames.MapDatasetJob, relationshipId1, datasetId1, jobId);
            await ThenTheMapDatasetJobWasCreated(JobConstants.DefinitionNames.MapDatasetJob, relationshipId2, datasetId2, jobId);
        }
        [TestMethod]
        public async Task MapFdzDatasets_GivenSpecificationIdAndRelationshipId_QueueMapDatasetJobForRelationship()
        {
            string jobId = NewRandomString();
            string relationshipId = NewRandomString();
            string datasetVersionId = NewRandomString();
            string datasetId = NewRandomString();

            Dataset dataset = NewDataset(_ => _.WithId(datasetId));

            DefinitionSpecificationRelationship relationship = NewDefinitionSpecificationRelationship(r => 
                                r.WithId(relationshipId)
                                .WithCurrent(NewDefinitionSpecificationRelationshipVersion(_ => _.WithDatasetDefinition(NewReference(d => d.WithId(datasetId)))
                                    .WithRelationshipId(relationshipId)
                                    .WithDatasetVersion(NewDatasetRelationshipVersion(v => v.WithId(datasetVersionId))))));

            

            GivenTheMessageProperties(("jobId", jobId), ("specification-id", SpecificationId), ("relationship-id", relationshipId), ("user-id", UserId), ("user-name", Username));
            AndTheJobDetails(jobId, JobConstants.DefinitionNames.MapFdzDatasetsJob);
            AndRelationshipForSpecification(relationship);
            AndDatasetForDatasetVersion(datasetVersionId, dataset);

            await WhenTheMapFdzDatasetsMessageIsProcessed();

            await ThenTheMapDatasetJobWasCreated(JobConstants.DefinitionNames.MapDatasetJob, relationshipId, datasetId, jobId);
        }

        private void AndTheMapDatasetFailsToQueue(string exception, bool retriable = false)
        {
            if (retriable)
            {
                _jobsApiClient
                    .CreateJob(Arg.Any<JobCreateModel>())
                    .Throws(new NonRetriableException(exception));
            }
            else
            {
                _jobsApiClient
                    .CreateJob(Arg.Any<JobCreateModel>())
                    .Throws(new Exception(exception));
            }
        }

        private void AndDatasetForDatasetVersion(string datasetVersionId, Dataset dataset)
        {
            _datasetRepository.GetDatasetByDatasetId(datasetVersionId)
                .Returns(dataset);
        }

        private void AndRelationshipsForSpecification(IEnumerable<DefinitionSpecificationRelationship> relationships)
        {
            _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(Arg.Any<Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>>>())
                .Returns(relationships);
        }

        private void AndRelationshipForSpecification(DefinitionSpecificationRelationship relationship)
        {
            _datasetRepository.GetDefinitionSpecificationRelationshipById(Arg.Is(relationship.Id))
                .Returns(relationship);
        }

        private string NewRandomString() => new RandomString();

        private async Task WhenTheMapFdzDatasetsMessageIsProcessed()
        {
            await _service.Run(_message, async() => { await _service.MapFdzDatasets(_message); });
        }

        private void GivenTheMessageProperties(params (string, string)[] properties)
        {
            _message.AddUserProperties(properties);
        }

        private void AndTheJobDetails(string jobId, string jobDefinitionId)
        {
            _jobsApiClient.GetJobById(Arg.Is(jobId))
                    .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel() { Id = jobId, JobDefinitionId = jobDefinitionId }));
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

        private DefinitionSpecificationRelationshipVersion NewDefinitionSpecificationRelationshipVersion(Action<DefinitionSpecificationRelationshipVersionBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipVersionBuilder relationshipVersionBuilder = new DefinitionSpecificationRelationshipVersionBuilder();

            setUp?.Invoke(relationshipVersionBuilder);

            return relationshipVersionBuilder.Build();
        }

        private DefinitionSpecificationRelationship NewDefinitionSpecificationRelationship(Action<DefinitionSpecificationRelationshipBuilder> setUp = null)
        {
            DefinitionSpecificationRelationshipBuilder relationshipBuilder = new DefinitionSpecificationRelationshipBuilder();

            setUp?.Invoke(relationshipBuilder);

            return relationshipBuilder.Build();
        }

        private DatasetRelationshipVersion NewDatasetRelationshipVersion(Action<DatasetRelationshipVersionBuilder> setUp = null)
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

        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetBuilder datasetBuilder = new DatasetBuilder();

            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
        }

        private Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private async Task ThenTheMapDatasetJobWasCreated(string definitionId, string expectedRelationshipId, string expectedDatasetId, string parentJobId)
        {
            await
                _jobsApiClient
                    .Received(1)
                    .CreateJob(Arg.Is<JobCreateModel>(
                        job =>
                            job.InvokerUserDisplayName == Username &&
                            job.InvokerUserId == UserId &&
                            job.JobDefinitionId == definitionId &&
                            job.Properties["specification-id"] == SpecificationId &&
                            job.Properties["relationship-id"] == expectedRelationshipId &&
                            job.ParentJobId == parentJobId &&
                            job.Trigger.EntityId == expectedDatasetId &&
                            job.Trigger.EntityType == nameof(Dataset) &&
                            job.Trigger.Message == $"Mapping dataset: '{expectedDatasetId}'"
                    ));
        }
    }
    }
