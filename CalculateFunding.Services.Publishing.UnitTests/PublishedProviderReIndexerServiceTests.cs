using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderReIndexerServiceTests
    {
        private PublishedProviderReIndexerService _service;
        private Mock<IPublishedFundingRepository> _publishedFundingRepository;
        private Mock<IPublishedProviderIndexerService> _publishedProviderIndexerService;
        private Mock<IJobManagement> _jobManagement;
        private const string JobId = "jobId";
        private const string SpecificationId = "specification-id";

        [TestInitialize]
        public void SetUp()
        {
            _publishedProviderIndexerService = new Mock<IPublishedProviderIndexerService>();
            _publishedFundingRepository = new Mock<IPublishedFundingRepository>();
            _jobManagement = new Mock<IJobManagement>();

            _service = new PublishedProviderReIndexerService(_publishedProviderIndexerService.Object,
                new ResiliencePolicies
                {
                    PublishedProviderSearchRepository = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _publishedFundingRepository.Object,
                _jobManagement.Object,
                Mock.Of<ILogger>());
        }

        [TestMethod]
        public async Task ReIndexesAllPublishedProvidersIfJobCanBeProcessed()
        {
            Message message = new Message();

            message.UserProperties.Add("jobId", JobId);

            _jobManagement.Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(JobId))
                .ReturnsAsync(NewJobViewModel());

            string specificationIdProvided = null;

            _publishedFundingRepository.Setup(_ => _.AllPublishedProviderBatchProcessing(It.IsAny<Func<List<PublishedProvider>, Task>>(), It.IsAny<int>(), It.IsAny<string>()))
                .Callback<Func<List<PublishedProvider>, Task>, int, string>((cb, batchSize, specId) =>
                {
                    cb.Invoke(new List<PublishedProvider>());

                    specificationIdProvided = specId;
                });


            await _service.Run(message);

            _jobManagement.Verify(_ => _.UpdateJobStatus(JobId, 0, 0, null, null), Times.Once);

            _publishedFundingRepository.Verify(_ => _.AllPublishedProviderBatchProcessing(It.IsAny<Func<List<PublishedProvider>, Task>>(), 1000, null), Times.Once);

            _jobManagement.Verify(_ => _.UpdateJobStatus(JobId, 0, 0, true, null), Times.Once);

            _publishedProviderIndexerService.Verify(_ => _.IndexPublishedProviders(It.IsAny<IEnumerable<PublishedProviderVersion>>()));

            specificationIdProvided
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task ReIndexesAllSpecificationPublishedProvidersIfJobCanBeProcessed()
        {
            Message message = new Message();

            message.UserProperties.Add("jobId", JobId);
            message.UserProperties.Add("specification-id", SpecificationId);

            _jobManagement.Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(JobId))
                 .ReturnsAsync(NewJobViewModel());

            string specificationIdProvided = null;


            _publishedFundingRepository.Setup(_ => _.AllPublishedProviderBatchProcessing(It.IsAny<Func<List<PublishedProvider>, Task>>(), It.IsAny<int>(), It.IsAny<string>()))
                 .Callback<Func<List<PublishedProvider>, Task>, int, string>((cb, batchSize, specId) =>
                 {
                     cb.Invoke(new List<PublishedProvider>());

                     specificationIdProvided = specId;
                 });


            await _service.Run(message);

            _jobManagement.Verify(_ => _.UpdateJobStatus(JobId, 0, 0, null, null), Times.Once);

            _publishedFundingRepository.Verify(_ => _.AllPublishedProviderBatchProcessing(It.IsAny<Func<List<PublishedProvider>, Task>>(), 1000, SpecificationId), Times.Once);

            _publishedProviderIndexerService.Verify(_ => _.IndexPublishedProviders(It.IsAny<IEnumerable<PublishedProviderVersion>>()));

            specificationIdProvided
                .Should()
                .Be(SpecificationId);
        }

        [TestMethod]
        public void IfJobCannotBeProcessedAnExceptionIsThrown()
        {
            Message message = new Message();

            message.UserProperties.Add("jobId", JobId);

            _jobManagement.Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(JobId)).Throws<NonRetriableException>();

            Func<Task> invocation = async () => await _service.Run(message);

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage($"Job can not be run '{JobId}'");
        }

        private static JobViewModel NewJobViewModel(Action<JobViewModelBuilder> setup = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setup?.Invoke(jobViewModelBuilder);

            return jobViewModelBuilder.Build();
        }
    }
}