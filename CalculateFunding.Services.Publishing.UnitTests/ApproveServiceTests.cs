using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ApproveServiceTests
    {
        private IApproveResultsJobTracker _jobTracker;
        private ISpecificationService _specificationService;
        private IApproveService _approveService;
        private IPublishedFundingRepository _publishedFundingRepository;

        private Message _message;
        private string _jobId;


        [TestInitialize]
        public void SetUp()
        {
            _specificationService = Substitute.For<ISpecificationService>();
            _jobTracker = Substitute.For<IApproveResultsJobTracker>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();

            _approveService = new ApproveService(Substitute.For<IPublishedProviderStatusUpdateService>(),
                _publishedFundingRepository,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _specificationService,
                _jobTracker);

            _jobId = new RandomString();
            _message = new Message();

            //need to setup an none empty collection to get past all the stubbed code
            //TODO; obvs once the service method proper is put under test revisit this
            _publishedFundingRepository.GetLatestPublishedProvidersBySpecification("")
                .Returns(new[] {new PublishedProvider()});
        }

        [TestMethod]
        public async Task SpecificationQueryMethodDelegatesToSpecificationService()
        {
            string specificationId = new RandomString();
            SpecificationSummary expectedSpecificationSummary = new SpecificationSummary();

            GivenTheSpecificationSummaryForId(specificationId, expectedSpecificationSummary);

            SpecificationSummary response = await _approveService.GetSpecificationSummaryById(specificationId);

            response
                .Should()
                .BeSameAs(expectedSpecificationSummary);
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

        private void GivenTheMessageHasAJobId()
        {
            GivenTheUserProperty("jobId", _jobId);
        }

        private void GivenTheUserProperty(string key, string value)
        {
            _message.UserProperties.Add(key, value);
        }

        private async Task WhenTheResultsAreApproved()
        {
            await _approveService.ApproveResults(_message);
        }

        private void GivenTheSpecificationSummaryForId(string specificationId, SpecificationSummary specificationSummary)
        {
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);
        }

        private void AndTheJobStartWasTrackedSuccessfully()
        {
            _jobTracker.TryStartTrackingJob(_jobId)
                .Returns(true);
        }

        private void ThenTheJobStartWasTracked()
        {
            _jobTracker
                .Received(1)
                .TryStartTrackingJob(_jobId);
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
    }
}