using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Action action = () => service.GetLatestJob(specificationId, null);

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
        public void GetLatestJob_WhenNoJobsForSpecification_ReturnNotFoundResult()
        {
            // Arrange
            string specificationId = "spec123";

            IJobService service = CreateJobService();

            // Act
            IActionResult result = service.GetLatestJob(specificationId, null);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public void GetLatestJob_WhenOnlyOneJobForSpecification_ReturnJob()
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
                    }
                }.AsQueryable());

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = service.GetLatestJob(specificationId, null);

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
        public void GetLatestJob_WhenMultipleJobsForSpecification_ReturnLatestJob()
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
            IActionResult result = service.GetLatestJob(specificationId, null);

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
        
        [TestMethod]
        public void GetLatestJob_WhenSingleJobTypeGiven_ReturnLatestJobOfType()
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
                        JobDefinitionId = "jobType2",
                        LastUpdated = DateTimeOffset.UtcNow.AddMinutes(-20),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    }
                }.AsQueryable());

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = service.GetLatestJob(specificationId, "jobType1");

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
        public void GetLatestJob_WhenSingleJobTypeGivenAndNoJobsOfType_ReturnNotFoundResult()
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
            IActionResult result = service.GetLatestJob(specificationId, "jobType2");

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public void GetLatestJob_WhenMultipleJobTypesGiven_ReturnLatestJobOfGivenTypes()
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
                        JobDefinitionId = "jobType2",
                        LastUpdated = DateTimeOffset.UtcNow.AddMinutes(-20),
                        RunningStatus = RunningStatus.InProgress,
                        SpecificationId = specificationId,
                        Trigger = new Trigger { EntityId = "calc1", EntityType = "Calculation", Message = "Calc run started" }
                    }
                }.AsQueryable());

            IJobService service = CreateJobService(jobRepository);

            // Act
            IActionResult result = service.GetLatestJob(specificationId, "jobType1,jobType2");

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
