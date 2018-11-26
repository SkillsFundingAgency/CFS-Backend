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
        public void GetJobById_JobIdIsNull_ThrowsArgumentException()
        {
            // Arrange
            IJobService jobService = CreateJobService();

            // Act
            Func<Task<IActionResult>> action = async () => await jobService.GetJobById(null, false);

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
        public void GetJobById_JobIdIsWhitespace_ThrowsArgumentException()
        {
            // Arrange
            IJobService jobService = CreateJobService();

            // Act
            Func<Task<IActionResult>> action = async () => await jobService.GetJobById(" ", false);

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
        public async Task GetJobById_JobIdIsNotFound_Returns404()
        {
            // Arrange
            IJobService jobService = CreateJobService();

            // Act
            IActionResult result = await jobService.GetJobById("unknown", false);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetJobById_JobIdIsFound_Returns200()
        {
            // Arrange
            string jobId = "job123";

            Job job = new Job
            {
                Created = DateTimeOffset.UtcNow.AddHours(-2),
                Id = jobId,
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetChildJobsForParent(Arg.Is(jobId))
                .Returns((IEnumerable<Job>)null);

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult result = await jobService.GetJobById(jobId, false);

            // Assert
            OkObjectResult objResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobViewModel returnedJob = objResult.Value
                .Should()
                .BeOfType<JobViewModel>()
                .Subject;

            returnedJob.Id.Should().Be(job.Id);
        }

        [TestMethod]
        public async Task GetJobById_AndIncludeChildren_JobHasNoChildren_Returns200()
        {
            // Arrange
            string jobId = "job123";

            Job job = new Job
            {
                Created = DateTimeOffset.UtcNow.AddHours(-2),
                Id = jobId,
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetChildJobsForParent(Arg.Is(jobId))
                .Returns(Enumerable.Empty<Job>());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult result = await jobService.GetJobById(jobId, false);

            // Assert
            OkObjectResult objResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobViewModel returnedJob = objResult.Value
                .Should()
                .BeOfType<JobViewModel>()
                .Subject;

            returnedJob
                .ChildJobs
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GetJobById_AndIncludeChildren_JobHasChildren_Returns200WithChildren()
        {
            // Arrange
            string jobId = "job123";
            string childJobId1 = "child123";
            string childJobId2 = "child456";

            Job job = new Job
            {
                Created = DateTimeOffset.UtcNow.AddHours(-2),
                Id = jobId,
            };

            List<Job> childJobs = new List<Job>
            {
                new Job { Id = childJobId1, ParentJobId = jobId },
                new Job { Id = childJobId2, ParentJobId = jobId }
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobById(Arg.Is(jobId))
                .Returns(job);

            jobRepository
                .GetChildJobsForParent(Arg.Is(jobId))
                .Returns(childJobs);

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult result = await jobService.GetJobById(jobId, false);

            // Assert
            OkObjectResult objResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobViewModel returnedJob = objResult.Value
                .Should()
                .BeOfType<JobViewModel>()
                .Subject;

            returnedJob
                .ChildJobs
                .Should()
                .HaveCount(2);

            returnedJob
                .ChildJobs
                .Should()
                .Contain(j => j.Id == childJobId1);

            returnedJob
                .ChildJobs
                .Should()
                .Contain(j => j.Id == childJobId2);
        }
    }
}
