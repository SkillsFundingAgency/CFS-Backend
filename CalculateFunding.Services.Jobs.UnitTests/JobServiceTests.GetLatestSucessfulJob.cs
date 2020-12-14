using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs
{
    public partial class JobServiceTests
    {
        [TestMethod]
        public void GetLatestSuccessfulJobs_WhenSpecificationIdIsNull_ThrowArgumentNullException()
        {
            // Arrange
            string specificationId = null;
            string jobDefinitionId = NewRandomString();

            IJobService service = CreateJobService();

            // Act
            Func<Task> action = () => service.GetLatestSuccessfulJob(specificationId, jobDefinitionId);

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
        public void GetLatestSuccessfulJobs_WhenJobDefiniotnIdIsNull_ThrowArgumentNullException()
        {
            // Arrange
            string specificationId = NewRandomString();
            string jobDefinitionId = null;

            IJobService service = CreateJobService();

            // Act
            Func<Task> action = () => service.GetLatestSuccessfulJob(specificationId, jobDefinitionId);

            // Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("jobDefinitionId");
        }

        [TestMethod]
        public async Task GetLatestSuccessfulJobs_WhenNoJobsForSpecificationAndDefinitionId_ReturnNotFoundResult()
        {
            // Arrange
            string specificationId = NewRandomString();
            string jobDefinitionId = NewRandomString(); 

            IJobService service = CreateJobService();

            // Act
            IActionResult result = await service.GetLatestSuccessfulJob(specificationId, jobDefinitionId);

            // Assert
            result.Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"No successfully completed job found for specification '{specificationId}' and jobDefinitonId '{jobDefinitionId}'.");
        }

        [TestMethod]
        public async Task GetLatestSuccessfulJobs_WhenSucessfulJobForSpecificationAndJobDefinitionExistsInCache_ReturnJobFromCache()
        {
            // Arrange
            string specificationId = NewRandomString();
            string jobDefinitionId = NewRandomString();
            string jobId = NewRandomString();

            Job job = new Job
            {
                Created = DateTimeOffset.UtcNow.AddHours(-1),
                Id = jobId,
                InvokerUserDisplayName = "test",
                InvokerUserId = "test1",
                JobDefinitionId = jobDefinitionId,
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                RunningStatus = RunningStatus.InProgress,
                SpecificationId = specificationId,
                Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();

            string cacheKey = $"{CacheKeys.LatestSuccessfulJobs}{specificationId}:{jobDefinitionId}";
            cacheProvider.GetAsync<Job>(cacheKey).Returns(job);

            IJobService service = CreateJobService(cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetLatestSuccessfulJob(specificationId, jobDefinitionId);

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobSummary jobSummary = okResult.Value
                .Should()
                .BeAssignableTo<JobSummary>()
                .Subject;

            jobSummary.JobId.Should().Be(jobId);
        }

        [TestMethod]
        public async Task GetLatestSuccessfulJobs_WhenSucessfulJobForSpecificationAndJobDefinitionNOtExistsInCache_RetrieveAndReturnJobFromRepository()
        {
            // Arrange
            string specificationId = NewRandomString();
            string jobDefinitionId = NewRandomString();
            string jobId = NewRandomString();

            Job job = new Job
            {
                Created = DateTimeOffset.UtcNow.AddHours(-1),
                Id = jobId,
                InvokerUserDisplayName = "test",
                InvokerUserId = "test1",
                JobDefinitionId = jobDefinitionId,
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                RunningStatus = RunningStatus.InProgress,
                SpecificationId = specificationId,
                Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();

            string cacheKey = $"{CacheKeys.LatestSuccessfulJobs}{specificationId}:{jobDefinitionId}";
            cacheProvider.GetAsync<Job>(cacheKey).Returns((Job)null);

            IJobRepository jobRepository = CreateJobRepository();
            jobRepository
                .GetLatestJobBySpecificationIdAndDefinitionId(specificationId, jobDefinitionId, CompletionStatus.Succeeded)
                .Returns(job);

            IJobService service = CreateJobService(cacheProvider: cacheProvider, jobRepository: jobRepository);

            // Act
            IActionResult result = await service.GetLatestSuccessfulJob(specificationId, jobDefinitionId);

            // Assert
            OkObjectResult okResult = result
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject;

            JobSummary jobSummary = okResult.Value
                .Should()
                .BeAssignableTo<JobSummary>()
                .Subject;

            jobSummary.JobId.Should().Be(jobId);
        }
    }
}
