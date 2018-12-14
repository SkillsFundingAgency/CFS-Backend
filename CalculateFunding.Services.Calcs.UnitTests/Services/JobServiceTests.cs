using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class JobServiceTests
    {
        [TestMethod]
        public async Task CreateInstructGenerateAggregationsAllocationJob_GivenJobNotSucceeeded_DoesNotCreateNewJob()
        {
            //Arrange
            JobNotification jobNotification = CreateJobNotification();
            jobNotification.CompletionStatus = CompletionStatus.Cancelled;

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IJobsRepository jobsRepository = CreateJobsRepository();

            JobService jobService = CreateJobService();

            //Act
            await jobService.CreateInstructAllocationJob(message);

            //Assert
            await
                jobsRepository
                    .DidNotReceive()
                    .CreateJob(Arg.Any<JobCreateModel>());
        }

        [TestMethod]
        public async Task CreateInstructGenerateAggregationsAllocationJob_GivenNotCorrectJobType_DoesNotCreateNewJob()
        {
            //Arrange
            JobNotification jobNotification = CreateJobNotification();
            jobNotification.JobType = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob;

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IJobsRepository jobsRepository = CreateJobsRepository();

            JobService jobService = CreateJobService(jobsRepository);

            //Act
            await jobService.CreateInstructAllocationJob(message);

            //Assert
            await
                jobsRepository
                    .DidNotReceive()
                    .CreateJob(Arg.Any<JobCreateModel>());
        }

        [TestMethod]
        public void CreateInstructGenerateAggregationsAllocationJob_GivenCreatingJobReturnsNull_ThrowsException()
        {
            //Arrange
            JobNotification jobNotification = CreateJobNotification();
            
            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns((Job)null);

            ILogger logger = CreateLogger();

            JobService jobService = CreateJobService(jobsRepository, logger);

            //Act
            Func<Task> test = () => jobService.CreateInstructAllocationJob(message);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to create new job of type: '{JobConstants.DefinitionNames.CreateInstructAllocationJob}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to create new job of type: '{JobConstants.DefinitionNames.CreateInstructAllocationJob}'"));
        }

        [TestMethod]
        public async Task CreateInstructGenerateAggregationsAllocationJob_GivenJobCreated_LogsInformation()
        {
            //Arrange
            JobNotification jobNotification = CreateJobNotification();

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            Job job = new Job
            {
                Id = "job-id-1"
            };

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(job);

            ILogger logger = CreateLogger();

            JobService jobService = CreateJobService(jobsRepository, logger);

            //Act
            await jobService.CreateInstructAllocationJob(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"Created new job of type: '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' with id: '{job.Id}'"));
        }


        private static JobService CreateJobService(IJobsRepository jobsRepository = null, ILogger logger = null)
        {
            return new JobService(jobsRepository ?? CreateJobsRepository(), logger ?? CreateLogger());
        }

        private static IJobsRepository CreateJobsRepository()
        {
            return Substitute.For<IJobsRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static JobNotification CreateJobNotification()
        {
            return new JobNotification
            {
                CompletionStatus = CompletionStatus.Succeeded,
                JobType = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob,
                InvokerUserDisplayName = "user",
                InvokerUserId = "user-id",
                SpecificationId = "spec-1",
                Trigger = new Trigger
                {
                    Message = "message",
                    EntityId = "calc-1",
                    EntityType = "Calculation"
                }
            };
        }
    }
}
