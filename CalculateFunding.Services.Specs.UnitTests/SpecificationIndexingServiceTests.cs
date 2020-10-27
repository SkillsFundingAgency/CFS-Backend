using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests
{
    [TestClass]
    public class SpecificationIndexingServiceTests
    {
        private Mock<IJobManagement> _jobs;
        private Mock<ILogger> _logger;
        private Mock<ISpecificationIndexer> _indexer;
        private Mock<ISpecificationsRepository> _specifications;

        private SpecificationIndexingService _service;
        
        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _indexer = new Mock<ISpecificationIndexer>();
            _specifications = new Mock<ISpecificationsRepository>();
            _logger = new Mock<ILogger>();

            _service = new SpecificationIndexingService(_jobs.Object,
                _logger.Object,
                _indexer.Object,
                _specifications.Object,
                new SpecificationsResiliencePolicies
                {
                    SpecificationsRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task QueuesReIndexSpecificationJobForSuppliedId()
        {
            string specificationId = NewRandomString();
            Reference user = NewUser();
            string correlationId = NewRandomString();

            Job expectedJob = NewJob();
            
            GivenTheJob(expectedJob, specificationId, user.Id, correlationId);
            
            OkObjectResult result = await WhenTheSpecificationIndexJobIsQueued(specificationId, user, correlationId) as OkObjectResult;

            result?.Value
                .Should()
                .BeSameAs(expectedJob);
        }

        [TestMethod]
        public void GuardsAgainstNoMatchingSpecificationWhenRunningIndexingJob()
        {
            Message message = NewMessage(("specification-id", NewRandomString()),
                ("jobId", NewRandomString()));

            Func<Task> invocation = () => WhenTheIndexingJobIsRun(message);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task IndexesSpecificationWithIdSuppliedInMessage()
        {
            string specificationId = NewRandomString();
            Specification specification = NewSpecification();
            string jobId = NewRandomString();

            GivenTheSpecification(specificationId, specification);
            
            await WhenTheIndexingJobIsRun(NewMessage(("specification-id", specificationId),
                ("jobId", jobId)));
            
            ThenTheJobTrackingWasStarted(jobId);
            AndTheSpecificationWasIndexed(specification);
            AndTheJobTrackingWasCompleted(jobId);
        }

        [TestMethod]
        [DynamicData(nameof(InvalidIds), DynamicDataSourceType.Method)]
        public void GuardsAgainstNotSupplyingASpecificationId(string invalidSpecificationId)
        {
            Func<Task> invocation = () => WhenTheSpecificationIndexJobIsQueued(invalidSpecificationId, 
                NewUser(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        private void GivenTheSpecification(string specificationId,
            Specification specification)
        {
            _specifications.Setup(_ => _.GetSpecificationById(specificationId))
                .ReturnsAsync(specification);
        }

        private void AndTheSpecificationWasIndexed(Specification specification)
        {
            _indexer.Verify(_ => _.Index(specification),
                Times.Once);
        }

        private void ThenTheJobTrackingWasStarted(string jobId)
        {
            TheJobIdHadJobLogWithCompletedFlag(jobId);
        }

        private void AndTheJobTrackingWasCompleted(string jobId)
        {
            TheJobIdHadJobLogWithCompletedFlag(jobId, true);
        }

        private void TheJobIdHadJobLogWithCompletedFlag(string jobId,
            bool? completed = null)
        {
            _jobs.Verify(_ => _.UpdateJobStatus(jobId, 0, 0, completed, null),
                Times.Once);    
        }

        private void GivenTheJob(Job job,
            string specificationId,
            string userId,
            string correlationId)
        {
            _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(jcm =>
                    jcm.CorrelationId == correlationId &&
                    jcm.Properties.ContainsKey("specification-id") &&
                    jcm.Properties["specification-id"].Equals(specificationId) &&
                    jcm.SpecificationId == specificationId &&
                    jcm.JobDefinitionId == JobConstants.DefinitionNames.ReIndexSpecificationJob &&
                    jcm.InvokerUserId == userId)))
                .ReturnsAsync(job);
        }

        private async Task WhenTheIndexingJobIsRun(Message message)
        {
            await _service.Run(message);
        }

        private async Task<IActionResult> WhenTheSpecificationIndexJobIsQueued(string specificationId,
            Reference user,
            string correlationId)
            => await _service.QueueSpecificationIndexJob(specificationId, user, correlationId);
        
        private static Reference NewUser() => new ReferenceBuilder()
            .Build();
        
        private static Job NewJob() => new JobBuilder()
            .Build();
        
        private static Specification NewSpecification() => new SpecificationBuilder()
            .Build();

        private static Message NewMessage(params (string, string)[] userProperties)
        {
            Message message = new Message();
            
            message.AddUserProperties(userProperties);

            return message;
        }
        
        private static string NewRandomString() => new RandomString();

        private static IEnumerable<object[]> InvalidIds()
        {
            yield return new object[]
            {
                null
            };
            yield return new object[]
            {
                ""
            };
            yield return new object[]
            {
                "   "
            };
        }
        
    }
}