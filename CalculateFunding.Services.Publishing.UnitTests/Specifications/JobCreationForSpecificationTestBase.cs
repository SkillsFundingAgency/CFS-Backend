using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Policy = Polly.Policy;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    public abstract class JobCreationForSpecificationTestBase
    {
        protected IJobsApiClient Jobs;
        protected IPublishingResiliencePolicies ResiliencePolicies;
        protected ILogger Logger;
        protected ICreateJobsForSpecifications JobCreation;

        protected string JobDefinitionId;
        protected string TriggerMessage;

        [TestInitialize]
        public void JobCreationForSpecificationTestBaseSetUp()
        {
            Jobs = Substitute.For<IJobsApiClient>();
            ResiliencePolicies = Substitute.For<IPublishingResiliencePolicies>();
            Logger = Substitute.For<ILogger>();

            ResiliencePolicies.JobsApiClient.Returns(Policy.NoOpAsync());
        }

        [TestMethod]
        public async Task CreatesRefreshFundingJobForSpecificationId()
        {
            Job expectedJob = NewJob();
            Reference user = NewUser();
            string correlationId = NewRandomString();
            string specificationId = NewRandomString();

            GivenTheJobCreatedForDetails(specificationId, correlationId, user, expectedJob);

            Job actualJob = await WhenTheSpecificationsJobIsCreated(specificationId, user, correlationId);

            actualJob
                .Should()
                .BeSameAs(expectedJob);
        }

        [TestMethod]
        public void ThrowsCustomExceptionMessageOnError()
        {
            GivenTheCreateJobThrowsException(new InvalidOperationException(NewRandomString()));

            string specificationId = NewRandomString();
            Func<Task<Job>> invocation
                = () => WhenTheSpecificationsJobIsCreated(specificationId,
                    NewUser(),
                    NewRandomString());

            invocation
                .Should()
                .Throw<Exception>()
                .Where(_ => 
                    _.Message == $"Failed to queue publishing of specification with id: {specificationId}");
        }

        private async Task<Job> WhenTheSpecificationsJobIsCreated(string specificationId,
            Reference user,
            string correlationId)
        {
            return await JobCreation.CreateJob(specificationId, user, correlationId);
        }

        private void GivenTheCreateJobThrowsException(Exception exception)
        {
            Jobs.CreateJob(Arg.Any<JobCreateModel>())
                .Throws(exception);
        }

        private void GivenTheJobCreatedForDetails(string specificationId,
            string correlationId,
            Reference user,
            Job job)
        {
            Jobs.CreateJob(Arg.Is<JobCreateModel>(_ =>
                    _.CorrelationId == correlationId &&
                    _.SpecificationId == specificationId &&
                    _.JobDefinitionId == JobDefinitionId &&
                    _.InvokerUserId == user.Id &&
                    _.InvokerUserDisplayName == user.Name &&
                    _.Properties != null &&
                    _.Properties.ContainsKey("specification-id") &&
                    _.Properties["specification-id"] == specificationId &&
                    _.Trigger != null &&
                    _.Trigger.EntityId == specificationId &&
                    _.Trigger.EntityType == nameof(Specification) &&
                    _.Trigger.Message == TriggerMessage))
                .Returns(job);
        }

        protected string NewRandomString()
        {
            return new RandomString();
        }

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