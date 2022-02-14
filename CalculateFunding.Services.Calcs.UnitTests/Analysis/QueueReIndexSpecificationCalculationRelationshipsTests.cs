using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
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
        private Mock<IJobManagement> _jobs;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();

            _queue = new QueueReIndexSpecificationCalculationRelationships(_jobs.Object);
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
                .Verify(_ => _.QueueJob(It.Is<JobCreateModel>(job =>
                        job.Properties.ContainsKey(SpecificationId) &&
                        job.Properties[SpecificationId] == specificationId &&
                        job.JobDefinitionId == JobConstants.DefinitionNames.ReIndexSpecificationCalculationRelationshipsJob &&
                        job.SpecificationId == specificationId &&
                        job.Trigger != null &&
                        job.Trigger.EntityId == specificationId &&
                        job.Trigger.EntityType == "Specification")),
                    Times.Once);
        }

        private async Task<IActionResult> WhenTheJobIsQueued(string specificationId = null)
        {
            return await _queue.QueueForSpecification(specificationId);
        }
    }
}