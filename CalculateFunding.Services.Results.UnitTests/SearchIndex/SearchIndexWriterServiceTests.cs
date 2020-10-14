using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.SearchIndex;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.UnitTests.SearchIndex
{
    [TestClass]
    public class SearchIndexWriterServiceTests
    {
        private SearchIndexWriterService _service;
        private ILogger _logger;
        private IJobManagement _jobManagement;
        private ISearchIndexProcessorFactory _searchIndexProcessorFactory;
        private ISearchIndexProcessor _searchIndexProcessor;

        [TestInitialize]
        public void Setup()
        {
            _logger = Substitute.For<ILogger>();
            _jobManagement = Substitute.For<IJobManagement>();
            _searchIndexProcessorFactory = Substitute.For<ISearchIndexProcessorFactory>();
            _service = new SearchIndexWriterService(_logger, _jobManagement, _searchIndexProcessorFactory);
            _searchIndexProcessor = Substitute.For<ISearchIndexProcessor>();
        }

        [TestMethod]
        public async Task ShouldRaiseException_WhenNoIndexWriterTypeGivenOnMessageUserProperties()
        {
            Message message = new Message();
            string jobId = Guid.NewGuid().ToString();
            message.UserProperties["jobId"] = jobId;
            string expectedErrorMessage = $"Index-writer-type missing from SearchIndexWriter job. JobId {jobId}";

            _jobManagement.GetJobById(jobId).Returns(new JobViewModel() { Id = jobId });

            Func<Task> test = async () => await _service.CreateSearchIndex(message);

            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be(expectedErrorMessage);

        }

        [TestMethod]
        public async Task ShouldProcessTheMessageSuccesfully_WhenIndexProcessorAvailableForGiveIndexWriterType()
        {
            Message message = new Message();
            string jobId = Guid.NewGuid().ToString();
            message.UserProperties["jobId"] = jobId;
            message.UserProperties["index-writer-type"] = SearchIndexWriterTypes.ProviderCalculationResultsIndexWriter;

            _jobManagement.GetJobById(jobId).Returns(new JobViewModel() { Id = jobId });
            _searchIndexProcessorFactory.CreateProcessor(SearchIndexWriterTypes.ProviderCalculationResultsIndexWriter)
                .Returns(_searchIndexProcessor);

            await _service.CreateSearchIndex(message);

            _searchIndexProcessorFactory
                .Received(1)
                .CreateProcessor(SearchIndexWriterTypes.ProviderCalculationResultsIndexWriter);

            await _searchIndexProcessor
                .Received(1)
                .Process(message);

        }
    }
}
