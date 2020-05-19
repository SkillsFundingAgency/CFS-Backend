using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class DeletePublishedProvidersJobCreationTests
    {
        private const string DeletePublishedProvidersJob = JobConstants.DefinitionNames.DeletePublishedProvidersJob;
        private IJobManagement _jobs;
        private DeletePublishedProvidersJobCreation _jobCreation;
        private IPublishingResiliencePolicies _resiliencePolicies;
        private ILogger _logger;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();

            _jobCreation = new DeletePublishedProvidersJobCreation(_jobs,
                _logger);
        }

        [TestMethod]
        public async Task CreatesRefreshFundingJobForSpecificationId()
        {
            Job expectedJob = NewJob();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string correlationId = NewRandomString();

            GivenTheJobCreatedForDetails(fundingPeriodId, fundingStreamId, correlationId,  expectedJob);

            Job actualJob = await WhenTheSpecificationsJobIsCreated(fundingPeriodId, fundingStreamId, correlationId);

            actualJob
                .Should()
                .BeSameAs(expectedJob);
        }

        [TestMethod]
        public void DoesNotSwallowExceptions()
        {
            InvalidOperationException expectedException = new InvalidOperationException(NewRandomString());
            
            GivenTheCreateJobThrowsException(expectedException);

            Func<Task<Job>> invocation
                = () => WhenTheSpecificationsJobIsCreated(NewRandomString(),
                    NewRandomString(),
                    NewRandomString());

            invocation
                .Should()
                .Throw<Exception>()
                .Where(_ => ReferenceEquals(_, expectedException));
        }

        private async Task<Job> WhenTheSpecificationsJobIsCreated(string fundingPeriodId,
            string fundingStreamId,
            string correlationId)
        {
            return await _jobCreation.CreateJob(fundingStreamId, fundingPeriodId, correlationId);
        }

        private void GivenTheCreateJobThrowsException(Exception exception)
        {
            _jobs.QueueJob(Arg.Any<JobCreateModel>())
                .Throws(exception);
        }

        private void GivenTheJobCreatedForDetails(string fundingPeriodId,
            string fundingStreamId,
            string correlationId,
            Job job)
        {
            _jobs.QueueJob(Arg.Is<JobCreateModel>(_ =>
                    _.CorrelationId == correlationId &&
                    _.SpecificationId == null &&
                    _.JobDefinitionId == DeletePublishedProvidersJob &&
                    _.Properties != null &&
                    _.Properties["funding-stream-id"] == fundingStreamId &&
                    _.Properties["funding-period-id"] == fundingPeriodId &&
                    _.Trigger != null &&
                    _.Trigger.EntityId == "N/A" &&
                    _.Trigger.EntityType == null &&
                    _.Trigger.Message == $"Requested deletion of published providers for funding stream {fundingStreamId} and funding period {fundingPeriodId}"))
                .Returns(job);
        }

        private string NewRandomString() => new RandomString();

        protected Reference NewUser(Action<UserBuilder> setUp = null)
        {
            UserBuilder userBuilder = new UserBuilder();

            setUp?.Invoke(userBuilder);

            return userBuilder.Build();
        }

        protected Job NewJob(Action<JobBuilder> setUp = null)
        {
            JobBuilder jobBuilder = new JobBuilder();

            setUp?.Invoke(jobBuilder);

            return jobBuilder.Build();
        }
    }
}