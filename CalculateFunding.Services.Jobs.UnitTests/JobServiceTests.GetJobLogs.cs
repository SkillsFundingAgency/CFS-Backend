using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Jobs
{
    public partial class JobServiceTests
    {
        [TestMethod]
        public void GetJobLogs_JobIdIsNull_ThrowsArgumentException()
        {
            // Arrange
            IJobService jobService = CreateJobService();

            // Act
            Func<Task> action = async () => await jobService.GetJobLogs(null);

            // Assert
            action
                .Should()
                .Throw<ArgumentException>()
                .And
                .ParamName
                .Should()
                .Be("jobId");
        }

        [TestMethod]
        public void GetJobLogs_JobIdIsWhitespace_ThrowsArgumentException()
        {
            // Arrange
            IJobService jobService = CreateJobService();

            // Act
            Func<Task> action = async () => await jobService.GetJobLogs("  ");

            // Assert
            action
                .Should()
                .Throw<ArgumentException>()
                .And
                .ParamName
                .Should()
                .Be("jobId");
        }

        [TestMethod]
        public async Task GetJobLogs_JobIdNotFound_Returns404()
        {
            // Arrange
            IJobService jobService = CreateJobService();

            // Act
            IActionResult result = await jobService.GetJobLogs("unknown");

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetJobLogs_JobIdFoundAndNoLogs_Returns200WithEmptyCollection()
        {
            // Arrange
            string jobId = "job123";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(new Job { Id = jobId });

            jobRepository
                .GetJobLogsByJobId(Arg.Is(jobId))
                .Returns(Enumerable.Empty<JobLog>());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult result = await jobService.GetJobLogs(jobId);

            // Assert
            OkObjectResult objResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IEnumerable<JobLog> jobLogs = objResult.Value
                .Should()
                .BeAssignableTo<IEnumerable<JobLog>>()
                .Subject;

            jobLogs
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GetJobLogs_JobIdFoundWithLogs_Returns200WithLogs()
        {
            // Arrange
            string jobId = "job123";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(new Job { Id = jobId });

            List<JobLog> logs = new List<JobLog>
            {
                new JobLog { Id = "log1", JobId = jobId }
            };

            jobRepository
                .GetJobLogsByJobId(Arg.Is(jobId))
                .Returns(logs);

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult result = await jobService.GetJobLogs(jobId);

            // Assert
            OkObjectResult objResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IEnumerable<JobLog> jobLogs = objResult.Value
                .Should()
                .BeAssignableTo<IEnumerable<JobLog>>()
                .Subject;

            jobLogs
                .Should()
                .HaveCount(1);

            jobLogs
                .Should()
                .Contain(l => l.Id == "log1");
        }
    }
}
