using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class DeletePublishedProvidersServiceTests
    {
        private ICreateDeletePublishedProvidersJobs _deletePublishedProviders;
        private IPublishedFundingRepository _publishedFundingRepository;
        private IJobManagement _jobManagement;
        private IDeselectSpecificationForFundingService _deselectSpecificationForFundingService;
        private IDeletePublishedFundingBlobDocumentsService _deletePublishedFundingBlobDocumentsService;
        private IDeleteFundingSearchDocumentsService _deleteFundingSearchDocumentsService;

        private DeletePublishedProvidersService _service;

        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _jobId;

        private const string FundingStreamId = "funding-stream-id";
        private const string FundingPeriodId = "funding-period-id";
        private const string JobId = "jobId";

        private Message _message;

        [TestInitialize]
        public void SetUp()
        {
            _deletePublishedProviders = Substitute.For<ICreateDeletePublishedProvidersJobs>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _jobManagement = Substitute.For<IJobManagement>();
            _deletePublishedFundingBlobDocumentsService = Substitute.For<IDeletePublishedFundingBlobDocumentsService>();
            _deselectSpecificationForFundingService = Substitute.For<IDeselectSpecificationForFundingService>();
            _deleteFundingSearchDocumentsService = Substitute.For<IDeleteFundingSearchDocumentsService>();

            _service = new DeletePublishedProvidersService(_deletePublishedProviders,
                _publishedFundingRepository,
                new ResiliencePolicies
                {
                    PublishedProviderSearchRepository = Policy.NoOpAsync(),
                    BlobClient = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync(),
                    SpecificationsApiClient = Policy.NoOpAsync()
                },
                _jobManagement,
                _deleteFundingSearchDocumentsService,
                _deletePublishedFundingBlobDocumentsService,
                _deselectSpecificationForFundingService,
                Substitute.For<ILogger>());

            _fundingPeriodId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _jobId = NewRandomString();
        }

        [TestMethod]
        [DynamicData(nameof(QueueDeleteExceptionExamples), DynamicDataSourceType.Method)]
        public void OnlyCorrelationIdIsOptional(string fundingStreamId,
            string fundingPeriodId)
        {
            Func<Task> invocation = () => WhenADeletePublishedProvidersJobIsQueued(fundingStreamId,
                fundingPeriodId,
                null);

            invocation
                .Should()
                .ThrowAsync<Exception>();
        }

        [TestMethod]
        public async Task QueueDeleteJobDelegatesToDeleteJobCreationService()
        {
            string correlationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            await WhenADeletePublishedProvidersJobIsQueued(fundingStreamId, fundingPeriodId, correlationId);

            await _deletePublishedProviders
                .Received(1)
                .CreateJob(fundingStreamId,
                    fundingPeriodId,
                    correlationId);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoMessageSupplied()
        {
            ThenDeletingThePublishedProvidersShouldThrowArgumentNullFor("message");
        }

        [TestMethod]
        public void ThrowsExceptionIfNoFundingStreamIdInMessage()
        {
            GivenTheMessage((JobId, _jobId), (FundingPeriodId, _fundingPeriodId));

            ThenDeletingThePublishedProvidersShouldThrowArgumentNullFor("fundingStreamId");
        }

        [TestMethod]
        public void ThrowsExceptionIfNoFundingPeriodIdInMessage()
        {
            GivenTheMessage((FundingStreamId, _fundingStreamId), (JobId, _jobId));

            ThenDeletingThePublishedProvidersShouldThrowArgumentNullFor("fundingPeriodId");
        }

        [TestMethod]
        public async Task DeletePublishedProvidersJobCleansBlobsSearchIndexesCosmosCollectionsAndDeselectsSpecificationForFunding()
        {
            GivenTheMessage((JobId, _jobId), (FundingPeriodId, _fundingPeriodId), (FundingStreamId, _fundingStreamId));
            AndTheJobExistsForTheJobId();
            
            await WhenThePublishedProvidersAreDeleted();

            await ThenThePublishedProvidersAreDeleted();
            await AndThePublishedProviderVersionsAreDeleted();
            await AndThePublishedFundingsAreDeleted();
            await AndThePublishedFundingVersionsAreDeleted();
            await AndThePublishedProviderSearchDocumentsAreDeleted();
            await AndTheFundingBlobDocumentsAreDeleted();
            await AndTheSpecificationIsDeselectedForFunding();
        }

        private async Task ThenThePublishedProvidersAreDeleted()
        {
            await _publishedFundingRepository
                .Received(1)
                .DeleteAllPublishedProvidersByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }

        private async Task AndThePublishedProviderVersionsAreDeleted()
        {
            await _publishedFundingRepository
                .Received(1)
                .DeleteAllPublishedProviderVersionsByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }

        private async Task AndThePublishedFundingsAreDeleted()
        {
            await _publishedFundingRepository
                .Received(1)
                .DeleteAllPublishedFundingsByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }

        private async Task AndThePublishedFundingVersionsAreDeleted()
        {
            await _publishedFundingRepository
                .Received(1)
                .DeleteAllPublishedFundingVersionsByFundingStreamAndPeriod(_fundingStreamId, _fundingPeriodId);
        }

        private async Task AndThePublishedProviderSearchDocumentsAreDeleted()
        {
            await _deleteFundingSearchDocumentsService
                .Received(1)
                .DeleteFundingSearchDocuments<PublishedProviderIndex>(_fundingStreamId, _fundingPeriodId);
            
            await _deleteFundingSearchDocumentsService
                .Received(1)
                .DeleteFundingSearchDocuments<PublishedFundingIndex>(_fundingStreamId, _fundingPeriodId);
        }

        private async Task AndTheFundingBlobDocumentsAreDeleted()
        {
            await _deletePublishedFundingBlobDocumentsService
                .Received(1)
                .DeletePublishedFundingBlobDocuments(_fundingStreamId, _fundingPeriodId, "publishedproviderversions");
            
            await _deletePublishedFundingBlobDocumentsService
                .Received(1)
                .DeletePublishedFundingBlobDocuments(_fundingStreamId, _fundingPeriodId, "publishedfunding");
        }

        private async Task AndTheSpecificationIsDeselectedForFunding()
        {
            await _deselectSpecificationForFundingService
                .Received(1)
                .DeselectSpecificationForFunding(_fundingStreamId, _fundingPeriodId);
        }
        
        private void ThenDeletingThePublishedProvidersShouldThrowArgumentNullFor(string parameterName)
        {
            Func<Task> invocation = WhenThePublishedProvidersAreDeleted;

            invocation
                .Should()
                .ThrowAsync<ArgumentNullException>()
                .Result
                .Which
                .ParamName
                .Should()
                .Be(parameterName);   
        }

        private void GivenTheMessage(params (string, string)[] properties)
        {
            _message = new Message();

            _message
                .AddUserProperties(properties);
        }

        private void AndTheJobExistsForTheJobId()
        {
            _jobManagement
                .RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Returns(new JobViewModel());
        }

        private async Task WhenThePublishedProvidersAreDeleted()
        {
            await _service.Process(_message);
        }

        public static IEnumerable<object[]> QueueDeleteExceptionExamples()
        {
            yield return new object[] {null, NewRandomString()};
            yield return new object[] {NewRandomString(), null};
        }

        private Task WhenADeletePublishedProvidersJobIsQueued(string fundingStreamId,
            string fundingPeriodId,
            string correlationId)
        {
            return _service.QueueDeletePublishedProvidersJob(fundingStreamId, fundingPeriodId, correlationId);
        }

        private static Reference NewUser(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private static string NewRandomString()
        {
            return new RandomString();
        }
    }
}