using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public void GetLatestJob_WhenSpecificationIdIsNull_ThrowArgumentNullException()
        {
            // Arrange
            string specificationId = null;

            IJobService service = CreateJobService();

            // Act
            Func<Task> action = () => service.GetLatestJob(specificationId, null);

            // Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task GetLatestJob_WhenNoJobsForSpecification_ReturnNoContent()
        {
            // Arrange
            string specificationId = "spec123";

            IJobService service = CreateJobService();

            // Act
            IActionResult result = await service.GetLatestJob(specificationId, null);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task GetCreatedJobsWithinTimeFrame_WhenJobsExistWithinTimeFrame_JobsReturned()
        {
            // Arrange
            string specificationId = "spec123";

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset hourAgo = now.AddHours(-1);

            string dateFromString = hourAgo.ToString("yyyy-MM-ddThh:mm:ss.sssZ");
            string dateToString = now.ToString("yyyy-MM-ddThh:mm:ss.sssZ");

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetRunningJobsWithinTimeFrame(Arg.Is<string>(_ => _ == dateFromString), Arg.Is<string>(_ => _ == dateToString))
                .Returns(
                    new[] {new Job
                    {
                        Created = DateTimeOffset.UtcNow.AddHours(-1),
                        Id = "job1",
                        InvokerUserDisplayName = "test",
                        InvokerUserId = "test1",
                        JobDefinitionId = "jobType1",
                        LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    } });

            IJobService service = CreateJobService(jobRepository: jobRepository);

            // Act
            IActionResult result = await service.GetCreatedJobsWithinTimeFrame(hourAgo, now);

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IEnumerable<JobSummary> jobs = okResult.Value as IEnumerable<JobSummary>;

            jobs
                .Should()
                .ContainSingle(_ => _.JobId == "job1");
        }

        [TestMethod]
        public async Task GetCreatedJobsWithinTimeFrame_WhenDateFromGreaterThanDateTo_ReturnBadRequest()
        {
            // Arrange
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset hourAgo = now.AddHours(-1);

            IJobService service = CreateJobService();

            // Act
            IActionResult result = await service.GetCreatedJobsWithinTimeFrame(now, hourAgo);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObjectResult = result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Subject;

            badRequestObjectResult
                .Value
                .Should()
                .Be($"dateTimeTo cannot be before dateTimeFrom.");
        }

        [TestMethod]
        public async Task GetCreatedJobsWithinTimeFrame_WhenDateFromGreaterThanNow_ReturnBadRequest()
        {
            // Arrange
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset hourAgo = now.AddHours(1);

            IJobService service = CreateJobService();

            // Act
            IActionResult result = await service.GetCreatedJobsWithinTimeFrame(hourAgo, now);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObjectResult = result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Subject;

            badRequestObjectResult
                .Value
                .Should()
                .Be($"dateTimeFrom cannot be in the future");
        }

        [TestMethod]
        public async Task GetLatestJob_WhenOnlyOneJobForSpecification_ReturnJob()
        {
            // Arrange
            string specificationId = "spec123";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationId(Arg.Is(specificationId))
                .Returns(
                    new Job
                    {
                        Created = DateTimeOffset.UtcNow.AddHours(-1),
                        Id = "job1",
                        InvokerUserDisplayName = "test",
                        InvokerUserId = "test1",
                        JobDefinitionId = "jobType1",
                        LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    });

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJob(specificationId, null);

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobSummary summary = okResult.Value
                .Should()
                .BeOfType<JobSummary>()
                .Subject;

            summary.JobId.Should().Be("job1");
        }

        [TestMethod]
        public async Task GetLatestJob_WhenSingleJobTypeGiven_ReturnLatestJobOfType()
        {
            // Arrange
            string specificationId = "spec123";

            string jobType = "jobType1";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationId(Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(m => m.First() == "jobType1"))
                .Returns(
                    new Job
                    {
                        Created = DateTimeOffset.UtcNow.AddHours(-1),
                        Id = "job1",
                        InvokerUserDisplayName = "test",
                        InvokerUserId = "test1",
                        JobDefinitionId = "jobType1",
                        LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    });

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJob(specificationId, jobType);

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobSummary summary = okResult.Value
                .Should()
                .BeOfType<JobSummary>()
                .Subject;

            summary.JobId.Should().Be("job1");
        }

        [TestMethod]
        public async Task GetLatestJob_WhenSingleJobTypeGivenAndNoJobsOfType_ReturnNotFoundResult()
        {
            // Arrange
            string specificationId = "spec123";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(new List<Job>
                {
                    new Job
                    {
                        Created = DateTimeOffset.UtcNow.AddHours(-1),
                        Id = "job1",
                        InvokerUserDisplayName = "test",
                        InvokerUserId = "test1",
                        JobDefinitionId = "jobType1",
                        LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    },
                    new Job
                    {
                        Created = DateTimeOffset.UtcNow.AddHours(-1),
                        Id = "job2",
                        InvokerUserDisplayName = "test",
                        InvokerUserId = "test1",
                        JobDefinitionId = "jobType1",
                        LastUpdated = DateTimeOffset.UtcNow.AddMinutes(-20),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    }
                }.AsQueryable());

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJob(specificationId, "jobType2");

            // Assert
            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task GetLatestJob_WhenMultipleJobTypesGiven_ReturnLatestJobOfGivenTypes()
        {
            // Arrange
            string specificationId = "spec123";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationId(Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(m => m.ElementAt(0) == "jobType1" && m.ElementAt(1) == "jobType2"))
                .Returns(
                    new Job
                    {
                        Created = DateTimeOffset.UtcNow.AddHours(-1),
                        Id = "job2",
                        InvokerUserDisplayName = "test",
                        InvokerUserId = "test1",
                        JobDefinitionId = "jobType1",
                        LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    });

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJob(specificationId, "jobType1,jobType2");

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobSummary summary = okResult.Value
                .Should()
                .BeOfType<JobSummary>()
                .Subject;

            summary.JobId.Should().Be("job2");
        }
    }
}
