using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    [TestClass]
    public class PublishedFundingUndoJobCreationTests
    {
        private Mock<IJobsApiClient> _jobs;
        private PublishedFundingUndoJobCreation _jobCreation;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobsApiClient>();
            _jobCreation = new PublishedFundingUndoJobCreation(_jobs.Object,
                new ResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public async Task QueuesPublishedFundingUndoJobForSuppliedCorrelationId()
        {
            string forCorrelationId = NewRandomString();
            string correlationId = NewRandomString();
            string specificationId = NewRandomString();
            bool isHardDelete = NewRandomFlag();
            Reference user = NewReference();
            string apiVersion = "v3";
            List<string> channelCodes = new List<string>();
            
            Job expectedJob = new Job();
            
            GivenTheJobForRequest(forCorrelationId,
                isHardDelete,
                user,
                correlationId,
                specificationId,
                expectedJob,
                apiVersion,
                channelCodes);

            Job actualJob = await _jobCreation.CreateJob(forCorrelationId,
                specificationId,
                isHardDelete,
                user,
                correlationId,
                apiVersion,
                channelCodes);

            actualJob
                .Should()
                .BeSameAs(expectedJob);
        }
        [TestMethod]
        public async Task QueuesPublishedFundingUndoJobForSuppliedCorrelationIdForV4API()
        {
            string forCorrelationId = NewRandomString();
            string correlationId = NewRandomString();
            string specificationId = NewRandomString();
            bool isHardDelete = NewRandomFlag();
            Reference user = NewReference();
            string apiVersion = "v4";
            List<string> channelCodes = new List<string>() { "Statement" };

            Job expectedJob = new Job();

            GivenTheJobForRequest(forCorrelationId,
                isHardDelete,
                user,
                correlationId,
                specificationId,
                expectedJob,
                apiVersion,
                channelCodes);

            Job actualJob = await _jobCreation.CreateJob(forCorrelationId,
                specificationId,
                isHardDelete,
                user,
                correlationId,
                apiVersion,
                channelCodes);

            actualJob
                .Should()
                .BeSameAs(expectedJob);
        }
        private void GivenTheJobForRequest(string forCorrelationId,
            bool isHardDelete,
            Reference user,
            string correlationId,
            string specificationId,
            Job expectedJob,
            string apiVersion,
            List<string> channelCodes)
        {
            _jobs.Setup(_ => _.CreateJob(It.Is<JobCreateModel>(jcm =>
                    jcm.CorrelationId == correlationId &&
                    jcm.JobDefinitionId == JobConstants.DefinitionNames.PublishedFundingUndoJob &&
                    jcm.SpecificationId == specificationId &&
                    jcm.InvokerUserId == user.Id &&
                    HasUserProperties(jcm.Properties,
                        "for-correlation-id", forCorrelationId,
                        "specification-id", specificationId,
                        "is-hard-delete", isHardDelete.ToString(),
                        "user-id", user.Id,
                        "user-name", user.Name,
                        "api-version", apiVersion,
                        "channel-codes", string.Join(",", channelCodes)
                    ))))
                .ReturnsAsync(expectedJob);
        }

        private bool HasUserProperties(IDictionary<string, string> properties,
            params string[] expectedPropertyPairs)
        {
            Dictionary<string, string> expectedProperties = new Dictionary<string, string>();

            for (int propertyName = 0; propertyName < expectedPropertyPairs.Length; propertyName += 2)
            {
                expectedProperties[expectedPropertyPairs[propertyName]] = expectedPropertyPairs[propertyName + 1];
            }

            return expectedProperties.Count == properties.Count &&
                   properties.All(_ => expectedProperties.Contains(_));
        }

        private Reference NewReference() => new ReferenceBuilder().Build();

        private string NewRandomString() => new RandomString();

        private bool NewRandomFlag() => new RandomBoolean();
    }
}