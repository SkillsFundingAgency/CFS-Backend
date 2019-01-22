using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Jobs.Services
{
    public partial class JobManagementServiceTests
    {
        [TestMethod]
        public async Task SupersedeJob_WhenSameJobId_ThenJobNotSuperseded()
        {
            // Arrange
            string jobId = "job-id-1";
            Job runningJob = new Job
            {
                Id = jobId
            };

            Job replacementJob = new Job
            {
                Id = jobId
            };

            IJobRepository jobRepository = CreateJobRepository();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository);

            // Act
            await jobManagementService.SupersedeJob(runningJob, replacementJob);

            // Assert
            await jobRepository
                .DidNotReceive()
                .UpdateJob(Arg.Any<Job>());
        }

        [TestMethod]
        public async Task SupersedeJob_WhenNotSameJobIdButSameParentJobId_ThenJobNotSuperseded()
        {
            // Arrange
            string jobId = "job-id-1";
            string supersedeJobId = "job-id-2";

            Job runningJob = new Job
            {
                Id = jobId,
                ParentJobId = "parent-1",
            };

            Job replacementJob = new Job
            {
                Id = supersedeJobId,
                ParentJobId = "parent-1",
            };

            IJobRepository jobRepository = CreateJobRepository();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository);

            // Act
            await jobManagementService.SupersedeJob(runningJob, replacementJob);

            // Assert
            await jobRepository
                .DidNotReceive()
                .UpdateJob(Arg.Any<Job>());
        }

        [TestMethod]
        public async Task SupersedeJob_WhenNotSameJobIdAndNotSameParentJobId_ThenJobSuperseded()
        {
            // Arrange
            string jobId = "job-id-1";
            string supersedeJobId = "job-id-2";

            Job runningJob = new Job
            {
                Id = jobId,
                ParentJobId = "parent-1",
            };

            Job replacementJob = new Job
            {
                Id = supersedeJobId,
                ParentJobId = "parent-2",
            };

            IJobRepository jobRepository = CreateJobRepository();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository);

            // Act
            await jobManagementService.SupersedeJob(runningJob, replacementJob);

            // Assert
            await jobRepository
                 .Received(1)
                 .UpdateJob(Arg.Is<Job>(j => j.CompletionStatus == CompletionStatus.Superseded && j.Completed.HasValue && j.RunningStatus == RunningStatus.Completed && j.SupersededByJobId == supersedeJobId));
        }

        [TestMethod]
        public async Task SupersedeJob_WhenDifferentJobId_ThenJobSuperseded()
        {
            // Arrange
            string jobId = "job-id-1";
            string supersedeJobId = "job-id-2";
            Job runningJob = new Job
            {
                Id = jobId,
            };

            Job replacementJob = new Job
            {
                Id = supersedeJobId
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository);

            // Act
            await jobManagementService.SupersedeJob(runningJob, replacementJob);

            // Assert
            await jobRepository
                .Received(1)
                .UpdateJob(Arg.Is<Job>(j => j.CompletionStatus == CompletionStatus.Superseded && j.Completed.HasValue && j.RunningStatus == RunningStatus.Completed && j.SupersededByJobId == supersedeJobId));
        }

        [TestMethod]
        public async Task SupersedeJob_WhenJobSuperseded_ThenNotificationSent()
        {
            // Arrange
            string jobId = "job-id-1";
            string supersedeJobId = "job-id-2";
            Job runningJob = new Job
            {
                Id = jobId
            };

            Job replacementJob = new Job
            {
                Id = supersedeJobId
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .UpdateJob(Arg.Any<Job>())
                .Returns(HttpStatusCode.OK);

            INotificationService notificationService = CreateNotificationsService();

            JobManagementService jobManagementService = CreateJobManagementService(jobRepository, notificationService);

            // Act
            await jobManagementService.SupersedeJob(runningJob, replacementJob);

            // Assert
            await notificationService
                .Received(1)
                .SendNotification(Arg.Is<JobNotification>(n => n.JobId == jobId));
        }
    }
}
