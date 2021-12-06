using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.UnitTests.ApiClientHelpers.Jobs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public class CalculationResultQADatabasePopulationServiceTests
    {
        private const string JobId = "jobId";

        private Mock<IJobManagement> _jobs;
        private Mock<IQaSchemaService> _qaSchemaService;

        private CalculationResultQADatabasePopulationService _service;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _qaSchemaService = new Mock<IQaSchemaService>();

            _service = new CalculationResultQADatabasePopulationService(
                _qaSchemaService.Object,
                new ResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync(),
                },
                _jobs.Object,
                Logger.None);
        }

        [TestMethod]
        public void QueueMergeSpecificationInformationJobGuardsAgainstMissingMergeRequest()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCalculationResultQADatabasePopulationJobIsQueued(null, NewUser(), NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("populateCalculationResultQADatabaseRequest");
        }

        [TestMethod]
        public async Task QueueMergeSpecificationInformationJobCreatesNewJobWithSuppliedMergeRequest()
        {
            string specificationId = NewRandomString();
            PopulateCalculationResultQADatabaseRequest populateCalculationResultQADatabaseRequest  
                = NewPopulateCalculationResultQADatabaseRequest(_ => _.WithSpecificationId(specificationId));
            Job expectedJob = NewJob();
            Reference user = NewUser();
            string correlationId = NewRandomString();

            GivenTheJob(expectedJob, populateCalculationResultQADatabaseRequest, user, correlationId, specificationId);

            OkObjectResult okObjectResult = await WhenTheCalculationResultQADatabasePopulationJobIsQueued(populateCalculationResultQADatabaseRequest, user, correlationId) as OkObjectResult;

            okObjectResult?.Value
                .Should()
                .BeSameAs(expectedJob);
        }

        private async Task<IActionResult> WhenTheCalculationResultQADatabasePopulationJobIsQueued(
            PopulateCalculationResultQADatabaseRequest request,
            Reference user,
            string correlationId)
            => await _service.QueueCalculationResultQADatabasePopulationJob(request, user, correlationId);

        private void GivenTheJob(Job job,
            PopulateCalculationResultQADatabaseRequest request,
            Reference user,
            string correlationId,
            string specificationId)
        {
            _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(jb =>
                    jb.SpecificationId == specificationId &&
                    jb.CorrelationId == correlationId &&
                    jb.InvokerUserId == user.Id &&
                    jb.InvokerUserDisplayName == user.Name &&
                    jb.MessageBody == request.AsJson(true) &&
                    jb.JobDefinitionId == JobConstants.DefinitionNames.PopulateCalculationResultsQaDatabaseJob &&
                    jb.Properties.ContainsKey("specification-id") &&
                    jb.Properties["specification-id"] == specificationId
                )))
                .ReturnsAsync(job);
        }

        private Reference NewUser() => new ReferenceBuilder().Build();

        private string NewRandomString() => new RandomString();

        private Job NewJob() => new JobBuilder().Build();

        private PopulateCalculationResultQADatabaseRequest NewPopulateCalculationResultQADatabaseRequest(Action<PopulateCalculationResultQADatabaseRequestBuilder> setUp = null)
        {
            PopulateCalculationResultQADatabaseRequestBuilder populateCalculationResultQADatabaseRequestBuilder = new PopulateCalculationResultQADatabaseRequestBuilder();

            setUp?.Invoke(populateCalculationResultQADatabaseRequestBuilder);

            return populateCalculationResultQADatabaseRequestBuilder.Build();
        }
    }
}
