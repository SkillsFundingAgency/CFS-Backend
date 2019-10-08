using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
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
        private const string JobType = "ApproveResults";
        private IJobTracker _jobTracker;
        private IApproveService _approveService;
        private IPublishedFundingDataService _publishedFundingDataService;
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;
        private ILogger _logger;
        private Message _message;
        private string _jobId;
        private string _userId;
        private string _userName;


        [TestInitialize]
        public void SetUp()
        {
            _jobTracker = Substitute.For<IJobTracker>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _publishedProviderStatusUpdateService = Substitute.For<IPublishedProviderStatusUpdateService>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            _logger = Substitute.For<ILogger>();

            _approveService = new ApproveService(_publishedProviderStatusUpdateService,
                _publishedFundingDataService,
                _publishedProviderIndexerService,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _jobTracker,
                _logger);

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
            AndTheJobStartWasTrackedSuccessfully();

            await WhenTheResultsAreApproved();

            ThenTheJobStartWasTracked();
            AndTheJobEndWasTracked();
        }

        [TestMethod]
        public async Task ApproveResults_ExitsEarlyIfUnableToStartTrackingJob()
        {
            GivenTheMessageHasAJobId();

            await WhenTheResultsAreApproved();

            ThenTheJobStartWasTracked();
            AndTheJobEndWasNotTracked();
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

            GivenTheMessageHasTheSpecificationId(specificationId);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, expectedPublishedProviders);
            AndTheJobStartWasTrackedSuccessfully();

            await WhenTheResultsAreApproved();

            ThenThePublishedProvidersWereApproved(expectedPublishedProviders);
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

        private void AndTheMessageIsOtherwiseValid()
        {
            GivenTheMessageHasAJobId();
        }

        private void AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(string specificationId, params PublishedProvider[] publishedProviders)
        {
            _publishedFundingDataService.GetPublishedProvidersForApproval(specificationId)
                .Returns(publishedProviders);
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

        private void AndTheJobStartWasTrackedSuccessfully()
        {
            _jobTracker.TryStartTrackingJob(_jobId, JobType)
                .Returns(true);
        }

        private void ThenTheJobStartWasTracked()
        {
            _jobTracker
                .Received(1)
                .TryStartTrackingJob(_jobId, JobType);
        }

        private void AndTheJobEndWasTracked()
        {
            _jobTracker
                .Received(1)
                .CompleteTrackingJob(_jobId);
        }

        private void AndTheJobEndWasNotTracked()
        {
            _jobTracker
                .Received(0)
                .CompleteTrackingJob(_jobId);
        }

        private PublishedProvider NewPublishedProvider()
        {
            return new PublishedProvider();
        }
    }
}