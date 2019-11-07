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
        private List<Job> testData = new List<Job>
        {
            new Job { Created = DateTimeOffset.Parse("2018-12-15T10:34:00.000Z"), CompletionStatus = CompletionStatus.Succeeded, Id = "job1", JobDefinitionId = "jobType1", Outcome = "Job Completed Successfully", RunningStatus = RunningStatus.Completed, SpecificationId = "spec123", Trigger = new Trigger { EntityId = "entity1" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-15T13:13:00.000Z"), CompletionStatus = CompletionStatus.Succeeded, Id = "job2", JobDefinitionId = "jobType1", Outcome = "Job Completed Successfully", RunningStatus = RunningStatus.Completed, SpecificationId = "spec456", Trigger = new Trigger { EntityId = "entity2" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-16T09:34:00.000Z"), CompletionStatus = CompletionStatus.Failed, Id = "job3", JobDefinitionId = "jobType2", Outcome = "Job Completed with Error", RunningStatus = RunningStatus.Completed, SpecificationId = "spec789", Trigger = new Trigger { EntityId = "entity3" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-16T10:12:00.000Z"), CompletionStatus = CompletionStatus.Cancelled, Id = "job4", JobDefinitionId = "jobType3", Outcome = "Job was Cancelled", RunningStatus = RunningStatus.Completed, SpecificationId = "spec100", Trigger = new Trigger { EntityId = "entity4" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-16T10:32:00.000Z"), CompletionStatus = CompletionStatus.Superseded, Id = "job5", JobDefinitionId = "jobType4", Outcome = "Job was Superseded", RunningStatus = RunningStatus.Completed, SpecificationId = "spec313", Trigger = new Trigger { EntityId = "entity5" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-16T15:45:00.000Z"), CompletionStatus = CompletionStatus.Failed, Id = "job6", JobDefinitionId = "jobType1", Outcome = "Job Completed with Error", RunningStatus = RunningStatus.Completed, SpecificationId = "spec345", Trigger = new Trigger { EntityId = "entity6" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-17T10:58:00.000Z"), CompletionStatus = null, Id = "job7", JobDefinitionId = "jobType2", Outcome = "", RunningStatus = RunningStatus.Queued, SpecificationId = "spec056", Trigger = new Trigger { EntityId = "entity7" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-17T10:59:00.000Z"), CompletionStatus = CompletionStatus.Superseded, Id = "job8", JobDefinitionId = "jobType6", Outcome = "Job was Superseded", RunningStatus = RunningStatus.Completed, SpecificationId = "spec589", Trigger = new Trigger { EntityId = "entity8" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-17T11:25:00.000Z"), CompletionStatus = CompletionStatus.TimedOut, Id = "job9", JobDefinitionId = "jobType2", Outcome = "Job Timed Out", RunningStatus = RunningStatus.Completed, SpecificationId = "spec901", Trigger = new Trigger { EntityId = "entity9" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-18T10:10:00.000Z"), CompletionStatus = CompletionStatus.TimedOut, Id = "job10", JobDefinitionId = "jobType3", Outcome = "Job Timed Out", RunningStatus = RunningStatus.Completed, SpecificationId = "spec711", Trigger = new Trigger { EntityId = "entity10" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-18T16:59:00.000Z"), CompletionStatus = CompletionStatus.Cancelled, Id = "job11", JobDefinitionId = "jobType2", Outcome = "Job was Cancelled", RunningStatus = RunningStatus.Completed, SpecificationId = "spec321", Trigger = new Trigger { EntityId = "entity11" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-19T08:12:00.000Z"), CompletionStatus = null, Id = "job12", JobDefinitionId = "jobType2", Outcome = "", RunningStatus = RunningStatus.InProgress, SpecificationId = "spec345", Trigger = new Trigger { EntityId = "entity6" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-20T12:34:00.000Z"), CompletionStatus = null, Id = "job13", JobDefinitionId = "jobType6", Outcome = "Job Completed Successfully", RunningStatus = RunningStatus.InProgress, SpecificationId = "spec123", Trigger = new Trigger { EntityId = "entity1" }},
            new Job { Created = DateTimeOffset.Parse("2018-12-20T15:15:00.000Z"), CompletionStatus = CompletionStatus.Succeeded, Id = "job14", ParentJobId = "job1", JobDefinitionId = "jobType3", Outcome = "Job Completed Successfully", RunningStatus = RunningStatus.Completed, SpecificationId = "spec123", Trigger = new Trigger { EntityId = "entity99", Message = "Triggered by parent" }, InvokerUserDisplayName = "Test User", InvokerUserId = "testuser" }
        };

        [TestMethod]
        public async Task GetJobs_WithNoFilter_ReturnsAllResults()
        {
            // Arrange
            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, null, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(testData.Count);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .HaveCount(testData.Count);
        }

        [TestMethod]
        public async Task GetJobs_WithSpecificationFilter_ReturnsResultsMatchingFilter()
        {
            // Arrange
            string specId = "spec123";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(specId, null, null, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(3);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .OnlyContain(s => s.SpecificationId == specId);

            summaries
                .Should()
                .HaveCount(3);
        }

        [TestMethod]
        public async Task GetJobs_WithUnknownSpecificationFilter_ReturnsZeroResults()
        {
            // Arrange
            string specId = "unknown";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(specId, null, null, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(0);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(0);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GetJobs_WithJobTypeFilter_ReturnsResultsMatchingFilter()
        {
            // Arrange
            string jobType = "jobType1";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, jobType, null, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(3);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .OnlyContain(s => s.JobType == jobType);

            summaries
                .Should()
                .HaveCount(3);
        }

        [TestMethod]
        public async Task GetJobs_WithUnknownJobTypeFilter_ReturnsZeroResults()
        {
            // Arrange
            string jobType = "unknown";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, jobType, null, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(0);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(0);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GetJobs_WithEntityIdFilter_ReturnsResultsMatchingFilter()
        {
            // Arrange
            string entityId = "entity1";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, entityId, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(2);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .OnlyContain(s => s.EntityId == entityId);

            summaries
                .Should()
                .HaveCount(2);
        }

        [TestMethod]
        public async Task GetJobs_WithUnknownEntityIdFilter_ReturnsZeroResults()
        {
            // Arrange
            string entityId = "unknown";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, entityId, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(0);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(0);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GetJobs_WithRunningStatusFilter_ReturnsResultsMatchingFilter()
        {
            // Arrange
            RunningStatus runningStatus = RunningStatus.InProgress;

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, null, runningStatus, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(2);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .OnlyContain(s => s.RunningStatus == runningStatus);

            summaries
                .Should()
                .HaveCount(2);
        }

        [TestMethod]
        public async Task GetJobs_WithCompletionStatusFilter_ReturnsResultsMatchingFilter()
        {
            // Arrange
            CompletionStatus completionStatus = CompletionStatus.Succeeded;

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, null, null, completionStatus, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(3);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .OnlyContain(s => s.CompletionStatus == completionStatus);

            summaries
                .Should()
                .HaveCount(3);
        }

        [TestMethod]
        public async Task GetJobs_WithExcludeChildJobsOption_ReturnsOnlyParentJobs()
        {
            // Arrange
            string specId = "spec123";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(specId, null, null, null, null, true, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(2);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .OnlyContain(s => s.SpecificationId == specId && s.ParentJobId == null);

            summaries
                .Should()
                .HaveCount(2);
        }

        [TestMethod]
        public async Task GetJobs_WhenAllFiltersPresent_ReturnsResultsMatchingFilter()
        {
            // Arrange
            string specificationId = "spec123";
            string jobType = "jobType1";
            string entityId = "entity1";
            RunningStatus runningStatus = RunningStatus.Completed;
            CompletionStatus completionStatus = CompletionStatus.Succeeded;

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(specificationId, jobType, entityId, runningStatus, completionStatus, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(1);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(1);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .OnlyContain(s =>
                    s.SpecificationId == specificationId
                    && s.JobType == jobType
                    && s.EntityId == entityId
                    && s.RunningStatus == runningStatus
                    && s.CompletionStatus == completionStatus);

            summaries
                .Should()
                .HaveCount(1);
        }

        [TestMethod]
        public async Task GetJobs_WhenFirstPageRequested_AndMoreThanOnePage_ReturnsFirstPageOfResults()
        {
            // Arrange
            List<Job> largeTestData = new List<Job>();

            for (int i = 0; i < 100; i++)
            {
                largeTestData.Add(new Job { CompletionStatus = CompletionStatus.Succeeded, Id = "job" + i, JobDefinitionId = "jobType1", Outcome = "Job Completed Successfully", RunningStatus = RunningStatus.Completed, SpecificationId = "spec123", Trigger = new Trigger { EntityId = "entity1" } });
            }

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(largeTestData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, null, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(largeTestData.Count);
            response.CurrentPage.Should().Be(1);
            response.TotalPages.Should().Be(2);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .HaveCount(50);

            summaries
                .First()
                .JobId
                .Should()
                .Be("job0", "first job should be the first job");

            summaries
                .Last()
                .JobId
                .Should()
                .Be("job49", "last job should be the 50th job");
        }

        [TestMethod]
        public async Task GetJobs_WhenSecondPageRequested_ReturnsSecondPageOfResults()
        {
            // Arrange
            List<Job> largeTestData = new List<Job>();

            for (int i = 0; i < 100; i++)
            {
                largeTestData.Add(new Job { CompletionStatus = CompletionStatus.Succeeded, Id = "job" + i, JobDefinitionId = "jobType1", Outcome = "Job Completed Successfully", RunningStatus = RunningStatus.Completed, SpecificationId = "spec123", Trigger = new Trigger { EntityId = "entity1" } });
            }

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(largeTestData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, null, null, null, false, 2);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(largeTestData.Count);
            response.CurrentPage.Should().Be(2);
            response.TotalPages.Should().Be(2);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .HaveCount(50);

            summaries
                .First()
                .JobId
                .Should()
                .Be("job50", "first job should be the 50th job");

            summaries
                .Last()
                .JobId
                .Should()
                .Be("job99", "last job should be the 100th job");
        }

        [TestMethod]
        public async Task GetJobs_WhenNonExistantPageRequested_ReturnsEmptyList()
        {
            // Arrange
            List<Job> largeTestData = new List<Job>();

            for (int i = 0; i < 100; i++)
            {
                largeTestData.Add(new Job { CompletionStatus = CompletionStatus.Succeeded, Id = "job" + i, JobDefinitionId = "jobType1", Outcome = "Job Completed Successfully", RunningStatus = RunningStatus.Completed, SpecificationId = "spec123", Trigger = new Trigger { EntityId = "entity1" } });
            }

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(largeTestData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, null, null, null, false, 3);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            response.TotalItems.Should().Be(largeTestData.Count);
            response.CurrentPage.Should().Be(3);
            response.TotalPages.Should().Be(2);

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task GetJobs_WhenInvalidPageNumberRequested_Returns400()
        {
            // Arrange
            IJobService jobService = CreateJobService();

            // Act
            IActionResult result = await jobService.GetJobs(null, null, null, null, null, false, 0);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Invalid page number, pages start from 1");
        }

        [TestMethod]
        public async Task GetJobs_CheckFieldsMapped()
        {
            // Arrange
            string entityId = "entity99";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetJobs()
                .Returns(testData.AsQueryable());

            IJobService jobService = CreateJobService(jobRepository);

            // Act
            IActionResult results = await jobService.GetJobs(null, null, entityId, null, null, false, 1);

            // Assert
            OkObjectResult objResult = results
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobQueryResponseModel response = objResult.Value
                .Should()
                .BeOfType<JobQueryResponseModel>()
                .Subject;

            IEnumerable<JobSummary> summaries = response.Results;

            summaries
                .Should()
                .HaveCount(1);

            JobSummary item = summaries.First();

            item.CompletionStatus.Should().Be(CompletionStatus.Succeeded);
            item.EntityId.Should().Be(entityId);
            item.InvokerUserDisplayName.Should().Be("Test User");
            item.InvokerUserId.Should().Be("testuser");
            item.JobId.Should().Be("job14");
            item.JobType.Should().Be("jobType3");
            item.ParentJobId.Should().Be("job1");
            item.RunningStatus.Should().Be(RunningStatus.Completed);
            item.SpecificationId.Should().Be("spec123");
            item.Created.Should().Be(DateTimeOffset.Parse("2018-12-20T15:15:00.000Z"));
        }
    }
}
