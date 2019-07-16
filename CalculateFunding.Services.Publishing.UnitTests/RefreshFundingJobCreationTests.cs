using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Constants;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Policy = Polly.Policy;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class RefreshFundingJobCreationTests : SpecificationPublishingServiceTestsBase
    {
        private IJobsApiClient _jobs;
        private ICalcsResiliencePolicies _resiliencePolicies;
        private ILogger _logger;

        private RefreshFundingJobCreation _jobCreation;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = Substitute.For<IJobsApiClient>();
            _resiliencePolicies = Substitute.For<ICalcsResiliencePolicies>();
            _logger = Substitute.For<ILogger>();

            _resiliencePolicies.JobsApiClient.Returns(Policy.NoOpAsync());

            _jobCreation = new RefreshFundingJobCreation(_jobs,
                _resiliencePolicies,
                _logger);
        }

        [TestMethod]
        public async Task CreatesRefreshFundingJobForSpecificationId()
        {
            Job expectedJob = NewJob();
            Reference user = NewUser();
            string correlationId = NewRandomString();
            string specificationId = NewRandomString();

            GivenTheJobCreatedForDetails(specificationId, correlationId, user, expectedJob);

            Job actualJob = await WhenTheRefreshFundingJobIsCreated(specificationId, user, correlationId);

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
                = () => WhenTheRefreshFundingJobIsCreated(specificationId,
                    NewUser(),
                    NewRandomString());

            invocation
                .Should()
                .Throw<Exception>()
                .Where(_ => 
                    _.Message == $"Failed to queue publishing of specification with id: {specificationId}");
        }

        private async Task<Job> WhenTheRefreshFundingJobIsCreated(string specificationId,
            Reference user,
            string correlationId)
        {
            return await _jobCreation.CreateJob(specificationId, user, correlationId);
        }

        private void GivenTheCreateJobThrowsException(Exception exception)
        {
            _jobs.CreateJob(Arg.Any<JobCreateModel>())
                .Throws(exception);
        }

        private void GivenTheJobCreatedForDetails(string specificationId,
            string correlationId,
            Reference user,
            Job job)
        {
            _jobs.CreateJob(Arg.Is<JobCreateModel>(_ =>
                    _.CorrelationId == correlationId &&
                    _.SpecificationId == specificationId &&
                    _.JobDefinitionId == JobConstants.DefinitionNames.CreateRefreshFundingjob &&
                    _.InvokerUserId == user.Id &&
                    _.InvokerUserDisplayName == user.Name &&
                    _.Properties != null &&
                    _.Properties.ContainsKey("specification-id") &&
                    _.Properties["specification-id"] == specificationId &&
                    _.Trigger != null &&
                    _.Trigger.EntityId == specificationId &&
                    _.Trigger.EntityType == nameof(Specification) &&
                    _.Trigger.Message == "Requesting publication of specification"))
                .Returns(job);
        }
    }
}