using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
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

            IJobManagement jobManagement = CreateJobManagement();

            JobService jobService = CreateJobService();

            //Act
            await jobService.CreateInstructAllocationJob(message);

            //Assert
            await
                jobManagement
                    .DidNotReceive()
                    .QueueJob(Arg.Any<JobCreateModel>());
        }

        [TestMethod]
        public async Task CreateInstructGenerateAggregationsAllocationJob_GivenNotCorrectJobType_DoesNotCreateNewJob()
        {
            //Arrange
            JobNotification jobNotification = CreateJobNotification();
            jobNotification.JobType = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob;

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IJobManagement jobManagement = CreateJobManagement();

            JobService jobService = CreateJobService(jobManagement);

            //Act
            await jobService.CreateInstructAllocationJob(message);

            //Assert
            await
                jobManagement
                    .DidNotReceive()
                    .QueueJob(Arg.Any<JobCreateModel>());
        }

        [TestMethod]
        public void CreateInstructGenerateAggregationsAllocationJob_GivenCreatingJobReturnsNull_ThrowsException()
        {
            //Arrange
            JobNotification jobNotification = CreateJobNotification();
            
            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns((Job)null);

            ILogger logger = CreateLogger();

            JobService jobService = CreateJobService(jobManagement, logger);

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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .QueueJob(Arg.Any<JobCreateModel>())
                .Returns(job);

            ILogger logger = CreateLogger();

            JobService jobService = CreateJobService(jobManagement, logger);

            //Act
            await jobService.CreateInstructAllocationJob(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"Created new job of type: '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' with id: '{job.Id}'"));
        }


        private static JobService CreateJobService(IJobManagement jobManagement = null, ILogger logger = null)
        {
            return new JobService(jobManagement ?? CreateJobManagement(), logger ?? CreateLogger());
        }

        private static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
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
