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

        [DataTestMethod]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion)]
        public void QueueJobGuardsAgainstMissingSpecificationId(SqlExportSource sqlExportSource)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheImportIsQueued(null, NewRandomString(), sqlExportSource);

            invocation
                .Should()
                .ThrowAsync<ArgumentNullException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [DataTestMethod]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion)]
        public void QueueJobGuardsAgainstMissingFundingStreamId(SqlExportSource sqlExportSource)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheImportIsQueued(NewRandomString(), null, sqlExportSource);

            invocation
                .Should()
                .ThrowAsync<ArgumentNullException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [DataTestMethod]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion)]
        public async Task QueuesRunSqlImportJobs(SqlExportSource sqlExportSource)
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            
            Job expectedJob = new()
            {
                Id = NewRandomString()
            };

            GivenTheRunSqlImportJob(specificationId, fundingStreamId, expectedJob, sqlExportSource);
            
            OkObjectResult result = await WhenTheImportIsQueued(specificationId, fundingStreamId, sqlExportSource) as OkObjectResult;

            result?.Value
                .Should()
                .BeEquivalentTo(new
                {
                    JobId = expectedJob.Id
                });
        }

        [DataTestMethod]
        [DataRow(SqlExportSource.CurrentPublishedProviderVersion)]
        [DataRow(SqlExportSource.ReleasedPublishedProviderVersion)]
        public async Task JobRunsSchemaGenerationThenImportForSpecificationAndFundingStreamSpecifiedInMessage(SqlExportSource sqlExportSource)
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();

            _importService.Job = new JobViewModel
            {
                JobDefinitionId = sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion ? JobConstants.DefinitionNames.RunSqlImportJob : JobConstants.DefinitionNames.RunReleasedSqlImportJob
            };

            await WhenTheImportIsRun(NewMessage(_ => _.WithUserProperty(SpecificationId, specificationId)
                .WithUserProperty(FundingStreamId, fundingStreamId)));
            
            ThenTheSchemaWasReCreated(specificationId, fundingStreamId, sqlExportSource);
            AndTheImportWasRun(specificationId, fundingStreamId, sqlExportSource);
        }

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);
            
            return messageBuilder.Build();
        }

        private void ThenTheSchemaWasReCreated(
            string specificationId, 
            string fundingStreamId,
            SqlExportSource sqlExportSource)
            => _schema.Verify(_ => _.ReCreateTablesForSpecificationAndFundingStream(specificationId, fundingStreamId, sqlExportSource),
                Times.Once);

        private void AndTheImportWasRun(
            string specificationId,
            string fundingStreamId,
            SqlExportSource sqlExportSource)
            => _import.Verify(_ => _.ImportData(specificationId, fundingStreamId, null, sqlExportSource),
                Times.Once);

        private void GivenTheRunSqlImportJob(
            string specificationId,
            string fundingStreamId,
            Job job,
            SqlExportSource sqlExportSource)
            => _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(
                    jcm => jcm.JobDefinitionId == (sqlExportSource == SqlExportSource.CurrentPublishedProviderVersion ? JobConstants.DefinitionNames.RunSqlImportJob : JobConstants.DefinitionNames.RunReleasedSqlImportJob) &&
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

        private async Task<IActionResult> WhenTheImportIsQueued(
            string specificationId,
            string fundingStreamId,
            SqlExportSource sqlExportSource)
            => await _importService.QueueSqlImport(specificationId, fundingStreamId, null, null, sqlExportSource);

        private static string NewRandomString() => new RandomString();
    }
}