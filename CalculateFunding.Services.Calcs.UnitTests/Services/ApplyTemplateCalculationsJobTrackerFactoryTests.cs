using System;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class ApplyTemplateCalculationsJobTrackerFactoryTests
    {
        private ILogger _logger;
        private IJobsApiClient _jobs;
        private AsyncPolicy _resiliencePolicy;
        private Message _message;

        private ApplyTemplateCalculationsJobTrackerFactory _factory;

        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _resiliencePolicy = Policy.NoOpAsync();
            _message = new Message();
            _jobs = Substitute.For<IJobsApiClient>();

            _factory = new ApplyTemplateCalculationsJobTrackerFactory(_jobs,
                new ResiliencePolicies
                {
                    JobsApiClient = _resiliencePolicy
                },
                _logger);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoJobIdInTheMessageProperties()
        {
            Func<IApplyTemplateCalculationsJobTracker> invocation = WhenTheJobTrackerIsCreated;

            invocation
                .Should()
                .Throw<Exception>()
                .WithMessage("No JobId property in message");
        }

        [TestMethod]
        public void CreatesNewApplyTemplateCalculationsJobTrackerUsingReferencedComponents()
        {
            string expectedJobId = new RandomString();
            
            GivenTheJobIdInTheMessage(expectedJobId);
            
            IApplyTemplateCalculationsJobTracker jobTracker = WhenTheJobTrackerIsCreated();

            jobTracker
                .Should()
                .BeOfType<ApplyTemplateCalculationsJobTracker>();

            jobTracker
                .Jobs
                .Should()
                .BeSameAs(_jobs);
            
            jobTracker
                .Logger
                .Should()
                .BeSameAs(_logger);
            
            jobTracker
                .JobsResiliencePolicy
                .Should()
                .BeSameAs(_resiliencePolicy);
            
            jobTracker
                .JobId
                .Should()
                .Be(expectedJobId);
        }

        private void GivenTheJobIdInTheMessage(string jobId)
        {
            _message.UserProperties.Add("jobId", jobId);
        }

        private IApplyTemplateCalculationsJobTracker WhenTheJobTrackerIsCreated()
        {
            return _factory.CreateJobTracker(_message);
        }
    }
}