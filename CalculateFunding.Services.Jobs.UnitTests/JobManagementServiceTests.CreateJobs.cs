using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
{
    public partial class JobManagementServiceTests
    {
        private const string SfaCorrelationId = "sfa-correlationId";

        [TestMethod]
        public async Task CreateJobs_GivenEmptyArrayOfJobCreateModels_ReturnsBadRequest()
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
            Func<Task> invocation = async () => await jobManagementService.CreateJobs(jobs, user);

            //Assert
            invocation
                .Should()
                .ThrowExactly<JobCreateException>();

            await
                jobRepository
                    .Received(1)
                    .CreateJob(Arg.Is<Job>(m => m.InvokerUserDisplayName == "authorname" && m.InvokerUserId == "authorId"));
        }

        [TestMethod]
        public async Task TryCreateJobs_GivenCreateJobReturnsNull_ReturnsResultWithErrorDetails()
        {
            //Arrange
            JobCreateModel jobCreateModel = new JobCreateModel
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
            };

            IEnumerable<JobCreateModel> jobs = new[]
            {
                jobCreateModel
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
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(new Job());
            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository, logger: logger);

            //Act
            OkObjectResult actionResult = await jobManagementService.TryCreateJobs(jobs, null) as OkObjectResult;

            IEnumerable<JobCreateResult> jobCreateResults = actionResult?.Value as IEnumerable<JobCreateResult>;

            jobCreateResults?.Should()
                .BeEquivalentTo(new JobCreateResult
                {
                    CreateRequest = jobCreateModel,
                    Error = $"Failed to save new job with definition id {jobDefinitionId}"
                });
        }

        [TestMethod]
        public void CreateJobs_GivenCreateJobReturnsNull_ReturnsNewInternalServerError()
        {
            //Arrange
            JobCreateModel jobCreateModel = new JobCreateModel
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "spec-id",
                Trigger = new Trigger(),
                InvokerUserId = "authorId",
                InvokerUserDisplayName = "authorname",
                Properties = new Dictionary<string, string>
                {
                    { "user-id", "authorId" },
                    { "user-name", "authorname" }
                }
            };

            IEnumerable<JobCreateModel> jobs = new[]
            {
                jobCreateModel
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
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Any<string>(), Arg.Any<string>())
                .Returns((Job)null);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository, logger: logger);

            //Act
            Func<Task> invocation = async () => await jobManagementService.CreateJobs(jobs, null);

            //Assert
            invocation
                .Should()
                .Throw<JobCreateException>()
                .Which
                .Details
                .Should()
                .BeEquivalentTo(new JobCreateErrorDetails
                {
                    Error = $"Failed to save new job with definition id {jobDefinitionId}",
                    CreateRequest = jobCreateModel
                });
        }

        [TestMethod]
        public void CreateJobs_GivenCreateJobReturnsThrowsException_ReturnsNewInternalServerError()
        {
            //Arrange
            JobCreateModel jobCreateModel = new JobCreateModel
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
            };

            IEnumerable<JobCreateModel> jobs = new[]
            {
                jobCreateModel
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
            Func<Task> invocation = async () => await jobManagementService.CreateJobs(jobs, null);

            //Assert
            invocation
                .Should()
                .Throw<JobCreateException>()
                .Which
                .Details
                .Should()
                .BeEquivalentTo(new JobCreateErrorDetails
                {
                    Error = $"Exception of type 'System.Exception' was thrown.",
                    CreateRequest = jobCreateModel
                });
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

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService,
                jobRepository: jobRepository,
                logger: logger);

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
        public async Task CreateJobs_GivenCreateJobReturnsJob_ReturnsOKObjectResultAndCacheJobs()
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
                    SpecificationId = "specificationId",
                    Properties = new Dictionary<string, string>
                    {
                        { "user-id", "authorId" },
                        { "user-name", "authorname" }
                    }
                },
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionIdTwo,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "specificationId",
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
                },
                new JobDefinition
                {
                    Id = jobDefinitionIdTwo
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = "specificationId",
            };

            Job jobTwo = new Job
            {
                JobDefinitionId = jobDefinitionIdTwo,
                SpecificationId = "specificationId",
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .CreateJob(Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionIdTwo))
                .Returns(jobTwo);

            jobRepository
                .CreateJob(Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionId))
                .Returns(job);

            ILogger logger = CreateLogger();
            ICacheProvider cacheProvider = CreateCacheProvider();

            string cacheKey = $"{CacheKeys.LatestJobs}{job.SpecificationId}:{job.JobDefinitionId}";
            cacheProvider
                .SetAsync(cacheKey, Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionId))
                .Returns(Task.CompletedTask);

            string cacheKeyTwo = $"{CacheKeys.LatestJobs}{jobTwo.SpecificationId}:{jobTwo.JobDefinitionId}";
            cacheProvider
                .SetAsync(cacheKeyTwo, Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionIdTwo))
                .Returns(Task.CompletedTask);

            string latestSuccessfulJobCacheKey = $"{CacheKeys.LatestSuccessfulJobs}{job.SpecificationId}:{job.JobDefinitionId}";
            cacheProvider
                .SetAsync(latestSuccessfulJobCacheKey, Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionId))
                .Returns(Task.CompletedTask);

            string latestSuccessfulJobCacheKeyTwo = $"{CacheKeys.LatestSuccessfulJobs}{jobTwo.SpecificationId}:{jobTwo.JobDefinitionId}";
            cacheProvider
                .SetAsync(latestSuccessfulJobCacheKeyTwo, Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionIdTwo))
                .Returns(Task.CompletedTask);

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService,
                jobRepository: jobRepository,
                logger: logger,
                cacheProvider: cacheProvider);

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

            await cacheProvider
                .Received(1)
                .SetAsync(cacheKey, Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionId));

            await cacheProvider
                .Received(1)
                .SetAsync(cacheKeyTwo, Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionIdTwo));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobReturnsJobAndJobIsNotAssociatedWithSpecification_ReturnsOKObjectResultAndDoesNotCacheCacheJob()
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
                    SpecificationId = null,
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
                },
                new JobDefinition
                {
                    Id = jobDefinitionIdTwo
                }
            };

            Job job = new Job
            {
                JobDefinitionId = jobDefinitionId,
                SpecificationId = null,
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            IJobRepository jobRepository = CreateJobRepository();

            jobRepository
                .CreateJob(Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionId))
                .Returns(job);

            ILogger logger = CreateLogger();
            ICacheProvider cacheProvider = CreateCacheProvider();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService,
                jobRepository: jobRepository,
                logger: logger,
                cacheProvider: cacheProvider);

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

            await cacheProvider
                .Received(0)
                .SetAsync(Arg.Any<string>(), Arg.Is<Job>(_ => _.JobDefinitionId == jobDefinitionId));
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
                    .SendNotification(Arg.Any<JobSummary>());
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
                    .SendNotification(Arg.Is<JobSummary>(
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
                    .SendNotification(Arg.Is<JobSummary>(
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
                   .SendNotification(Arg.Is<JobSummary>(
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
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Any<string>(), Arg.Any<string>())
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
                    .SendNotification(Arg.Is<JobSummary>(
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
                    .SendNotification(Arg.Is<JobSummary>(
                        m => m.JobId == "current-job-1"));

            await
               notificationService
                   .DidNotReceive()
                   .SendNotification(Arg.Is<JobSummary>(
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
                    .SendNotification(Arg.Any<JobSummary>());
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
                    .SendNotification(Arg.Any<JobSummary>());
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
            string jobId = NewRandomString();
            string correlationId = NewRandomString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    CorrelationId = correlationId,
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
                CorrelationId = correlationId,
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
                            m["jobId"] == jobId &&
                            m.ContainsKey(SfaCorrelationId) &&
                            m[SfaCorrelationId] == correlationId
                ));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobForQueueing_DoesNotTryToPlacedOnQueueIfNoQueueOrTopicInDefinition()
        {
            //Arrange
            string jobId = NewRandomString();
            string correlationId = NewRandomString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    CorrelationId = correlationId,
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
                MessageBody = "a message",
                CorrelationId = correlationId
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
                            m["jobId"] == jobId &&
                            m.ContainsKey(SfaCorrelationId) &&
                            m[SfaCorrelationId] == correlationId
                ));
        }

        [TestMethod]
        public async Task CreateJobs_GivenCreateJobForQueueingToRunInSession_EnsuresMessageIsPlacedOnQueue()
        {
            //Arrange
            const string specificationId = "spec-id-1";

            string jobId = NewRandomString();
            string correlationId = NewRandomString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId =specificationId,
                    CorrelationId = correlationId,
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
                CorrelationId = correlationId,
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
                            m.ContainsKey(SfaCorrelationId) &&
                            m[SfaCorrelationId] == correlationId &&
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
            string jobId = NewRandomString();
            string correlationId = NewRandomString();

            IEnumerable<JobCreateModel> jobs = new[]
            {
                new JobCreateModel
                {
                    JobDefinitionId = jobDefinitionId,
                    Trigger = new Trigger(),
                    InvokerUserId = "authorId",
                    InvokerUserDisplayName = "authorname",
                    SpecificationId = "spec-id-1",
                    CorrelationId = correlationId,
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
                CorrelationId = correlationId,
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
                            Arg.Is<Dictionary<string, string>>(m =>
                                m.ContainsKey("jobId") &&
                                m["jobId"] == jobId &&
                                m.ContainsKey(SfaCorrelationId) &&
                                m[SfaCorrelationId] == correlationId));
        }

        [TestMethod]
        public void CreateJobs_GivenCreateJobForQueueingOnTopicButThrowsException_LogsException()
        {
            //Arrange
            string jobId = Guid.NewGuid().ToString();

            JobCreateModel jobCreateModel = new JobCreateModel
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
            };

            IEnumerable<JobCreateModel> jobs = new[]
            {
                jobCreateModel
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
                .When(x => x.SendToTopicAsJson(Arg.Is("TestTopic"), Arg.Any<string>(), Arg.Any<Dictionary<string, string>>()))
                .Do(x => { throw new Exception("Failed to send to topic"); });


            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(
                jobDefinitionsService: jobDefinitionsService, jobRepository: jobRepository,
                logger: logger, messengerService: messengerService);

            //Act

            Func<Task> result = new Func<Task>(async () => { await jobManagementService.CreateJobs(jobs, null); });

            //Assert
            result
                .Should()
                .Throw<JobCreateException>()
                .Which
                .Details
                .Should()
                .BeEquivalentTo(new JobCreateErrorDetails
                {
                    Error = $"Failed to queue job with id: {job.JobId} on Queue/topic TestTopic. Exception type: 'System.Exception'",
                    CreateRequest = jobCreateModel
                });

            logger
                .Received(1)
                .Error(Arg.Is<Exception>(m => m.Message == "Failed to send to topic"), Arg.Is($"Failed to queue job with id: {jobId} on Queue/topic TestTopic. Exception type: 'System.Exception'"));
        }

        private string NewRandomString() => new RandomString();
    }
}
