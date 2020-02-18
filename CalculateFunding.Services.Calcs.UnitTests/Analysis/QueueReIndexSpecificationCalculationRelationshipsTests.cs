using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Analysis;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    [TestClass]
    public class QueueReIndexSpecificationCalculationRelationshipsTests
    {
        private const string SpecificationId = "specification-id";
        private QueueReIndexSpecificationCalculationRelationships _queue;
        private Mock<IJobsApiClient> _jobs;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobsApiClient>();

            _queue = new QueueReIndexSpecificationCalculationRelationships(_jobs.Object,
                new ResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void GuardsAgainstNotSpecificationIdBeingSupplied()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheJobIsQueued();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public async Task CreatesNewReIndexJobForSuppliedSpecificationId()
        {
            string specificationId = new RandomString();

            IActionResult result = await WhenTheJobIsQueued(specificationId);

            result
                .Should()
                .BeOfType<OkResult>();

            _jobs
                .Verify(_ => _.CreateJob(It.Is<JobCreateModel>(job =>
                        job.Properties.ContainsKey(SpecificationId) &&
                        job.Properties[SpecificationId] == specificationId &&
                        job.JobDefinitionId == JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob &&
                        job.SpecificationId == specificationId &&
                        job.Trigger != null &&
                        job.Trigger.EntityId == specificationId &&
                        job.Trigger.EntityType == nameof(Specification))),
                    Times.Once);
        }

        private async Task<IActionResult> WhenTheJobIsQueued(string specificationId = null)
        {
            return await _queue.QueueForSpecification(specificationId);
        }
    }
}