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
        #if NCRUNCH
        [Ignore]
        #endif
        [TestMethod]
        [DataRow(null)]
        [DataRow(new string[] { })]
        public async Task GetLatestJobsByJobDefinitionIds_WhenNoJobsSpecified_ReturnsBadRequest(IEnumerable<string> input)
        {
            IJobService service = CreateJobService();

            IActionResult result = await service.GetLatestJobsByJobDefinitionIds(input);

            result.Should().BeOfType<BadRequestObjectResult>();

            BadRequestObjectResult badRequestObjectResult = result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Subject;

            badRequestObjectResult
                .Value
                .Should()
                .Be($"At least one JobType must be provided.");
        }

        [TestMethod]
        public async Task GetLatestJobs_WhenNoJobs_ReturnOkResult_WithNull()
        {
            string jobDefinitionId = "jobType1";

            IJobService service = CreateJobService();

            IActionResult result = await service.GetLatestJobsByJobDefinitionIds(new[] { jobDefinitionId });

            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            Dictionary<string, JobViewModel>  model = okResult.Value
                .Should()
                .BeOfType<Dictionary<string, JobViewModel>>()
                .Subject;

            model
                .First()
                .Value
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task GetLatestJobsByJobDefinitionIds_WhenOnlyOneJob_ReturnJob()
        {
            string jobDefinition1Id = "jobType1";
            string jobDefinition2Id = "jobType2";

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobByJobDefinitionId(Arg.Is<string>(_ => _.Contains(jobDefinition1Id)))
                .Returns(new Job()
                {
                    Created = DateTimeOffset.UtcNow.AddHours(-1),
                    Id = "job1",
                    InvokerUserDisplayName = "test",
                    InvokerUserId = "test1",
                    JobDefinitionId = jobDefinition1Id,
                    LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                    RunningStatus = RunningStatus.InProgress,
                    SpecificationId = "ABC123",
                    Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                });

            jobRepository
                .GetLatestJobByJobDefinitionId(Arg.Is<string>(_ => _.Contains(jobDefinition2Id)))
                .Returns(new Job()
                {
                    Created = DateTimeOffset.UtcNow.AddHours(-1),
                    Id = "job2",
                    InvokerUserDisplayName = "test",
                    InvokerUserId = "test1",
                    JobDefinitionId = jobDefinition1Id,
                    LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                    RunningStatus = RunningStatus.InProgress,
                    SpecificationId = "ABC124",
                    Trigger = new Trigger { EntityId = "calc2", EntityType = "Calculation", Message = "Calc run started" }
                });

            IJobService service = CreateJobService(jobRepository);

            IActionResult result = await service.GetLatestJobsByJobDefinitionIds(new[] { jobDefinition1Id, jobDefinition2Id });

            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobViewModel> latestJobs = okResult.Value
                .Should()
                .BeAssignableTo<IDictionary<string, JobViewModel>>()
                .Subject;

            latestJobs[jobDefinition1Id].Id.Should().Be("job1");
            latestJobs[jobDefinition2Id].Id.Should().Be("job2");
        }

        [TestMethod]
        public async Task GetLatestJobsByJobDefinitionId_WhenOnlyOneJob_AndExistsInCache_ReturnJobFromCache()
        {
            string specificationId = "spec123";
            string jobDefinitionId = "jobType1";
            JobCacheItem jobCacheItem = new JobCacheItem
            {
                Job = new Job
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
                }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();

            string cacheKey = $"{CacheKeys.LatestJobsByJobDefinitionIds}{jobDefinitionId}";
            cacheProvider
                .GetAsync<JobCacheItem>(cacheKey)
                .Returns(jobCacheItem);

            IJobService service = CreateJobService(
                cacheProvider: cacheProvider);

            IActionResult result = await service.GetLatestJobsByJobDefinitionIds(new[] { jobDefinitionId });

            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobViewModel> latestJobs = okResult.Value
                .Should()
                .BeAssignableTo<IDictionary<string, JobViewModel>>()
                .Subject;

            latestJobs[jobDefinitionId].Id.Should().Be("job1");
        }

        [TestMethod]
        public async Task GetLatestJobsByJobDefinition_WhenTwoJobs_AndOneExistsInCache_AndOneDoesNotExistsInCache_ReturnLatestJobs()
        {
            string specificationId = "spec123";
            string jobType = "jobType1";
            string jobTypeTwo = "jobType2";

            JobCacheItem jobCacheItem = new JobCacheItem
            {
                Job = new Job
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
                }
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

            string cacheKey = $"{CacheKeys.LatestJobsByJobDefinitionIds}{jobType}";
            cacheProvider
                .GetAsync<JobCacheItem>(cacheKey)
                .Returns(jobCacheItem);

            string cacheKeyTwo = $"{CacheKeys.LatestJobsByJobDefinitionIds}{jobTypeTwo}";
            cacheProvider
                .SetAsync(jobTypeTwo, Arg.Is<JobCacheItem>(_ => _.Job.JobDefinitionId == jobTypeTwo))
                .Returns(Task.CompletedTask);


            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobByJobDefinitionId(Arg.Is<string>(_ => _ == jobTypeTwo))
                .Returns(jobTwo);

            IJobService service = CreateJobService(
                jobRepository: jobRepository,
                cacheProvider: cacheProvider);

            IActionResult result = await service.GetLatestJobsByJobDefinitionIds(
                new[] { jobType, jobTypeTwo });

            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            IDictionary<string, JobViewModel> latestJobs = okResult.Value
                 .Should()
                .BeAssignableTo<IDictionary<string, JobViewModel>>()
                .Subject;

            latestJobs.Count().Should().Be(2);
            latestJobs.Values.Should().Contain(x => x.Id == "job1");
            latestJobs.Values.Should().Contain(x => x.Id == "job2");

            await cacheProvider
                .Received(1)
                .SetAsync(cacheKeyTwo, Arg.Is<JobCacheItem>(_ => _.Job.JobDefinitionId == jobTypeTwo));
        }

        [TestMethod]
        public async Task GetLatestJobsByJobDefinition_WhenNoJobTypesProvided_ReturnBadRequest()
        {
            IJobRepository jobRepository = CreateJobRepository();
            IJobService service = CreateJobService(jobRepository);

            IActionResult result = await service.GetLatestJobsByJobDefinitionIds(Array.Empty<string>());

            BadRequestObjectResult badRequestObjectResult = result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Subject;

            badRequestObjectResult.Value.Should().Be("At least one JobType must be provided.");
        }
    }
}
