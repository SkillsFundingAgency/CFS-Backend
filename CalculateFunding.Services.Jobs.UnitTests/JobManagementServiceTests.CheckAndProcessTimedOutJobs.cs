using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
{
    public partial class JobManagementServiceTests
    {
        [TestMethod]
        public async Task CheckAndProcessTimedOutJobs_GivenZeroNonCompletedJobsToProcess_LogsAndReturns()
        {
            //Arrange
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetNonCompletedJobs()
                .Returns(Enumerable.Empty<Job>());

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger);

            //Act
            await jobManagementService.CheckAndProcessTimedOutJobs();

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is("Zero non completed jobs to process, finished processing timed out jobs"));
        }

        [TestMethod]
        public void CheckAndProcessTimedOutJobs_GivenNonCompletedJobsFoundButNoDefinitionsReturned_LogsAndThrowsException()
        {
            //Arrange
            IEnumerable<Job> nonCompletedJobs = new[]
            {
                new Job {Id = "job-id-1"},
                new Job {Id = "job-id-2"}
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetNonCompletedJobs()
                .Returns(nonCompletedJobs);

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(Enumerable.Empty<JobDefinition>());

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger, jobDefinitionsService: jobDefinitionsService);

            //Act
            Func<Task> test = async () => await jobManagementService.CheckAndProcessTimedOutJobs();

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be("Failed to retrieve job definitions when processing timed out jobs");

            logger
                .Received(1)
                .Error(Arg.Is("Failed to retrieve job definitions when processing timed out jobs"));

            logger
                .Received(1)
                .Information($"{nonCompletedJobs.Count()} non completed jobs to process");
        }

        [TestMethod]
        public async Task CheckAndProcessTimedOutJobs_GivenNonCompletedJobsFoundButCouldntFiundJobDefinition_LogsError()
        {
            //Arrange
            IEnumerable<Job> nonCompletedJobs = new[]
            {
                new Job {Id = "job-id-1", JobDefinitionId = "job-def-3"},
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetNonCompletedJobs()
                .Returns(nonCompletedJobs);

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = "job-def-1",
                    Timeout = TimeSpan.FromHours(12)
                },
                new JobDefinition
                {
                    Id = "job-def-2",
                    Timeout = TimeSpan.FromHours(12)
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger, jobDefinitionsService: jobDefinitionsService);

            //Act
            await jobManagementService.CheckAndProcessTimedOutJobs();

            //Assert
            logger
                .Received(1)
                .Error($"Failed to find job definition : 'job-def-3' for job id: 'job-id-1'");
        }

        [TestMethod]
        public async Task CheckAndProcessTimedOutJobs_GivenNonCompletedJobsFoundButHasNotTimedOut_DoesNotUpdateJob()
        {
            //Arrange
            IEnumerable<Job> nonCompletedJobs = new[]
            {
                new Job {Id = "job-id-1", JobDefinitionId = "job-def-2", Created = DateTimeOffset.Now.AddHours(-6)},
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetNonCompletedJobs()
                .Returns(nonCompletedJobs);

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = "job-def-1",
                    Timeout = TimeSpan.FromHours(12)
                },
                new JobDefinition
                {
                    Id = "job-def-2",
                    Timeout = TimeSpan.FromHours(12)
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger, jobDefinitionsService: jobDefinitionsService);

            //Act
            await jobManagementService.CheckAndProcessTimedOutJobs();

            //Assert
            await
                jobRepository
                    .DidNotReceive()
                    .UpdateJob(Arg.Any<Job>());
        }

        [TestMethod]
        public async Task CheckAndProcessTimedOutJobs_GivenNonCompletedJobsAndHasTimeOutButFailedToUpdate_LogsError()
        {
            //Arrange
            Job job = new Job {Id = "job-id-1", JobDefinitionId = "job-def-2", Created = DateTimeOffset.Now.AddHours(-13)};
            IEnumerable<Job> nonCompletedJobs = new[]
            {
                job
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetNonCompletedJobs()
                .Returns(nonCompletedJobs);
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(job);
            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.BadRequest);

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = "job-def-1",
                    Timeout = TimeSpan.FromHours(12)
                },
                new JobDefinition
                {
                    Id = "job-def-2",
                    Timeout = TimeSpan.FromHours(12)
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger, jobDefinitionsService: jobDefinitionsService);

            //Act
            await jobManagementService.CheckAndProcessTimedOutJobs();

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is("Failed to update timeout job, Id: 'job-id-1' with status code 400"));
        }

        [TestMethod]
        public async Task CheckAndProcessTimedOutJobs_GivenNonCompletedJobsAndHasTimeOutAndUpdatesSuccesully_SendsNotification()
        {
            //Arrange
            IEnumerable<Job> nonCompletedJobs = new[]
            {
                new Job {Id = "job-id-1", JobDefinitionId = "job-def-2", Created = DateTimeOffset.Now.AddHours(-13)},
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetNonCompletedJobs()
                .Returns(nonCompletedJobs);

            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = "job-def-1",
                    Timeout = TimeSpan.FromHours(12)
                },
                new JobDefinition
                {
                    Id = "job-def-2",
                    Timeout = TimeSpan.FromHours(12)
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            INotificationService notificationService = CreateNotificationsService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger, 
                jobDefinitionsService: jobDefinitionsService, notificationService: notificationService);

            //Act
            await jobManagementService.CheckAndProcessTimedOutJobs();

            //Assert
            await
                jobRepository
                    .Received(1)
                    .UpdateJob(Arg.Is<Job>(
                        m => m.Id == "job-id-1" &&
                             m.CompletionStatus == CompletionStatus.TimedOut &&
                             m.RunningStatus == RunningStatus.Completed
                        ));

            await
                notificationService
                    .Received(1)
                    .SendNotification(Arg.Is<JobNotification>(
                            m => m.JobId == "job-id-1" &&
                            m.CompletionStatus == CompletionStatus.TimedOut &&
                            m.RunningStatus == RunningStatus.Completed
                        ));
        }

        [TestMethod]
        public async Task CheckAndProcessTimedOutJobs_GivenTwoNonCompletedJobsAndHasTimeOutButOnlyoneUpdatesSuccessfully_SendsOneNotification()
        {
            //Arrange
            IEnumerable<Job> nonCompletedJobs = new[]
            {
                new Job {Id = "job-id-1", JobDefinitionId = "job-def-2", Created = DateTimeOffset.Now.AddHours(-13)},
                new Job {Id = "job-id-2", JobDefinitionId = "job-def-1", Created = DateTimeOffset.Now.AddHours(-13)},
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetNonCompletedJobs()
                .Returns(nonCompletedJobs);
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Any<string>(), Arg.Any<string>())
                .Returns(nonCompletedJobs.First());
            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK, HttpStatusCode.BadRequest);

            IEnumerable<JobDefinition> jobDefinitions = new[]
            {
                new JobDefinition
                {
                    Id = "job-def-1",
                    Timeout = TimeSpan.FromHours(12)
                },
                new JobDefinition
                {
                    Id = "job-def-2",
                    Timeout = TimeSpan.FromHours(12)
                }
            };

            IJobDefinitionsService jobDefinitionsService = CreateJobDefinitionsService();
            jobDefinitionsService
                .GetAllJobDefinitions()
                .Returns(jobDefinitions);

            INotificationService notificationService = CreateNotificationsService();

            ILogger logger = CreateLogger();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, logger: logger,
                jobDefinitionsService: jobDefinitionsService, notificationService: notificationService);

            //Act
            await jobManagementService.CheckAndProcessTimedOutJobs();

            //Assert
            await
                jobRepository
                    .Received(1)
                    .UpdateJob(Arg.Is<Job>(
                        m => m.Id == "job-id-1" &&
                             m.CompletionStatus == CompletionStatus.TimedOut &&
                             m.RunningStatus == RunningStatus.Completed
                        ));

            await
                notificationService
                    .Received(1)
                    .SendNotification(Arg.Is<JobNotification>(
                            m => m.JobId == "job-id-1" &&
                            m.CompletionStatus == CompletionStatus.TimedOut &&
                            m.RunningStatus == RunningStatus.Completed
                        ));

            logger
                .Received(1)
                .Error(Arg.Is("Failed to update timeout job, Id: 'job-id-2' with status code 400"));
        }
    }
}
