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

namespace CalculateFunding.Services.Calcs
{
    [TestClass]
    public class JobManagementServiceTests
    {
        const string jobDefinitionId = "JobDefinition";

        [TestMethod]
        public async Task CreateJobs_GivenEmptyArrayOfJobCreateModels_ReturnsBadrequest()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = Enumerable.Empty<JobCreateModel>();

            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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

            HttpRequest request = Substitute.For<HttpRequest>();

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns((IEnumerable<JobDefinition>)null);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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

            HttpRequest request = Substitute.For<HttpRequest>();

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, logger: logger);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                    Trigger = new Trigger()
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            HttpRequest request = Substitute.For<HttpRequest>();

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "authorId"),
                new Claim(ClaimTypes.Name, "authorname")
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository);

            //Act
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                    InvokerUserDisplayName = "authorname"
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                    InvokerUserDisplayName = "authorname"
                }
            };

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                    InvokerUserDisplayName = "authorname"
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

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                    SpecificationId = "spec-id-1"
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

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                    SpecificationId = "spec-id-1"
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

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                                m.Completed.Value.Date == DateTimeOffset.Now.ToLocalTime().Date &&
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
                               m.Completed.Value.Date == DateTimeOffset.Now.ToLocalTime().Date &&
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
                    SpecificationId = "spec-id-1"
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

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                            string.IsNullOrWhiteSpace(m.ParentJobId) &&
                            m.StatusDateTime.Date == DateTimeOffset.Now.Date
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
                    SpecificationId = "spec-id-1"
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

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
                            string.IsNullOrWhiteSpace(m.ParentJobId) &&
                            m.StatusDateTime.Date == DateTimeOffset.Now.Date
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
        public async Task CreateJobs_GivenMultipleCreateJobsThatDoNotSupersede_CreatesTwoNotificationsReturnsOKObjectResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname"
                },
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname"
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

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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
        public async Task CreateJobs_GivenMultipleCreateJobsAndOneSupersedesOneCurrentJob_CreatesThreeNotificationsReturnsOKObjectResult()
        {
            //Arrange
            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname"
                },
                new JobCreateModel
                {
                    JobDefinitionId = "job=def-2",
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname"
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

            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult actionResult = await jobManagementService.CreateJobs(jobs, request);

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

        public JobManagementService CreateJobManagementService(
            IJobRepository jobRepository = null,
            INotificationService notificationService = null,
            IJobDefinitionsService jobDefinitionsService = null,
            IJobsResiliencePolicies resilliencePolicies = null,
            ILogger logger = null)
        {
            return new JobManagementService(
                    jobRepository ?? CreateJobRepository(),
                    notificationService ?? CreateNotificationsService(),
                    jobDefinitionsService ?? CreateJobDefinitionsService(),
                    resilliencePolicies ?? CreateResilliencePolicies(),
                    logger ?? CreateLogger()
                );
        }

        public IJobRepository CreateJobRepository()
        {
            return Substitute.For<IJobRepository>();
        }

        public INotificationService CreateNotificationsService()
        {
            return Substitute.For<INotificationService>();
        }

        public IJobDefinitionsService CreateJobDefinitionsService()
        {
            return Substitute.For<IJobDefinitionsService>();
        }

        public ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public IJobsResiliencePolicies CreateResilliencePolicies()
        { 
            return JobsResilienceTestHelper.GenerateTestPolicies();
        }
    }
}
