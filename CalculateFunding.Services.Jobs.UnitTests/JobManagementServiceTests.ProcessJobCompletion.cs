using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
{
    public partial class JobManagementServiceTests
    {
        [TestMethod]
        public void ProcesJobCompletion_MessageIsNull_ArgumentNullExceptionThrown()
        {
            // Arrange
            JobManagementService jobManagementService = CreateJobManagementService();

            // Act
            Func<Task> action = async () => await jobManagementService.ProcessJobNotification(null);

            // Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("message");
        }

        [TestMethod]
        public void ProcessJobCompletion_MessageBodyIsNull_ArgumentNullExceptionThrown()
        {
            // Arrange
            JobManagementService jobManagementService = CreateJobManagementService();

            Message message = new Message();

            // Act
            Func<Task> action = async () => await jobManagementService.ProcessJobNotification(message);

            // Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("message payload");
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobIsNotComplete_ThenNoActionTaken()
        {
            // Arrange
            IJobRepository jobRepository = CreateJobRepository();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository);

            string jobId = "abc123";

            JobNotification jobNotification = new JobNotification
            {
                JobId = jobId,
                RunningStatus = RunningStatus.InProgress
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .DidNotReceive()
                .GetJobById(Arg.Is(jobId));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobIdIsNotSet_ThenNoActionTakenAndErrorLogged()
        {
            // Arrange
            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Job Notification message has no JobId"));

            await jobRepository
                .DidNotReceive()
                .GetJobById(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobIdNotFound_ThenNoActionTakenAndErrorLogged()
        {
            // Arrange
            string jobId = "abc123";

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();

            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns((Job)null);

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Could not find job with id {JobId}"), Arg.Is(jobId));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasNoParent_ThenNoActionTakenAndMessageLogged()
        {
            // Arrange
            string jobId = "abc123";

            Job job = new Job { Id = jobId, ParentJobId = null };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification
            {
                JobId = jobId,
                RunningStatus = RunningStatus.Completed
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            logger
                .Received(1)
                .Information(Arg.Is("Completed Job {JobId} has no parent"), Arg.Is(jobId));

            await jobRepository
                .DidNotReceive()
                .GetChildJobsForParent(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentOnlyOneChild_ThenParentCompleted()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.RunningStatus == RunningStatus.Completed && j.Completed.HasValue && j.Outcome == "All child jobs completed"));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentOnlyOneChild_ThenCacheUpdated()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);
            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);
            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job });
            var moreRecentJob = new Job {Id = "newer-job-id"};
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(moreRecentJob);
            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            string cacheKey = $"{CacheKeys.LatestJobs}{job.SpecificationId}:{job.JobDefinitionId}";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .SetAsync(cacheKey, Arg.Is<Job>(_ => _.Id == job.Id))
                .Returns(Task.CompletedTask);

            JobManagementService jobManagementService = CreateJobManagementService(
                jobRepository,
                logger: logger,
                cacheProvider: cacheProvider);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j =>
                    j.Id == parentJobId &&
                    j.RunningStatus == RunningStatus.Completed &&
                    j.Completed.HasValue &&
                    j.Outcome == "All child jobs completed"));

            await
                cacheProvider
                .Received(1)
                .SetAsync(cacheKey, Arg.Is<Job>(_ => _.Id == moreRecentJob.Id));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentTwoChildrenOnlyOneCompleted_ThenParentNotCompleted()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, RunningStatus = RunningStatus.InProgress };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .DidNotReceive()
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.RunningStatus == RunningStatus.Completed && j.Completed.HasValue));

            logger
                .Received(1)
                .Information(Arg.Is("Completed Job {JobId} parent {ParentJobId} has in progress child jobs and cannot be completed"), Arg.Is(jobId), Arg.Is(parentJobId));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_WhenMultipleChildJobsCompleted_EnsuresOnlyCompletesParentOnce()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId1 = "child123";
            string jobId2 = "child456";

            Job job1 = new Job { Id = jobId1, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = jobId2, ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(jobId1)
                .Returns(job1);
            jobRepository
                .GetJobById(jobId2)
                .Returns(job2);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob, new Job { Id = parentJobId, RunningStatus = RunningStatus.Completed });

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job1, job2 });

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger, notificationService: notificationService);

            JobNotification jobNotification = new JobNotification { JobId = jobId1, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message1 = new Message(Encoding.UTF8.GetBytes(json));
            message1.UserProperties["jobId"] = jobId1;

            Message message2 = new Message(Encoding.UTF8.GetBytes(json));
            message2.UserProperties["jobId"] = jobId2;

            // Act
            await jobManagementService.ProcessJobNotification(message1);
            await jobManagementService.ProcessJobNotification(message2);

            // Assert
            await jobRepository
                    .Received(1)
                    .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.RunningStatus == RunningStatus.Completed && j.Completed.HasValue));

            await
                notificationService
                    .Received(1)
                    .SendNotification(Arg.Is<JobNotification>(m => m.JobId == parentJobId));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentTwoChildrenOnlyBothCompleted_ThenParentCompleted()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.RunningStatus == RunningStatus.Completed && j.Completed.HasValue && j.Outcome == "All child jobs completed"));

            logger
                .Received(1)
                .Information(Arg.Is("Parent Job {ParentJobId} of Completed Job {JobId} has been completed because all child jobs are now complete"), Arg.Is(job.ParentJobId), Arg.Is(jobId));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentWithMultipleCompletedChildrenWithOneTimedOut_ThenParentCompletedStatusIsTimedOut()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.TimedOut };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.CompletionStatus == CompletionStatus.TimedOut));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentWithMultipleCompletedChildrenWithOneCancelled_ThenParentCompletedStatusIsCancelled()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.Cancelled };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.CompletionStatus == CompletionStatus.Cancelled));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentWithMultipleCompletedChildrenWithOneSuperseded_ThenParentCompletedStatusIsSuperseded()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.Superseded };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.CompletionStatus == CompletionStatus.Superseded));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentWithMultipleCompletedChildrenWithOneFailed_ThenParentCompletedStatusIsFailed()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.Failed };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.CompletionStatus == CompletionStatus.Failed));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentWithMultipleCompletedChildrenWithAllSucceeded_ThenParentCompletedStatusIsSucceeded()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.Succeeded };

            Job job3 = new Job { Id = "child789", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.Succeeded };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.CompletionStatus == CompletionStatus.Succeeded));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentWithMultipleCompletedChildrenWithAllSucceededAndPreCompletionJobsNotRunning_ThenPreCompletionJobQueued()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";
            string preCompletionJobDefinition = "PreCompletionJobDefinitionId";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job job2 = new Job { Id = "child456", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.Succeeded };

            Job job3 = new Job { Id = "child789", ParentJobId = parentJobId, RunningStatus = RunningStatus.Completed, CompletionStatus = CompletionStatus.Succeeded };

            Job preCompletionJob = new Job
            {
                Id = "preCompletionJob",
                ParentJobId = parentJobId,
                JobDefinitionId = preCompletionJobDefinition,
                RunningStatus = RunningStatus.InProgress
            };

            Job parentJob = new Job
            {
                Id = parentJobId,
                RunningStatus = RunningStatus.InProgress,
                JobDefinitionId = jobDefinitionId,
                Trigger = new Trigger { }
            };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job, job2 });

            jobRepository
                .CreateJob(Arg.Any<Job>())
                .Returns(preCompletionJob);

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();

            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(new[] { new JobDefinition { Id = jobDefinitionId, PreCompletionJobs = new[] { preCompletionJobDefinition } },
                    new JobDefinition { Id = preCompletionJobDefinition } });

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository,
                logger: logger,
                jobDefinitionsService: jobDefinitionsService);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.Id == parentJobId && j.RunningStatus == RunningStatus.Completing));
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobHasParentThatIsCompleted_ThenNotificationSent()
        {
            // Arrange
            string parentJobId = "parent123";
            string jobId = "child123";

            Job job = new Job { Id = jobId, ParentJobId = parentJobId, CompletionStatus = CompletionStatus.Succeeded, RunningStatus = RunningStatus.Completed };

            Job parentJob = new Job { Id = parentJobId, RunningStatus = RunningStatus.InProgress };

            ILogger logger = CreateLogger();
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJob);

            jobRepository
                .GetChildJobsForParent(Arg.Is(parentJobId))
                .Returns(new List<Job> { job });

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger, notificationService: notificationService);

            JobNotification jobNotification = new JobNotification { JobId = jobId, RunningStatus = RunningStatus.Completed };

            string json = JsonConvert.SerializeObject(jobNotification);
            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties["jobId"] = jobId;

            // Act
            await jobManagementService.ProcessJobNotification(message);

            // Assert
            await notificationService
                .Received(1)
                .SendNotification(Arg.Is<JobNotification>(n => n.JobId == parentJobId && n.RunningStatus == RunningStatus.Completed));

            logger
                .Received(1)
                .Information(Arg.Is("Parent Job {ParentJobId} of Completed Job {JobId} has been completed because all child jobs are now complete"), Arg.Is(job.ParentJobId), Arg.Is(jobId));
        }
    }
}
