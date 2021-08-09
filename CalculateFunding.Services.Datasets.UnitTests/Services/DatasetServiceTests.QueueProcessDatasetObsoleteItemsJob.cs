using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Datasets.Services;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.UnitTests.Services
{
    [TestClass]
    public class DatasetServiceTestsQueueProcessDatasetObsoleteItemsJob : DatasetServiceTestsBase
    {
        private IDatasetService _datasetService;
        private Mock<IJobManagement> _jobManagement;
        private string _specificationId;

        [TestInitialize]
        public void Setup()
        {
            _specificationId = new RandomString();
            _jobManagement = CreateMockJobManagement();
            _datasetService = CreateDatasetService(jobManagement: _jobManagement.Object);
        }

        [TestMethod]
        public async Task QueueProcessDatasetObsoleteItemsJob_JobAlreadyRunning_ReturnsInternalServerErrorResult()
        {
            string correlationId = "any-id";
            string jobId = "any-id";

            Reference author = new Reference();

            _jobManagement.Setup(_ => _.GetLatestJobsForSpecification(_specificationId, new string[] {
                    JobConstants.DefinitionNames.ProcessDatasetObsoleteItems
            }))
            .ReturnsAsync(new Dictionary<string, JobSummary> { { JobConstants.DefinitionNames.ProcessDatasetObsoleteItems,
                                                                    new JobSummary {
                                                                        JobId = jobId,
                                                                        RunningStatus = RunningStatus.InProgress
                                                                    } } });

            Func<Task> invocation = async () => await _datasetService.QueueProcessDatasetObsoleteItemsJob(_specificationId, author, correlationId);

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .WithMessage($"Unable to queue a new process dataset obsolete items as one is already running job id:{jobId}.");
        }

        [TestMethod]
        public async Task QueueProcessDatasetObsoleteItemsJob_JobActionQueuesJob_ReturnsOKJobDetails()
        {
            string correlationId = "any-id";
            string jobId = "any-id";

            Reference author = new Reference();
            Job job = new Job { Id = jobId };

            _jobManagement.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(j => j.SpecificationId == _specificationId)))
                .ReturnsAsync(job);

            IActionResult actionResult = await _datasetService.QueueProcessDatasetObsoleteItemsJob(_specificationId, author, correlationId);

            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .And
                .NotBeNull();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            okObjectResult.Value.Should().NotBeNull().And.BeAssignableTo<JobCreationResponse>();

            JobCreationResponse actualJob = okObjectResult.Value as JobCreationResponse;

            actualJob.JobId.Should().Be(jobId);
        }

        private Mock<IJobManagement> CreateMockJobManagement()
        {
            return new Mock<IJobManagement>();
        }
    }
}
