using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs.Services
{
    public partial class JobManagementServiceTests
    {
        [TestMethod]
        public async Task AddJobLog_GivenJobNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel();

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns((Job)null);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"A job could not be found for job id: '{jobId}'");

            logger
                .Received(1)
                .Error($"A job could not be found for job id: '{jobId}'");
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobFoundAndSetToInProgressButFailedToUpdateJob_ReturnsInternalServerErrorResult()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel();

            Job job = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.Queued
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Is(job))
                .Returns(HttpStatusCode.BadRequest);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to update job id: '{jobId}' with status code '400'");

            logger
                .Received(1)
                .Error($"Failed to update job id: '{jobId}' with status code '400'");

            job
                .RunningStatus
                .Should()
                .Be(RunningStatus.InProgress);
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobFoundAndSetToCompletedAndIsSuccess_EnsuresJobIsUpdatedWithCorrectValues()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = true,
                Outcome = "outcome"
            };

            Job job = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.InProgress
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Is(job))
                .Returns(HttpStatusCode.OK);

            jobRepository
              .CreateJobLog(Arg.Any<JobLog>())
              .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>();

            job.RunningStatus.Should().Be(RunningStatus.Completed);
            job.Completed.Should().NotBeNull();
            job.CompletionStatus.Should().Be(CompletionStatus.Succeeded);
            job.Outcome.Should().Be("outcome");
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobFoundAndSetToCompletedAndIsFailed_EnsuresJobIsUpdatedWithCorrectValues()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = "outcome"
            };

            Job job = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.InProgress
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Is(job))
                .Returns(HttpStatusCode.OK);

            jobRepository
              .CreateJobLog(Arg.Any<JobLog>())
              .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>();

            job.RunningStatus.Should().Be(RunningStatus.Completed);
            job.Completed.Should().NotBeNull();
            job.CompletionStatus.Should().Be(CompletionStatus.Failed);
            job.Outcome.Should().Be("outcome");
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobUpdated_EnsuresNewJobLogCreated()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = "outcome",
                ItemsFailed = 40,
                ItemsProcessed = 100,
                ItemsSucceeded = 60
            };

            Job job = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.InProgress
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Is(job))
                .Returns(HttpStatusCode.OK);

            jobRepository
              .CreateJobLog(Arg.Any<JobLog>())
              .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>();

            await
                jobRepository
                .Received(1)
                .CreateJobLog(Arg.Is<JobLog>(m =>
                    !string.IsNullOrWhiteSpace(m.Id) &&
                    m.JobId == jobId &&
                    m.ItemsProcessed == 100 &&
                    m.ItemsFailed == 40 &&
                    m.ItemsSucceeded == 60 &&
                    m.CompletedSuccessfully == false));
           
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobUpdatedButCreatingJobLogFails_ThrowsExceptionDoesNotSendNotification()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = "outcome",
                ItemsFailed = 40,
                ItemsProcessed = 100,
                ItemsSucceeded = 60
            };

            Job job = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.InProgress
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Is(job))
                .Returns(HttpStatusCode.OK);

            jobRepository
              .CreateJobLog(Arg.Any<JobLog>())
              .Returns(HttpStatusCode.BadRequest);

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository: jobRepository, notificationService: notificationService);

            //Act
            Func<Task> test = async() => await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which.Message
                .Should()
                .Be($"Failed to create a job log for job id: '{jobId}'");

            await
                notificationService
                    .DidNotReceive()
                    .SendNotification(Arg.Any<JobNotification>());
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobUpdated_EnsuresNewNotificationIsSent()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = "outcome",
                ItemsFailed = 40,
                ItemsProcessed = 100,
                ItemsSucceeded = 60
            };

            Job job = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.InProgress,
                JobDefinitionId = "job-definition-id",
                InvokerUserDisplayName = "authorName",
                InvokerUserId = "authorId",
                ItemCount = 100,
                SpecificationId = "spec-id-1",
                Trigger = new Trigger
                {
                    EntityId = "spec-id-1",
                    EntityType = "Specification",
                    Message = "allocating"
                }
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Is(job))
                .Returns(HttpStatusCode.OK);

            jobRepository
               .CreateJobLog(Arg.Any<JobLog>())
               .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository: jobRepository, logger: logger, notificationService: notificationService);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>();

            await
                notificationService
                .Received(1)
                .SendNotification(Arg.Is<JobNotification>(m =>
                    m.JobId == jobId &&
                    m.JobType == "job-definition-id" &&
                    m.CompletionStatus == CompletionStatus.Failed &&
                    m.InvokerUserDisplayName == "authorName" &&
                    m.InvokerUserId == "authorId" &&
                    m.ItemCount == 100 &&
                    m.Outcome == "outcome" &&
                    m.OverallItemsFailed == 40 &&
                    m.OverallItemsProcessed == 100 &&
                    m.OverallItemsSucceeded == 60 &&
                    m.ParentJobId == null &&
                    m.SpecificationId == "spec-id-1" &&
                    string.IsNullOrWhiteSpace(m.SupersededByJobId) &&
                    m.Trigger.EntityId == "spec-id-1" &&
                    m.Trigger.EntityType == "Specification" &&
                    m.Trigger.Message == "allocating" &&
                    m.RunningStatus == RunningStatus.Completed
                ));

        }
    }
}
