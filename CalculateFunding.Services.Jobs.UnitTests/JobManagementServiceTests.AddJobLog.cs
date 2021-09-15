using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

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
        [DynamicData(nameof(JobLogOutcomeTypeExamples), DynamicDataSourceType.Method)]
        public async Task AddJobLog_WhenNoOutcomeTypeIsSuppliedInTheViewModelItIsDeterminedByTheJobLogDetails(JobLogUpdateModel jobLogUpdateModel,
            OutcomeType? expectedOutcomeType)
        {
            //Arrange
            string jobId = "job-id-1";

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
            job.OutcomeType.Should().Be(expectedOutcomeType);
        }

        public static IEnumerable<object[]> JobLogOutcomeTypeExamples()
        {
            yield return new object[]
            {
                NewJobLogUpdateModel(_ => _.WithCompletedSuccessfully(true)),
                OutcomeType.Succeeded
            };
            yield return new object[]
            {
                NewJobLogUpdateModel(_ => _.WithCompletedSuccessfully(false)),
                OutcomeType.Failed
            };
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobFoundAndSetToCompletedAndIsFailed_EnsuresJobIsUpdatedWithCorrectValues()
        {
            //Arrange
            string jobId = "job-id-1";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = "outcome",
                OutcomeType = NewRandomOutcomeType()
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
            job.OutcomeType.Should().Be(jobLogUpdateModel.OutcomeType);
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
            Func<Task> test = async () => await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

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
                    .SendNotification(Arg.Any<JobSummary>());
        }

        [TestMethod]
        public async Task AddJobLog_GivenJobUpdated_EnsuresNewNotificationIsSent()
        {
            //Arrange
            string jobId = "job-id-1";
            DateTimeOffset lastUpdated = new RandomDateTime();
            List<Outcome> outcomes = new List<Outcome>
            {
                new Outcome
                {
                    Description = "outcome-1"
                }
            };

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
                LastUpdated = lastUpdated,
                ItemCount = 100,
                SpecificationId = "spec-id-1",
                Trigger = new Trigger
                {
                    EntityId = "spec-id-1",
                    EntityType = "Specification",
                    Message = "allocating"
                },
                Outcomes = outcomes
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
                .SendNotification(Arg.Is<JobSummary>(m =>
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
                    m.RunningStatus == RunningStatus.Completed &&
                    m.LastUpdated == lastUpdated &&
                    m.Outcomes.SequenceEqual(outcomes) &&
                    m.OutcomeType == OutcomeType.Failed
                ));

        }


        [TestMethod]
        public async Task AddJobLog_GivenJobUpdatedAndCompletedSuccessfully_EnsuresLatestCacheIsUpdated()
        {
            //Arrange
            string jobId = "job-id-1";
            DateTimeOffset lastUpdated = new RandomDateTime();
            List<Outcome> outcomes = new List<Outcome>
            {
                new Outcome
                {
                    Description = "outcome-1"
                }
            };

            string specificationId = "spec-id-1";
            string jobDefinitionId = "job-definition-id";

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = true,
                Outcome = "outcome",
                ItemsFailed = 40,
                ItemsProcessed = 100,
                ItemsSucceeded = 60
            };

            Job job = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.InProgress,
                JobDefinitionId = jobDefinitionId,
                InvokerUserDisplayName = "authorName",
                InvokerUserId = "authorId",
                LastUpdated = lastUpdated,
                ItemCount = 100,
                SpecificationId = specificationId,
                Trigger = new Trigger
                {
                    EntityId = "spec-id-1",
                    EntityType = "Specification",
                    Message = "allocating"
                },
                Outcomes = outcomes
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

            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Is(specificationId), Arg.Is(jobDefinitionId))
                .Returns(job);

            ILogger logger = CreateLogger();

            INotificationService notificationService = CreateNotificationsService();

            ICacheProvider cacheProvider = CreateCacheProvider();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobRepository: jobRepository,
                logger: logger,
                notificationService: notificationService,
                cacheProvider: cacheProvider);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>();

            await
                notificationService
                .Received(1)
                .SendNotification(Arg.Is<JobSummary>(m =>
                    m.JobId == jobId &&
                    m.JobType == "job-definition-id" &&
                    m.CompletionStatus == CompletionStatus.Succeeded &&
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
                    m.RunningStatus == RunningStatus.Completed &&
                    m.LastUpdated == lastUpdated &&
                    m.Outcomes.SequenceEqual(outcomes) &&
                    m.OutcomeType == OutcomeType.Succeeded
                ));

            string cacheKey = $"{CacheKeys.LatestJobs}{job.SpecificationId}:{job.JobDefinitionId}";
            string latestSuccessfulJobCacheKey = $"{CacheKeys.LatestSuccessfulJobs}{job.SpecificationId}:{job.JobDefinitionId}";


            await cacheProvider
                .Received(1)
                .SetAsync(cacheKey, Arg.Is<JobCacheItem>(_ => _.Job.Id == jobId));

            await cacheProvider
                .Received(1)
                .SetAsync(latestSuccessfulJobCacheKey, Arg.Is<JobCacheItem>(_ => _.Job.Id == jobId));
        }

        [DataTestMethod]
        [DataRow(CompletionStatus.Cancelled)]
        [DataRow(CompletionStatus.Failed)]
        [DataRow(CompletionStatus.Superseded)]
        [DataRow(CompletionStatus.TimedOut)]
        public async Task AddJobLog_GivenJobUpdatedAndCompletedUnsuccesfully_EnsuresLatestCacheIsNotUpdated(CompletionStatus completionStatus)
        {
            //Arrange
            string jobId = "job-id-1";
            DateTimeOffset lastUpdated = new RandomDateTime();
            List<Outcome> outcomes = new List<Outcome>
            {
                new Outcome
                {
                    Description = "outcome-1"
                }
            };

            string specificationId = "spec-id-1";
            string jobDefinitionId = "job-definition-id";

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
                JobDefinitionId = jobDefinitionId,
                InvokerUserDisplayName = "authorName",
                InvokerUserId = "authorId",
                LastUpdated = lastUpdated,
                ItemCount = 100,
                SpecificationId = specificationId,
                Trigger = new Trigger
                {
                    EntityId = "spec-id-1",
                    EntityType = "Specification",
                    Message = "allocating"
                },
                Outcomes = outcomes
            };

            Job completedJob = new Job
            {
                Id = jobId,
                RunningStatus = RunningStatus.Completed,
                JobDefinitionId = jobDefinitionId,
                InvokerUserDisplayName = "authorName",
                InvokerUserId = "authorId",
                LastUpdated = lastUpdated,
                ItemCount = 100,
                SpecificationId = specificationId,
                Trigger = new Trigger
                {
                    EntityId = "spec-id-1",
                    EntityType = "Specification",
                    Message = "allocating"
                },
                Outcomes = outcomes,
                CompletionStatus = completionStatus,
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

            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Is(specificationId), Arg.Is(jobDefinitionId))
                .Returns(completedJob);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobRepository: jobRepository,
                logger: logger,
                cacheProvider: cacheProvider);

            //Act
            IActionResult actionResult = await jobManagementService.AddJobLog(jobId, jobLogUpdateModel);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>();

            string cacheKey = $"{CacheKeys.LatestJobs}{job.SpecificationId}:{job.JobDefinitionId}";
            string latestSuccessfulJobCacheKey = $"{CacheKeys.LatestSuccessfulJobs}{job.SpecificationId}:{job.JobDefinitionId}";


            await cacheProvider
                .Received(1)
                .SetAsync(cacheKey, Arg.Is<JobCacheItem>(_ => _.Job.Id == jobId));

            await cacheProvider
                .Received(0)
                .SetAsync(latestSuccessfulJobCacheKey, Arg.Is<JobCacheItem>(_ => _.Job.Id == jobId));
        }
    }
}
