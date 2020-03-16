using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ApproveServiceTests
    {
        private const string JobType = "ApproveResults";
        private IJobManagement _jobManagement;
        private IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private IApproveService _approveService;
        private IPublishedFundingDataService _publishedFundingDataService;
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;
        private IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;
        private ITransactionFactory _transactionFactory;
        private IPublishedProviderVersionService _publishedProviderVersionService;
        private ITransactionResiliencePolicies _transactionResiliencePolicies;
        private ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private IJobsApiClient _jobsApiClient;
        private ILogger _logger;
        private Message _message;
        private string _jobId;
        private JobViewModel _job;
        private string _userId;
        private string _userName;


        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _jobsApiClient = Substitute.For<IJobsApiClient>();
            _jobManagement = new JobManagement(_jobsApiClient, _logger, new JobManagementResiliencePolicies { JobsApiClient = Policy.NoOpAsync() });
            _calculationEngineRunningChecker = Substitute.For<ICalculationEngineRunningChecker>();
            _prerequisiteCheckerLocator = Substitute.For<IPrerequisiteCheckerLocator>();
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.Approve)
                .Returns(new ApprovePrerequisiteChecker(_calculationEngineRunningChecker, _jobManagement, _logger));
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _publishedProviderStatusUpdateService = Substitute.For<IPublishedProviderStatusUpdateService>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            _generateCsvJobsLocator = Substitute.For<IGeneratePublishedFundingCsvJobsCreationLocator>();
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
                _generateCsvJobsLocator);

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
        public void CheckPrerequisitesForSpecificationToBeApproved_WhenPreReqsValidationErrors_ThrowsException()
        {
            string specificationId = NewRandomString();

            GivenTheMessageHasTheSpecificationId(specificationId);
            AndTheMessageIsOtherwiseValid();
            AndCalculationEngineRunning(specificationId);
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();

            Func<Task> invocation = WhenTheResultsAreApproved;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Specification with id: '{specificationId} has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { "Calculation engine is still running" };

            _logger.Received(1)
                .Error("Calculation engine is still running");

            _jobsApiClient.Received(1)
                .AddJobLog(Arg.Is(_jobId), Arg.Is<JobLogUpdateModel>(_ => _.CompletedSuccessfully == false && _.Outcome == string.Join(", ", prereqValidationErrors)));
        }

        [TestMethod]
        public async Task ApproveResults_IfUpdateProviderStatusExceptionsTransactionCompensates()
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
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();
            AndNumberOfApprovedPublishedProvidersThrowsException(expectedPublishedProviders);

            Func<Task> invocation = WhenTheResultsAreApproved;

            invocation
                .Should()
                .Throw<Exception>();

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>());
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
            string specificationId = NewRandomString();

            GivenTheMessageHasAJobId();
            GivenTheMessageHasTheSpecificationId(specificationId);
            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();

            await WhenTheResultsAreApproved();
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

            GivenTheMessageHasTheSpecificationId(specificationId);
            GivenTheMessageHasTheFundingLineCode(fundingLineCode);
            AndTheMessageIsOtherwiseValid();
            AndTheSpecificationHasTheHeldUnApprovedPublishedProviders(specificationId, expectedPublishedProviders);

            AndRetrieveJobAndCheckCanBeProcessedSuccessfully();
            AndNumberOfApprovedPublishedProvidersIsReturned(expectedPublishedProviders);

            await WhenTheResultsAreApproved();

            ThenThePublishedProvidersWereApproved(expectedPublishedProviders);
            AndTheCsvGenerationJobsWereCreated();
        }

        private void AndTheCsvGenerationJobsWereCreated()
        {
            _generateCsvJobsLocator.Received(1)
                .GetService(Arg.Any<GeneratePublishingCsvJobsCreationAction>());
        }

        private void AndCalculationEngineRunning(string specificationId)
        {
            string[] jobTypes = new string[] { JobConstants.DefinitionNames.RefreshFundingJob,
                JobConstants.DefinitionNames.PublishProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob };

            _calculationEngineRunningChecker
                .IsCalculationEngineRunning(Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt))))
                .Returns(true);
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

        private void AndNumberOfApprovedPublishedProvidersIsReturned(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProviderStatusUpdateService
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.SequenceEqual(publishedProviders)),
                    Arg.Is<Reference>(auth => auth.Id == _userId &&
                                              auth.Name == _userName),
                    PublishedProviderStatus.Approved,
                    _jobId)
                .Returns(publishedProviders.Count());
        }

        private void AndNumberOfApprovedPublishedProvidersThrowsException(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProviderStatusUpdateService
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.SequenceEqual(publishedProviders)),
                    Arg.Is<Reference>(auth => auth.Id == _userId &&
                                              auth.Name == _userName),
                    PublishedProviderStatus.Approved,
                    _jobId)
                .Throws(new Exception());
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

            _jobsApiClient.GetJobById(_jobId)
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, _job));
        }

        private void AndRetrieveJobAndCheckCannotBeProcessedSuccessfully(Action<JobViewModelBuilder> setUp = null)
        {
            _jobsApiClient.GetJobById(_jobId)
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, null));
        }

        private void AndTheJobEndWasTracked()
        {
            _jobsApiClient
                .Received(1)
                .AddJobLog(_jobId, Arg.Is<JobLogUpdateModel>(_ => _.CompletedSuccessfully == true));
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