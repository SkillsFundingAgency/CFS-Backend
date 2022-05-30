using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class ApproveServiceTests
    {
        private IJobManagement _jobManagement;
        private IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private IPublishedFundingDataService _publishedFundingDataService;
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;
        private IPublishedFundingCsvJobsService _publishFundingCsvJobsService;
        private ITransactionFactory _transactionFactory;
        private IPublishedProviderVersionService _publishedProviderVersionService;
        private ITransactionResiliencePolicies _transactionResiliencePolicies;
        private IJobsRunning _jobsRunning;
        private ILogger _logger;
        private Message _message;
        private string _jobId;
        private string _correlationId;
        private JobViewModel _job;
        private string _userId;
        private string _userName;

        private ApproveService _approveService;

        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _jobManagement = Substitute.For<IJobManagement>();
            _jobsRunning = Substitute.For<IJobsRunning>();
            _prerequisiteCheckerLocator = Substitute.For<IPrerequisiteCheckerLocator>();
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.ApproveAllProviders)
                .Returns(new ApproveAllProvidersPrerequisiteChecker(_jobsRunning, _jobManagement, _logger));
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.ApproveBatchProviders)
                .Returns(new ApproveBatchProvidersPrerequisiteChecker(_jobsRunning, _jobManagement, _logger));
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _publishedProviderStatusUpdateService = Substitute.For<IPublishedProviderStatusUpdateService>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            _publishFundingCsvJobsService = Substitute.For<IPublishedFundingCsvJobsService>();
            _transactionResiliencePolicies = new TransactionResiliencePolicies { TransactionPolicy = Policy.NoOpAsync() };
            _transactionFactory = new TransactionFactory(_logger, _transactionResiliencePolicies);
            _publishedProviderVersionService = Substitute.For<IPublishedProviderVersionService>();

            _approveService = new ApproveService(_publishedProviderStatusUpdateService,
                _publishedFundingDataService,
                _publishedProviderIndexerService,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _prerequisiteCheckerLocator,
                _jobManagement,
                _logger,
                _transactionFactory,
                _publishedProviderVersionService,
                _publishFundingCsvJobsService);

            _jobId = NewRandomString();
            _correlationId = NewRandomString();
            _userId = NewRandomString();
            _userName = NewRandomString();

            _message = new Message();

            SetUserProperty("user-id", _userId);
            SetUserProperty("user-name", _userName);
            SetUserProperty("jobId", null);

            _publishedFundingDataService.GetPublishedProvidersForApproval(Arg.Any<string>(), Arg.Any<string[]>())
                .Returns(new[] { new PublishedProvider() });
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        [TestMethod]
        public void CheckPrerequisitesForApproveSpecificationToBeApproved_WhenPreReqsValidationErrors_ThrowsException()
        {
            string specificationId = NewRandomString();

            GivenTheMessageHasTheSpecificationId(specificationId);
            AndTheMessageIsOtherwiseValid();
            AndCalculationEngineApproveSpecificationRunning(specificationId);
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully(_ => _.WithJobId(_jobId));

            Func<Task> invocation = WhenAllProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Approve All Providers with specification id: '{specificationId}' has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { $"{JobConstants.DefinitionNames.RefreshFundingJob} is still running" };

            _logger.Received(1)
                .Error($"{JobConstants.DefinitionNames.RefreshFundingJob} is still running");

            _jobManagement
                .Received(1)
                .UpdateJobStatus(_jobId, 0, false, string.Join(", ", prereqValidationErrors));
        }

        [TestMethod]
        public void CheckPrerequisitesForApproveProvidersSpecificationToBeApproved_WhenPreReqsValidationErrors_ThrowsException()
        {
            string specificationId = NewRandomString();
            string providerId = NewRandomString();
            string[] providers = new[] { providerId };

            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheApproveProvidersRequest(BuildApproveProvidersRequest(_=>_.WithProviders(providers)));
            AndTheMessageIsOtherwiseValid();
            AndCalculationEngineApproveProviderRunning(specificationId);
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully(_ => _.WithJobId(_jobId));

            Func<Task> invocation = WhenBatchProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Approve Batch Providers with specification id: '{specificationId}' has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { $"{JobConstants.DefinitionNames.RefreshFundingJob} is still running" };

            _logger.Received(1)
                .Error($"{JobConstants.DefinitionNames.RefreshFundingJob} is still running");

            _jobManagement
                .Received(1)
                .UpdateJobStatus(_jobId, 0, false, string.Join(", ", prereqValidationErrors));
        }

        [TestMethod]
        public async Task ApproveAllProvidersResults_IfUpdateProviderStatusExceptionsTransactionCompensates()
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

            GivenTheMessageHasACorrelationId();
            GivenTheMessageHasTheSpecificationId(specificationId);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, null, expectedPublishedProviders);
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully(_ => _.WithJobId(_jobId));
            AndNumberOfApprovedPublishedProvidersThrowsException(expectedPublishedProviders);

            Func<Task> invocation = WhenAllProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<Exception>();

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(specificationId), Arg.Is(_jobId));
        }

        [TestMethod]
        public async Task ApproveBatchProvidersResults_IfUpdateProviderStatusExceptionsTransactionCompensates()
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
            string providerId = NewRandomString();
            string[] providerIds = new[] { providerId };

            GivenTheMessageHasACorrelationId();
            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheApproveProvidersRequest(BuildApproveProvidersRequest(_ => _.WithProviders(providerIds)));
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, providerIds, expectedPublishedProviders);
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully(_ => _.WithJobId(_jobId));
            AndNumberOfApprovedPublishedProvidersThrowsException(expectedPublishedProviders);

            Func<Task> invocation = WhenBatchProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<Exception>();

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(specificationId), Arg.Is(_jobId));
        }

        [TestMethod]
        public void ApproveAllProvidersResults_ThrowsExceptionIfNoJobIdInMessage()
        {
            Func<Task> invocation = WhenAllProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage("Missing job id");
        }

        [TestMethod]
        public void ApproveBatchProvidersResults_ThrowsExceptionIfNoJobIdInMessage()
        {
            Func<Task> invocation = WhenBatchProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage("Missing job id");
        }

        [TestMethod]
        public async Task ApproveAllProvidersResults_TracksStartAndCompletionOfJobByJobIdInMessageProperties()
        {
            string specificationId = NewRandomString();

            GivenTheMessageHasAJobId();
            GivenTheMessageHasTheSpecificationId(specificationId);
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();

            await WhenAllProvidersResultsAreApproved();
            AndTheJobEndWasTracked();
        }

        [TestMethod]
        public async Task ApproveBatchProvidersResults_TracksStartAndCompletionOfJobByJobIdInMessageProperties()
        {
            string specificationId = NewRandomString();
            string providerId = NewRandomString();
            string[] providerIds = new[] { providerId };

            GivenTheMessageHasAJobId();
            GivenTheMessageHasACorrelationId();
            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheApproveProvidersRequest(BuildApproveProvidersRequest(_ => _.WithProviders(providerIds)));
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();

            await WhenBatchProvidersResultsAreApproved();
            AndTheJobEndWasTracked();
        }

        [TestMethod]
        public void ApproveAllProvidersResults_ExitsEarlyIfUnableToStartTrackingJob()
        {
            GivenTheMessageHasAJobId();
            AndRetrieveJobAndCheckCannotBeProcessedSuccessfully(new JobNotFoundException(string.Empty, string.Empty));

            Func<Task> invocation = WhenAllProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage($"Could not find the job with id: '{_jobId}'");
        }

        [TestMethod]
        public void ApproveBatchProvidersResults_ExitsEarlyIfUnableToStartTrackingJob()
        {
            GivenTheMessageHasAJobId();            
            AndRetrieveJobAndCheckCannotBeProcessedSuccessfully(new Exception());

            Func<Task> invocation = WhenBatchProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage($"Job can not be run '{_jobId}'");
        }

        [TestMethod]
        public void ApproveAllProvidersResults_ExitsEarlyIfAnyOfThePublishedProvidersAreInError()
        {
            PublishedProvider[] expectedPublishedProviders =
            {
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv =>
                    ppv.WithErrors(NewPublishedProviderError())))),
                NewPublishedProvider(),
            };

            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();

            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheFundingLineCode(fundingLineCode);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, null, expectedPublishedProviders);

            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();
            AndNumberOfApprovedPublishedProvidersIsReturned(expectedPublishedProviders);

            Func<Task> invocation = WhenAllProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<NonRetriableException>();
        }

        [TestMethod]
        public void ApproveBatchProvidersResults_ExitsEarlyIfAnyOfThePublishedProvidersAreInError()
        {
            PublishedProvider[] expectedPublishedProviders =
            {
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv =>
                    ppv.WithErrors(NewPublishedProviderError())))),
                NewPublishedProvider(),
            };

            string specificationId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string providerId = NewRandomString();
            string[] providerIds = new[] { providerId };

            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheApproveProvidersRequest(BuildApproveProvidersRequest(_ => _.WithProviders(providerIds)));
            GivenTheMessageHasTheFundingLineCode(fundingLineCode);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, providerIds, expectedPublishedProviders);

            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();
            AndNumberOfApprovedPublishedProvidersIsReturned(expectedPublishedProviders);

            Func<Task> invocation = WhenBatchProvidersResultsAreApproved;

            invocation
                .Should()
                .Throw<NonRetriableException>();
        }

        [TestMethod]
        public async Task ApproveAllProvidersResults_SendsHeldAndUpdatedPublishedProvidersBySpecIdToBeApproved()
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

            GivenTheMessageHasACorrelationId();
            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheFundingLineCode(fundingLineCode);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, null, expectedPublishedProviders);

            AndRetrieveJobAndCheckCanBeProcessedSuccessfully(_ => _.WithJobId(_jobId));
            AndNumberOfApprovedPublishedProvidersIsReturned(expectedPublishedProviders);

            await WhenAllProvidersResultsAreApproved();

            ThenThePublishedProvidersWereApproved(expectedPublishedProviders);
            AndTheCsvGenerationJobsWereCreated(specificationId, expectedPublishedProviders.First().Current.FundingPeriodId);
        }

        [TestMethod]
        public async Task ApproveBatchProvidersResults_SendsHeldAndUpdatedPublishedProvidersBySpecIdToBeApproved()
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
            string publishedProviderId = NewRandomString();
            string[] publishedProviderIds = new[] { publishedProviderId };
            
            GivenTheMessageHasACorrelationId();
            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheApproveProvidersRequest(BuildApproveProvidersRequest(_ => _.WithProviders(publishedProviderIds)));
            GivenTheMessageHasTheFundingLineCode(fundingLineCode);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, publishedProviderIds, expectedPublishedProviders);

            AndRetrieveJobAndCheckCanBeProcessedSuccessfully(_ => _.WithJobId(_jobId));
            AndNumberOfApprovedPublishedProvidersIsReturned(expectedPublishedProviders);

            await WhenBatchProvidersResultsAreApproved();

            ThenThePublishedProvidersWereApproved(expectedPublishedProviders);
            AndTheCsvGenerationJobsWereCreated(specificationId, expectedPublishedProviders.First().Current.FundingPeriodId);
        }

        private void AndTheCsvGenerationJobsWereCreated(string specificationId, string fundingPeriodId)
        {
            _publishFundingCsvJobsService.Received(1)
                .GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction.Approve,
                        Arg.Is(specificationId),
                        Arg.Is(fundingPeriodId),
                        Arg.Any<IEnumerable<string>>(),
                        Arg.Any<string>(),
                        Arg.Any<Reference>(),
                        true);
        }

        private void AndCalculationEngineApproveProviderRunning(string specificationId)
        {
            string[] jobTypes = new string[] {
                JobConstants.DefinitionNames.PublishedFundingUndoJob,
                JobConstants.DefinitionNames.RefreshFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob
            };

            _jobsRunning
                .GetJobTypes(Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt))))
                .Returns(new[] { JobConstants.DefinitionNames.RefreshFundingJob });
        }

        private void AndCalculationEngineApproveSpecificationRunning(string specificationId)
        {
            string[] jobTypes = new string[] { 
                JobConstants.DefinitionNames.PublishedFundingUndoJob,
                JobConstants.DefinitionNames.RefreshFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob
            };

            _jobsRunning
                .GetJobTypes(Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt))))
                .Returns(new[] { JobConstants.DefinitionNames.RefreshFundingJob });
        }

        private void GivenTheMessageHasAJobId()
        {
            GivenTheUserProperty("jobId", _jobId);
        }

        private void GivenTheMessageHasACorrelationId()
        {
            GivenTheUserProperty("sfa-correlationId", _correlationId);
        }

        private void GivenTheUserProperty(string key, string value)
        {
            _message.UserProperties.Remove(key);
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

        private void GivenTheMessageHasTheApproveProvidersRequest(PublishedProviderIdsRequest approveProvidersRequest)
        {
            _message.Body = Encoding.UTF8.GetBytes(JsonExtensions.AsJson(approveProvidersRequest));
        }

        private void GivenTheMessageHasTheFundingLineCode(string fundingLineCode)
        {
            GivenTheUserProperty("funding-line-code", fundingLineCode);
        }

        private void AndTheMessageIsOtherwiseValid()
        {
            GivenTheMessageHasAJobId();
        }

        private void AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(string specificationId, string[] publishedProviderIds = null, params PublishedProvider[] publishedProviders)
        {
            _publishedFundingDataService.GetPublishedProvidersForApproval(specificationId, Arg.Is<string[]>(_ => _ == null || _.SequenceEqual(publishedProviderIds)))
                .Returns(publishedProviders);
        }

        private void AndNumberOfApprovedPublishedProvidersIsReturned(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProviderStatusUpdateService
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.SequenceEqual(publishedProviders)),
                    Arg.Is<Reference>(auth => auth.Id == _userId &&
                                              auth.Name == _userName),
                    PublishedProviderStatus.Approved,
                    _jobId,
                    _correlationId)
                .Returns(publishedProviders.Count());
        }

        private void AndNumberOfApprovedPublishedProvidersThrowsException(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProviderStatusUpdateService
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.SequenceEqual(publishedProviders)),
                    Arg.Is<Reference>(auth => auth.Id == _userId &&
                                              auth.Name == _userName),
                    PublishedProviderStatus.Approved,
                    _jobId,
                    _correlationId)
                .Throws(new Exception());
        }

        private async Task WhenBatchProvidersResultsAreApproved()
        {
            await _approveService.Run(_message, async () =>
            {
                await _approveService.ApproveResults(_message, batched: true);
            });
        }

        private async Task WhenAllProvidersResultsAreApproved()
        {
            await _approveService.Run(_message);
        }

        private void ThenThePublishedProvidersWereApproved(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProviderStatusUpdateService
                .Received(1)
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.SequenceEqual(publishedProviders)),
                    Arg.Is<Reference>(auth => auth.Id == _userId &&
                                              auth.Name == _userName),
                    PublishedProviderStatus.Approved,
                    _jobId,
                    _correlationId);
        }

        private void AndRetrieveJobAndCheckCanBeProcessedSuccessfully(Action<JobViewModelBuilder> setUp = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setUp?.Invoke(jobViewModelBuilder);

            _job = jobViewModelBuilder.Build();

            _jobManagement.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Returns(_job);
        }

        private PublishedProviderIdsRequest BuildApproveProvidersRequest(Action<PublishedProviderIdsRequestBuilder> setUp = null)
        {
            PublishedProviderIdsRequestBuilder approveProvidersRequestBuilder = new PublishedProviderIdsRequestBuilder();

            setUp?.Invoke(approveProvidersRequestBuilder);

            return approveProvidersRequestBuilder.Build();
        }

        private void AndRetrieveJobAndCheckCannotBeProcessedSuccessfully(Exception exception)
        {
            _jobManagement.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Throws(exception);
        }

        private void AndTheJobEndWasTracked()
        {
            _jobManagement
                .Received(1)
                .UpdateJobStatus(_jobId, 0, 0, true, null);
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();
            
            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);
            
            return providerVersionBuilder.Build();
        }
        
        private PublishedProviderError NewPublishedProviderError() => new PublishedProviderError();
    }
}