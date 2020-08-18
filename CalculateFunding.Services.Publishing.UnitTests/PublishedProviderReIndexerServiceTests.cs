using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderReIndexerServiceTests
    {
        private PublishedProviderReIndexerService _service;
        private ISearchRepository<PublishedProviderIndex> _searchRepository;
        private IPublishedFundingRepository _publishedFundingRepository;
        private IJobManagement _jobManagement;
        private const string JobId = "jobId";
        private const string SpecificationId = "specification-id";

        [TestInitialize]
        public void SetUp()
        {
            _searchRepository = Substitute.For<ISearchRepository<PublishedProviderIndex>>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _jobManagement = Substitute.For<IJobManagement>();

            _service = new PublishedProviderReIndexerService(_searchRepository,
                new ResiliencePolicies
                {
                    PublishedProviderSearchRepository = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _publishedFundingRepository,
                _jobManagement,
                Substitute.For<ILogger>());
        }

        [TestMethod]
        public async Task ReIndexesAllPublishedProvidersIfJobCanBeProcessed()
        {
            Message message = new Message();

            message.UserProperties.Add("jobId", JobId);

            _jobManagement.RetrieveJobAndCheckCanBeProcessed(JobId)
                .Returns(NewJobViewModel());

            await _service.Run(message);

            await _jobManagement
                .Received(1)
                .UpdateJobStatus(JobId, 0, 0, null, null);

            await _publishedFundingRepository
                .Received(1)
                .AllPublishedProviderBatchProcessing(Arg.Any<Func<List<PublishedProvider>, Task>>(), Arg.Is(1000), null);

            await _jobManagement
                .Received(1)
                .UpdateJobStatus(JobId, 0, 0, true, null);
        }

        [TestMethod]
        public async Task ReIndexesAllSpecificationPublishedProvidersIfJobCanBeProcessed()
        {
            Message message = new Message();

            message.UserProperties.Add("jobId", JobId);
            message.UserProperties.Add("specification-id", SpecificationId);

            _jobManagement.RetrieveJobAndCheckCanBeProcessed(JobId)
                .Returns(NewJobViewModel());

            await _service.Run(message);

            await _jobManagement
                .Received(1)
                .UpdateJobStatus(JobId, 0, 0, null, null);

            await _publishedFundingRepository
                .Received(1)
                .AllPublishedProviderBatchProcessing(Arg.Any<Func<List<PublishedProvider>, Task>>(), Arg.Is(1000), Arg.Is(SpecificationId));

            await _jobManagement
                .Received(1)
                .UpdateJobStatus(JobId, 0, 0, true, null);
        }

        [TestMethod]
        public void IfJobCannotBeProcessedAnExceptionIsThrown()
        {
            Message message = new Message();

            message.UserProperties.Add("jobId", JobId);

            _jobManagement.RetrieveJobAndCheckCanBeProcessed(JobId)
                .Returns<JobViewModel>(x => { throw new Exception(); });

            Func<Task> invocation = async() => await _service.Run(message);

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage("Job cannot be run");
        }

        private static JobViewModel NewJobViewModel(Action<JobViewModelBuilder> setup = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setup?.Invoke(jobViewModelBuilder);

            return jobViewModelBuilder.Build();
        }
    }
}