using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
{
    public partial class JobManagementServiceTests
    {
        [TestMethod]
        public async Task CreateJobs_GivenEmptyArrayOfJobCreateModels_ReturnsBadrequest()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = Enumerable.Empty<JobCreateModel>();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Empty collection of job create models was provided");

            logger
                .Received(1)
                .Warning(Arg.Is("Empty collection of job create models was provided"));
        }

        [TestMethod]
        public async Task CreateJobs_GivenNoJobDefinitionsReturned_ReturnsInternalServerError()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger()
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns((IEnumerable<JobDefinition>)null);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Failed to retrieve job definitions");

            logger
                .Received(1)
                .Error(Arg.Is("Failed to retrieve job definitions"));
        }

        [TestMethod]
        public async Task CreateJobs_GivenJobDefinitionNotFound_ReturnsPreconditionFailedResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger()
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = "any-id"
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"A job definition could not be found for id: {jobDefinitionId}");

            logger
                .Received(1)
                .Warning(Arg.Is($"A job definition could not be found for id: {jobDefinitionId}"));
        }

        [TestMethod]
        public async Task CreateJobs_GivenUserDetailsNotInModel_GetsUserDetailsFromRequest()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            Reference user = new Reference("authorId", "authorname");

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, user);

            //Assert
            await
                jobRepository
                    .Received(1)
                    .CreateJob(Arg.Is<Job>(m => m.InvokerUserDisplayName == "authorname" && m.InvokerUserId == "authorId"));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobReturnsNull_ReturnsNewInternalServerError()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns((Job)null);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to create a job for job definition id: {jobDefinitionId}");

            logger
                .Received(1)
                .Error($"Failed to create a job for job definition id: {jobDefinitionId}");
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobReturnsThrowsException_ReturnsNewInternalServerError()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .When(x => x.CreateJob(Arg.Any<Job>()))
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to create a job for job definition id: {jobDefinitionId}");

            logger
                .Received(1)
                .Error($"Failed to create a job for job definition id: {jobDefinitionId}");
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobReturnsJob_ReturnsOKObjectResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobDoesSupersededButNoNonCompletedJobsToSupersede_ReturnsOKObjectResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                     SupersedeExistingRunningJobOnEnqueue = true
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns((IEnumerable<Job>)null);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            await
                jobRepository
                    .DidNotReceive()
                    .UpdateJob(Arg.Any<Job>());
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobDoesSupersededAndFoundJobsToSupersede_ReturnsOKObjectResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
           {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                     SupersedeExistingRunningJobOnEnqueue = true
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = Guid.NewGuid().ToString()
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job { Id = "current-job-1" },
                new Job { Id = "current-job-2" }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            INotificationService notificationService = CreateNotificationsService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, notificationService: notificationService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            await
                jobRepository
                    .Received(1)
                    .UpdateJob(Arg.Is<Job>(
                            m =>
                                m.Id == "current-job-1" &&
                                m.CompletionStatus == CompletionStatus.Superseded &&
                                !string.IsNullOrWhiteSpace(m.SupersededByJobId) &&
                                m.RunningStatus == RunningStatus.Completed
                        ));

            await
               jobRepository
                   .Received(1)
                   .UpdateJob(Arg.Is<Job>(
                           m =>
                               m.Id == "current-job-2" &&
                               m.CompletionStatus == CompletionStatus.Superseded &&
                               !string.IsNullOrWhiteSpace(m.SupersededByJobId) &&
                               m.RunningStatus == RunningStatus.Completed
                       ));

            await
                notificationService
                    .Received(3)
                    .SendNotification(Arg.Any<JobNotification>());
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreate_EnsuresCorrectMessageSentReturnsOKObjectResult()
        {
            //Arrange
            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                     SupersedeExistingRunningJobOnEnqueue = true
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                }
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-1",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-2",
                        EntityType = "e-type-1",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-2",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-3",
                        EntityType = "e-type-2",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            INotificationService notificationService = CreateNotificationsService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, notificationService: notificationService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();


            await
                notificationService
                    .Received(1)
                    .SendNotification(Arg.Is<JobNotification>(
                        m => m.JobId == jobId &&
                            m.JobType == jobDefinitionId &&
                            m.RunningStatus == RunningStatus.Queued &&
                            m.SpecificationId == job.SpecificationId &&
                            m.InvokerUserDisplayName == "authorname" &&
                            m.InvokerUserId == "authorId" &&
                            m.ItemCount == 1000 &&
                            m.Trigger.Message == "test" &&
                            m.Trigger.EntityType == "e-type" &&
                            m.Trigger.EntityId == "e-1" &&
                            string.IsNullOrWhiteSpace(m.ParentJobId)
                        ));

            await
                notificationService
                    .Received(1)
                    .SendNotification(Arg.Is<JobNotification>(
                        m => m.JobId == "current-job-1" &&
                            m.JobType == jobDefinitionId &&
                            m.RunningStatus == RunningStatus.Completed &&
                            m.SpecificationId == job.SpecificationId &&
                            m.InvokerUserDisplayName == "authorname" &&
                            m.InvokerUserId == "authorId" &&
                            m.ItemCount == 100 &&
                            m.Trigger.Message == "test" &&
                            m.Trigger.EntityType == "e-type-1" &&
                            m.Trigger.EntityId == "e-2" &&
                            string.IsNullOrWhiteSpace(m.ParentJobId) &&
                            m.SupersededByJobId == jobId &&
                            m.CompletionStatus == CompletionStatus.Superseded

                        ));

            await
               notificationService
                   .Received(1)
                   .SendNotification(Arg.Is<JobNotification>(
                       m => m.JobId == "current-job-2" &&
                           m.JobType == jobDefinitionId &&
                           m.RunningStatus == RunningStatus.Completed &&
                           m.SpecificationId == job.SpecificationId &&
                           m.InvokerUserDisplayName == "authorname" &&
                           m.InvokerUserId == "authorId" &&
                           m.ItemCount == 100 &&
                           m.Trigger.Message == "test" &&
                           m.Trigger.EntityType == "e-type-2" &&
                           m.Trigger.EntityId == "e-3" &&
                           string.IsNullOrWhiteSpace(m.ParentJobId) &&
                           m.SupersededByJobId == jobId &&
                           m.CompletionStatus == CompletionStatus.Superseded

                       ));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateButFailsToUpdateSuperseded_DoesNotSendMessage()
        {
            //Arrange
            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                     SupersedeExistingRunningJobOnEnqueue = true
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                }
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-1",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-2",
                        EntityType = "e-type-1",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-2",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-3",
                        EntityType = "e-type-2",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.BadRequest);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            INotificationService notificationService = CreateNotificationsService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, notificationService: notificationService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();


            await
                notificationService
                    .Received(1)
                    .SendNotification(Arg.Is<JobNotification>(
                        m => m.JobId == jobId &&
                            m.JobType == jobDefinitionId &&
                            m.RunningStatus == RunningStatus.Queued &&
                            m.SpecificationId == job.SpecificationId &&
                            m.InvokerUserDisplayName == "authorname" &&
                            m.InvokerUserId == "authorId" &&
                            m.ItemCount == 1000 &&
                            m.Trigger.Message == "test" &&
                            m.Trigger.EntityType == "e-type" &&
                            m.Trigger.EntityId == "e-1" &&
                            string.IsNullOrWhiteSpace(m.ParentJobId)
                        ));

            await
                notificationService
                    .DidNotReceive()
                    .SendNotification(Arg.Is<JobNotification>(
                        m => m.JobId == "current-job-1"));

            await
               notificationService
                   .DidNotReceive()
                   .SendNotification(Arg.Is<JobNotification>(
                       m => m.JobId == "current-job-2"));

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to update superseded job, Id: {currentJobs.ElementAt(0).Id}"));

            logger
               .Received(1)
               .Error(Arg.Is($"Failed to update superseded job, Id: {currentJobs.ElementAt(1).Id}"));
        }

        [TestMethod]
        public async Task CreateJobs_GivenMultipleCreateJobsThatDoNotSupersede_CreatesTwoNotificationsScalesUpTwoTimesReturnsOKObjectResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                },
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                     SupersedeExistingRunningJobOnEnqueue = true
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService,
                jobRepository: jobRepository, logger: logger, notificationService: notificationService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;
            IEnumerable<Job> newJobs = okObjectResult.Value as IEnumerable<Job>;

            newJobs
                .Count()
                .Should()
                .Be(2);

            await
                jobRepository
                    .Received(2)
                    .CreateJob(Arg.Any<Job>());

            await
                notificationService
                    .Received(2)
                    .SendNotification(Arg.Any<JobNotification>());
        }

        [TestMethod]
        public async Task CreateJobs_GivenMultipleCreateJobsAndOneSupersedesOneCurrentJob_CreatesThreeNotificationsScalesUpOneJobReturnsOKObjectResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                },
                new JobCreateModel
                {
                    JobDefinitionId = "job=def-2",
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
           {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                    SupersedeExistingRunningJobOnEnqueue = true
                },
                new JobDefinition
                {
                    Id = "job=def-2"
                }
            };

            Job job1 = new Job
            {
                JobDefinitionId = jobDefinitionId
            };

            Job job2 = new Job
            {
                JobDefinitionId = "job=def-2"
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-1",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-2",
                        EntityType = "e-type-1",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job1, job2);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            ILogger logger = CreateLogger();

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService,
                jobRepository: jobRepository, logger: logger, notificationService: notificationService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;
            IEnumerable<Job> newJobs = okObjectResult.Value as IEnumerable<Job>;

            newJobs
                .Count()
                .Should()
                .Be(2);

            await
                jobRepository
                    .Received(2)
                    .CreateJob(Arg.Any<Job>());

            await
                notificationService
                    .Received(3)
                    .SendNotification(Arg.Any<JobNotification>());
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobModelThatDoesNotValidate_ReturnsBadRequest()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<CreateJobValidationModel> validator = CreateNewCreateJobValidator(validationResult);

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            ILogger logger = CreateLogger();

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService,
                logger: logger, notificationService: notificationService, createJobValidator: validator);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObject = actionResult as BadRequestObjectResult;
            IList<ValidationResult> validationResults = badRequestObject.Value as IList<ValidationResult>;

            validationResults
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobForQueueing_EnsuresMessageIsPlacedOnQueue()
        {
            //Arrange
            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                    SupersedeExistingRunningJobOnEnqueue = true,
                    MessageBusQueue = "TestQueue"
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                },
                Properties = new Dictionary<string, string>
                {
                    { "specificationId", "spec-id-1" }
                },
                MessageBody = "a message"
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-1",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-2",
                        EntityType = "e-type-1",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-2",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-3",
                        EntityType = "e-type-2",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            IMessengerService messengerService = CreateMessengerService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, messengerService: messengerService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            await
                messengerService
                    .Received(1)
                    .SendToQueueAsJson(
                        Arg.Is("TestQueue"), 
                        Arg.Is("a message"), 
                        Arg.Is<Dictionary<string, string>>(
                            m => m.ContainsKey("specificationId") &&
                            m["specificationId"] == "spec-id-1" &&
                            m.ContainsKey("jobId") &&
                            m["jobId"] == jobId
                ));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobForQueueing_DoesNotTryToPlacedOnQueueIfNoQueueOrTopicInDefinition()
        {
            //Arrange
            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                },
                Properties = new Dictionary<string, string>
                {
                    { "specificationId", "spec-id-1" }
                },
                MessageBody = "a message"
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            IMessengerService messengerService = CreateMessengerService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, messengerService: messengerService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            await
                messengerService
                    .Received(0)
                    .SendToQueueAsJson(
                        Arg.Any<string>(), 
                        Arg.Is("a message"), 
                        Arg.Is<Dictionary<string, string>>(
                            m => m.ContainsKey("specificationId") &&
                            m["specificationId"] == "spec-id-1" &&
                            m.ContainsKey("jobId") &&
                            m["jobId"] == jobId
                ));
        }
        
        [TestMethod]
        public async Task CreateJobs_GivenCreateJobForQueueingToRunInSession_EnsuresMessageIsPlacedOnQueue()
        {
            //Arrange
            const string specificationId = "spec-id-1";

            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId =specificationId,
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" },
                        { "specificationId", specificationId }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                    SupersedeExistingRunningJobOnEnqueue = true,
                    MessageBusQueue = "TestQueue",
                    SessionMessageProperty = "specificationId",
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                },
                Properties = new Dictionary<string, string>
                {
                    { "specificationId", specificationId },
                    { "session-id", specificationId }
                },
                MessageBody = "a message"
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-1",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-2",
                        EntityType = "e-type-1",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-2",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-3",
                        EntityType = "e-type-2",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            IMessengerService messengerService = CreateMessengerService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, messengerService: messengerService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            await
                messengerService
                    .Received(1)
                    .SendToQueueAsJson(
                        Arg.Is("TestQueue"),
                        Arg.Is("a message"),
                        Arg.Is<Dictionary<string, string>>(
                            m => m.ContainsKey("specificationId") &&
                            m["specificationId"] == specificationId &&
                            m.ContainsKey("jobId") &&
                            m["jobId"] == jobId), Arg.Is(false), Arg.Is(specificationId));
        }

        [TestMethod]
        public void CreateJobs_GivenShouldRequireSessionIdButSessionPropertyIsMissing_LogsAndThrowsException()
        {
            //Arrange
            const string specificationId = "spec-id-1";

            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = specificationId,
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                    SupersedeExistingRunningJobOnEnqueue = true,
                    MessageBusQueue = "TestQueue",
                    SessionMessageProperty = "blah",
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                },
                Properties = new Dictionary<string, string>
                {
                    { "specificationId", specificationId }
                },
                MessageBody = "a message"
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            IMessengerService messengerService = CreateMessengerService();

            ILogger logger = CreateLogger();

            string errorMessage = $"Missing session property on job with id '{job.Id}";

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, messengerService: messengerService);

            //Act
            Func<Task> test = async () => await jobManagementService.CreateJobs(jobs, null);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobForQueueingOnTopic_EnsuresMessageIsPlacedOnTopicQueue()
        {
            //Arrange
            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                    SupersedeExistingRunningJobOnEnqueue = true,
                    MessageBusTopic = "TestTopic"
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                }
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-1",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-2",
                        EntityType = "e-type-1",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-2",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-3",
                        EntityType = "e-type-2",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            IMessengerService messengerService = CreateMessengerService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, messengerService: messengerService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            await
                messengerService
                    .Received(1)
                    .SendToTopicAsJson(Arg.Is("TestTopic"), Arg.Any<string>(),
                            Arg.Is<Dictionary<string, string>>(m => m.ContainsKey("jobId") && m["jobId"] == jobId));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobForQueueingOnTopicButThrowsException_LogsException()
        {
            //Arrange
            string jobId = Guid.NewGuid().ToString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId,
                    SupersedeExistingRunningJobOnEnqueue = true,
                    MessageBusTopic = "TestTopic"
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id-1",
                Id = jobId,
                ItemCount = 1000,
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Trigger = new Trigger
                {
                    EntityId = "e-1",
                    EntityType = "e-type",
                    Message = "test"
                }
            };

            IEnumerable<Job> currentJobs = new[]
            {
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-1",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-2",
                        EntityType = "e-type-1",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
                new Job {
                    JobDefinitionId = jobDefinitionId,
                    SpecificationId = "spec-id-1",
                    Id = "current-job-2",
                    ItemCount = 100,
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    Trigger = new Trigger
                    {
                        EntityId = "e-3",
                        EntityType = "e-type-2",
                        Message = "test"
                    },
                    RunningStatus = RunningStatus.InProgress
                },
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(job);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            jobRepository
                .GetRunningJobsForSpecificationAndJobDefinitionId(Arg.Is(jobs.First().SpecificationId), Arg.Is(jobDefinitionId))
                .Returns(currentJobs);

            IMessengerService messengerService = CreateMessengerService();
            messengerService
                .When(x => x.SendToTopicAsJson(Arg.Is("TestTopic"), Arg.Any<string>(), Arg.Any<Dictionary<string,string>>()))
                .Do(x => { throw new Exception("Failed to send to topic"); });


            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, messengerService: messengerService);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, null);

            //Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to send to topic"), Arg.Is($"Failed to queue job with id: {jobId} on Queue/topic TestTopic"));
        }
    }
}
