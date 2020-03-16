using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ApproveServiceTests
    {
        private IJobManagement _jobManagement;
        private IApprovePrerequisiteChecker _approvePrerequisiteChecker;
        private IApproveService _approveService;
        private IPublishedFundingDataService _publishedFundingDataService;
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;
        private IGeneratePublishedFundingCsvJobsCreation _publishedFundingCsvJobsCreation;
        private IGeneratePublishedFundingCsvJobsCreationLocator _generatePublishedFundingCsvJobsCreationLocator;
        private ILogger _logger;
        private Message _message;
        private string _jobId;
        private JobViewModel _job;
        private string _userId;
        private string _userName;

        [TestInitialize]
        public void SetUp()
        {
            _jobManagement = Substitute.For<IJobManagement>();
            _approvePrerequisiteChecker = Substitute.For<IApprovePrerequisiteChecker>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _publishedProviderStatusUpdateService = Substitute.For<IPublishedProviderStatusUpdateService>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            _publishedFundingCsvJobsCreation = Substitute.For<IGeneratePublishedFundingCsvJobsCreation>();
            _generatePublishedFundingCsvJobsCreationLocator = Substitute.For<IGeneratePublishedFundingCsvJobsCreationLocator>();
            _generatePublishedFundingCsvJobsCreationLocator
                .GetService(Arg.Any<GeneratePublishingCsvJobsCreationAction>())
                .Returns(_publishedFundingCsvJobsCreation);

            _logger = Substitute.For<ILogger>();

            _approveService = new ApproveService(_publishedProviderStatusUpdateService,
                _publishedFundingDataService,
                _publishedProviderIndexerService,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _approvePrerequisiteChecker,
                _jobManagement,
                _logger,
                _generatePublishedFundingCsvJobsCreationLocator);

            _jobId = NewRandomString();
            _userId = NewRandomString();
            _userName = NewRandomString();

            _message = new Message();

            SetUserProperty("user-id", _userId);
            SetUserProperty("user-name", _userName);

            _publishedFundingDataService.GetPublishedProvidersForApproval(Arg.Any<string>())
                .Returns(new[] { new PublishedProvider() });
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        [TestMethod]
        public void ApproveResults_ThrowsExceptionIfNoJobIdInMessage()
        {
            Func<Task> invocation = WhenTheResultsAreApproved;

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("jobId");
        }

        [TestMethod]
        public async Task ApproveResults_TracksStartAndCompletionOfJobByJobIdInMessageProperties()
        {
            GivenTheMessageHasAJobId();
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();

            await WhenTheResultsAreApproved();

            ThenRetrieveJobAndCheckCanBeProcessed();
            AndTheJobEndWasTracked();
        }

        [TestMethod]
        public void ApproveResults_ExitsEarlyIfUnableToStartTrackingJob()
        {
            GivenTheMessageHasAJobId();
            AndRetrieveJobAndCheckCannotBeProcessedSuccessfully();

            Func<Task> invocation = WhenTheResultsAreApproved;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage("Job can not be run");
        }

        [TestMethod]
        public async Task ApproveResults_SendsHeldAndUpdatedPublishedProvidersBySpecIdToBeApproved()
        {
            PublishedProvider[] expectedPublishedProviders =
            {
                NewPublishedProvider(),
                NewPublishedProvider(),
                NewPublishedProvider(),
                NewPublishedProvider(),
                NewPublishedProvider(),
                NewPublishedProvider()
            };

            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();
            IEnumerable<string> fundingLineCodes = new[] { fundingLineCode };

            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheFundingLineCode(fundingLineCode);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, expectedPublishedProviders);
            AndThePublishedProvidersFundingLines(specificationId, fundingLineCodes);

            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();

            await WhenTheResultsAreApproved();

            ThenThePublishedProvidersWereApproved(expectedPublishedProviders);
            await AndTheCsvGenerationJobsWereCreated(specificationId, fundingLineCodes);
        }

        private async Task AndTheCsvGenerationJobsWereCreated(string specificationId, IEnumerable<string> fundingLineCodes)
        {
            await _publishedFundingCsvJobsCreation
                .Received(1)
                .CreateJobs(Arg.Is(specificationId),
                    Arg.Any<string>(),
                    Arg.Is<Reference>(usr => usr != null &&
                                             usr.Id == _userId &&
                                             usr.Name == _userName),
                    Arg.Is<IEnumerable<string>>(flc => flc.Count() == fundingLineCodes.Count() && flc.SequenceEqual(fundingLineCodes)));
        }

        private void GivenTheMessageHasAJobId()
        {
            GivenTheUserProperty("jobId", _jobId);
        }

        private void GivenTheUserProperty(string key, string value)
        {
            _message.UserProperties.Add(key, value);
        }

        private void SetUserProperty(string key, string value)
        {
            GivenTheUserProperty(key, value);
        }

        private void GivenTheMessageHasTheSpecificationId(string specificationId)
        {
            GivenTheUserProperty("specification-id", specificationId);
        }

        private void GivenTheMessageHasTheFundingLineCode(string fundingLineCode)
        {
            GivenTheUserProperty("funding-line-code", fundingLineCode);
        }

        private void AndTheMessageIsOtherwiseValid()
        {
            GivenTheMessageHasAJobId();
        }

        private void AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(string specificationId, params PublishedProvider[] publishedProviders)
        {
            _publishedFundingDataService.GetPublishedProvidersForApproval(specificationId)
                .Returns(publishedProviders);
        }

        private void AndThePublishedProvidersFundingLines(string specificationId, IEnumerable<string> fundingLineCodes)
        {
            _publishedFundingDataService.GetPublishedProviderFundingLines(specificationId)
                .Returns(fundingLineCodes);
        }

        private async Task WhenTheResultsAreApproved()
        {
            await _approveService.ApproveResults(_message);
        }

        private void ThenThePublishedProvidersWereApproved(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProviderStatusUpdateService
                .Received(1)
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.SequenceEqual(publishedProviders)),
                    Arg.Is<Reference>(auth => auth.Id == _userId &&
                                              auth.Name == _userName),
                    PublishedProviderStatus.Approved,
                    _jobId);
        }

        private void AndRetrieveJobAndCheckCanBeProcessedSuccessfully(Action<JobViewModelBuilder> setUp = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setUp?.Invoke(jobViewModelBuilder);

            _job = jobViewModelBuilder.Build();

            _jobManagement.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Returns(_job);
        }

        private void AndRetrieveJobAndCheckCannotBeProcessedSuccessfully(Action<JobViewModelBuilder> setUp = null)
        {
            _jobManagement.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Returns(Task.FromException<JobViewModel>(new Exception()));
        }

        private void ThenRetrieveJobAndCheckCanBeProcessed()
        {
            _jobManagement
                .Received(1)
                .RetrieveJobAndCheckCanBeProcessed(_jobId);
        }

        private void AndTheJobEndWasTracked()
        {
            _jobManagement
                .Received(1)
                .UpdateJobStatus(_jobId, 0, 0, true, null);
        }

        private void AndTheJobEndWasNotTracked()
        {
            _jobManagement
                .Received(0)
                .UpdateJobStatus(_jobId, 0, 0, true, null);
        }

        private PublishedProvider NewPublishedProvider()
        {
            return new PublishedProvider();
        }
    }
}