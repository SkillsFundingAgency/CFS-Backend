using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class SqlImportServiceTests
    {
        private const string SpecificationId = "specification-id";
        private const string FundingStreamId = "funding-stream-id";
        
        private Mock<ISqlImporter> _import;
        private Mock<IQaSchemaService> _schema;
        private Mock<IJobManagement> _jobs;

        private SqlImportService _importService;

        [TestInitialize]
        public void SetUp()
        {
            _import = new Mock<ISqlImporter>();
            _schema = new Mock<IQaSchemaService>();
            _jobs = new Mock<IJobManagement>();

            _importService = new SqlImportService(_import.Object,
                _schema.Object,
                _jobs.Object,
                Logger.None);
        }

        [TestMethod]
        public void QueueJobGuardsAgainstMissingSpecificationId()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheImportIsQueued(null, NewRandomString());

            invocation
                .Should()
                .ThrowAsync<ArgumentNullException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public void QueueJobGuardsAgainstMissingFundingStreamId()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheImportIsQueued(NewRandomString(), null);

            invocation
                .Should()
                .ThrowAsync<ArgumentNullException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [TestMethod]
        public async Task QueuesRunSqlImportJobs()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            
            Job expectedJob = new Job();

            GivenTheRunSqlImportJob(specificationId, fundingStreamId, expectedJob);
            
            OkObjectResult result = await WhenTheImportIsQueued(specificationId, fundingStreamId) as OkObjectResult;

            result?.Value
                .Should()
                .BeSameAs(expectedJob);
        }

        [TestMethod]
        public async Task JobRunsSchemaGenerationThenImportForSpecificationAndFundingStreamSpecifiedInMessage()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();

            await WhenTheImportIsRun(NewMessage(_ => _.WithUserProperty(SpecificationId, specificationId)
                .WithUserProperty(FundingStreamId, fundingStreamId)));
            
            ThenTheSchemaWasReCreated(specificationId, fundingStreamId);
            AndTheImportWasRun(specificationId, fundingStreamId);
        }

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);
            
            return messageBuilder.Build();
        }

        private void ThenTheSchemaWasReCreated(string specificationId, 
            string fundingStreamId)
            => _schema.Verify(_ => _.ReCreateTablesForSpecificationAndFundingStream(specificationId, fundingStreamId),
                Times.Once);

        private void AndTheImportWasRun(string specificationId,
            string fundingStreamId)
            => _import.Verify(_ => _.ImportData(specificationId, fundingStreamId),
                Times.Once);
        private void GivenTheRunSqlImportJob(string specificationId,
            string fundingStreamId,
            Job job)
            => _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(
                    jcm => jcm.JobDefinitionId == JobConstants.DefinitionNames.RunSqlImportJob &&
                           jcm.SpecificationId == specificationId &&
                           HasProperty(jcm.Properties, SpecificationId, specificationId) &&
                           HasProperty(jcm.Properties, FundingStreamId, fundingStreamId))))
                .ReturnsAsync(job);

        private static bool HasProperty(IDictionary<string, string> actualProperties,
            string key,
            string value)
            => actualProperties.ContainsKey(key!) &&
               actualProperties[key] == value;

        private async Task WhenTheImportIsRun(Message message)
            => await _importService.Process(message);

        private async Task<IActionResult> WhenTheImportIsQueued(string specificationId,
            string fundingStreamId)
            => await _importService.QueueSqlImport(specificationId, fundingStreamId, null, null);

        private static string NewRandomString() => new RandomString();
    }
}