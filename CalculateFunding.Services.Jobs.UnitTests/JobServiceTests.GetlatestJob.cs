using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
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
        public void GetLatestJobs_WhenSpecificationIdIsNull_ThrowArgumentNullException()
        {
            // Arrange
            string specificationId = null;

            IJobService service = CreateJobService();

            // Act
            Func<Task> action = () => service.GetLatestJobs(specificationId, null);

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
        public async Task GetLatestJobs_WhenNoJobsForSpecification_ReturnOKResultWithNull()
        {
            // Arrange
            string specificationId = "spec123";
            string jobDefinitionId = "jobType1";

            IJobService service = CreateJobService();

            // Act
            IActionResult result = await service.GetLatestJobs(
                specificationId, 
                new[] { jobDefinitionId });

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobSummary> latestJobs = okResult.Value as IDictionary<string, JobSummary>;

            latestJobs[jobDefinitionId]
                .JobId
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task GetCreatedJobsWithinTimeFrame_WhenJobsExistWithinTimeFrame_JobsReturned()
        {
            // Arrange
            string specificationId = "spec123";

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset hourAgo = now.AddHours(-1);

            string outcome = "outcome-1";
            List<Outcome> outcomes = new List<Outcome>
            {
                new Outcome
                {
                    Description = outcome
                }
            };

            string dateFromString = hourAgo.ToString("yyyy-MM-ddTHH:mm:ss.sss+00:00");
            string dateToString = now.ToString("yyyy-MM-ddTHH:mm:ss.sss+00:00");

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
                        Trigger = new Trigger
                        {
                            EntityId = "calc1",
                            EntityType = "Calculation",
                            Message = "Calc run started"
                        },
                        Outcomes = outcomes,
                        Outcome = outcome,
                        OutcomeType = OutcomeType.Succeeded
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
                .ContainSingle(_ =>
                    _.JobId == "job1" &&
                    _.Outcomes.SequenceEqual(outcomes) &&
                    _.Outcome == outcome &&
                    _.OutcomeType == OutcomeType.Succeeded);
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
        public async Task GetLatestJobs_WhenOnlyOneJobForSpecification_ReturnJob()
        {
            // Arrange
            string specificationId = "spec123";
            string jobDefinitionId = "jobType1";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Is(specificationId), Arg.Is<string>(_ => _.Contains(jobDefinitionId)))
                .Returns(new Job()
                {
                    Created = DateTimeOffset.UtcNow.AddHours(-1),
                    Id = "job1",
                    InvokerUserDisplayName = "test",
                    InvokerUserId = "test1",
                    JobDefinitionId = jobDefinitionId,
                    LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                    RunningStatus = RunningStatus.InProgress,
                    SpecificationId = specificationId,
                    Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                });

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJobs(
                specificationId,
                new[] { jobDefinitionId });

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobSummary> latestJobs = okResult.Value
                .Should()
                .BeAssignableTo<IDictionary<string, JobSummary>>()
                .Subject;

            latestJobs[jobDefinitionId].JobId.Should().Be("job1");
        }

        [TestMethod]
        public async Task GetLatestJobs_WhenOnlyOneJobForSpecification_AndExistsInCache_ReturnJobFromCache()
        {
            // Arrange
            string specificationId = "spec123";
            string jobDefinitionId = "jobType1";

            Job job = new Job
            {
                Created = DateTimeOffset.UtcNow.AddHours(-1),
                Id = "job1",
                InvokerUserDisplayName = "test",
                InvokerUserId = "test1",
                JobDefinitionId = jobDefinitionId,
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                RunningStatus = RunningStatus.InProgress,
                SpecificationId = specificationId,
                Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();

            string cacheKey = $"{CacheKeys.LatestJobs}{specificationId}:{jobDefinitionId}";
            cacheProvider
                .GetAsync<Job>(cacheKey)
                .Returns(job);

            IJobService service = CreateJobService(
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetLatestJobs(
                specificationId,
                new[] { jobDefinitionId });

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobSummary> latestJobs = okResult.Value
                .Should()
                .BeAssignableTo<IDictionary<string, JobSummary>>()
                .Subject;

            latestJobs[jobDefinitionId].JobId.Should().Be("job1");
        }

        [TestMethod]
        public async Task GetLatestJobs_WhenTwoJobsForSpecification_AndOneExistsInCache_AndOneDoesNotExistsInCache_ReturnLatestJobs()
        {
            // Arrange
            string specificationId = "spec123";
            string jobType = "jobType1";
            string jobTypeTwo = "jobType2";

            Job job = new Job
            {
                Created = DateTimeOffset.UtcNow.AddHours(-1),
                Id = "job1",
                InvokerUserDisplayName = "test",
                InvokerUserId = "test1",
                JobDefinitionId = jobType,
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                RunningStatus = RunningStatus.InProgress,
                SpecificationId = specificationId,
                Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
            };

            Job jobTwo = new Job
            {
                Created = DateTimeOffset.UtcNow.AddMinutes(-1),
                Id = "job2",
                InvokerUserDisplayName = "test",
                InvokerUserId = "test1",
                JobDefinitionId = jobTypeTwo,
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                RunningStatus = RunningStatus.InProgress,
                SpecificationId = specificationId,
                Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();

            string cacheKey = $"{CacheKeys.LatestJobs}{specificationId}:{jobType}";
            cacheProvider
                .GetAsync<Job>(cacheKey)
                .Returns(job);

            string cacheKeyTwo = $"{CacheKeys.LatestJobs}{specificationId}:{jobTypeTwo}";
            cacheProvider
                .SetAsync(jobTypeTwo, Arg.Is<Job>(_ => _.JobDefinitionId == jobTypeTwo))
                .Returns(Task.CompletedTask);


            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(specificationId, Arg.Is<string>(_ => _ == jobTypeTwo))
                .Returns(jobTwo);

            IJobService service = CreateJobService(
                jobRepository: jobRepository,
                cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetLatestJobs(
                specificationId,
                new[] { jobType, jobTypeTwo });

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobSummary> latestJobs = okResult.Value
                 .Should()
                .BeAssignableTo<IDictionary<string, JobSummary>>()
                .Subject;

            latestJobs.Count().Should().Be(2);
            latestJobs.Values.Should().Contain(x => x.JobId == "job1");
            latestJobs.Values.Should().Contain(x => x.JobId == "job2");

            await cacheProvider
                .Received(1)
                .SetAsync(cacheKeyTwo, Arg.Is<Job>(_ => _.JobDefinitionId == jobTypeTwo));
        }

        [TestMethod]
        public async Task GetLatestJobs_WhenSingleJobTypeGiven_ReturnLatestJobOfType()
        {
            // Arrange
            string specificationId = "spec123";

            string jobType = "jobType1";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Is(specificationId), Arg.Is<string>(_ => _ == "jobType1"))
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
            IActionResult result = await service.GetLatestJobs(
                specificationId,
                new[] { jobType });

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobSummary> latestJobs = okResult.Value
                .Should()
                .BeAssignableTo<IDictionary<string, JobSummary>>()
                .Subject;

            latestJobs.Values.Should().Contain(x => x.JobId == "job1");
        }

        [TestMethod]
        public async Task GetLatestJobs_WhenSingleJobTypeGivenAndNoJobsOfType_ReturnOKResultWithNull()
        {
            // Arrange
            string specificationId = "spec123";
            string jobDefinitionId = "jobType2";


            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Is(specificationId), Arg.Is<string>(_ => _ == jobDefinitionId))
                .Returns((Job)null);

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJobs(
                specificationId,
                new[] { jobDefinitionId });

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobSummary> latestJobs = okResult.Value as IDictionary<string, JobSummary>;

            latestJobs[jobDefinitionId]
                .JobId
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task GetLatestJobs_WhenNoJobTypes_ReturnBadRequest()
        {
            // Arrange
            string specificationId = "spec123";

            IJobRepository jobRepository = CreateJobRepository();
            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJobs(
                specificationId,
                Array.Empty<string>());

            // Assert
            BadRequestObjectResult badRequestObjectResult = result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Subject;

            badRequestObjectResult.Value.Should().Be("JobTypes must be provided to get latest jobs.");
        }

        [TestMethod]
        public async Task GetLatestJobs_WhenMultipleJobTypesGiven_ReturnLatestJobsOfGivenTypes()
        {
            // Arrange
            string specificationId = "spec123";

            string outcome = "outcome-1";
            List<Outcome> outcomes = new List<Outcome>
            {
                new Outcome
                {
                    Description = outcome
                }
            };

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Is(specificationId), Arg.Is<string>(_ => _ == "jobType1"))
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
                        Trigger = new Trigger
                        {
                            EntityId = "calc1",
                            EntityType = "Calculation",
                            Message = "Calc run started"
                        },
                        Outcome = outcome,
                        Outcomes = outcomes,
                        ItemCount = 1,
                        OutcomeType = OutcomeType.Succeeded
                    });

            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(Arg.Is(specificationId), Arg.Is<string>(_ => _ == "jobType2"))
                .Returns(
                    new Job
                    {
                        Created = DateTimeOffset.UtcNow.AddHours(-1),
                        Id = "job10",
                        InvokerUserDisplayName = "test",
                        InvokerUserId = "test1",
                        JobDefinitionId = "jobType2",
                        LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc2", EntityType = "Calculation", Message = "Calc run started" }
                    });

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = await service.GetLatestJobs(
                specificationId,
                new[] { "jobType1", "jobType2" });

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobSummary> latestJobs = okResult.Value
                .Should()
                .BeAssignableTo<IDictionary<string, JobSummary>>()
                .Subject;

            latestJobs.Values.Should().Contain(x =>
                x.JobId == "job2" &&
                x.Outcome == outcome &&
                x.Outcomes.SequenceEqual(outcomes) &&
                x.ItemCount == 1 &&
                x.OutcomeType == OutcomeType.Succeeded);

            latestJobs.Values.Should().Contain(x => x.JobId == "job10");
        }
    }
}
