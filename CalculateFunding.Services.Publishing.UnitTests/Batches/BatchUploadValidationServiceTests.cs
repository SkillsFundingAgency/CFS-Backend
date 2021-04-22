using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Batches;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.UnitTests.ApiClientHelpers.Jobs;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    [TestClass]
    public class BatchUploadValidationServiceTests
    {
        private Mock<IBatchUploadReaderFactory> _batchUploadReaderFactory;
        private Mock<IBatchUploadReader> _batchUploadReader;
        private Mock<IPublishedFundingRepository> _publishedFunding;
        private Mock<IBlobClient> _blobClient;
        private Mock<IValidator<BatchUploadValidationRequest>> _validation;
        private Mock<IJobManagement> _jobs;
        private Mock<ICloudBlob> _cloudBlob;

        private BatchUploadValidationService _service;

        private const string BatchId = "batch-id";
        private const string FundingStreamId = "funding-stream-id";
        private const string FundingPeriodId = "funding-period-id";
        private const string SpecificationId = "specification-id";

        [TestInitialize]
        public void SetUp()
        {
            _batchUploadReaderFactory = new Mock<IBatchUploadReaderFactory>();
            _batchUploadReader = new Mock<IBatchUploadReader>();
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _blobClient = new Mock<IBlobClient>();
            _validation = new Mock<IValidator<BatchUploadValidationRequest>>();
            _jobs = new Mock<IJobManagement>();
            _cloudBlob = new Mock<ICloudBlob>();

            _batchUploadReaderFactory.Setup(_ => _.CreateBatchUploadReader())
                .Returns(_batchUploadReader.Object);

            _service = new BatchUploadValidationService(_jobs.Object,
                _validation.Object,
                _batchUploadReaderFactory.Object,
                _publishedFunding.Object,
                _blobClient.Object,
                new ResiliencePolicies
                {
                    JobsApiClient = Policy.NoOpAsync(),
                    BlobClient = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public async Task QueueBatchUploadValidationReturnsBadRequestIfRequestFailsValidation()
        {
            BatchUploadValidationRequest request = NewBatchUploadValidationRequest();
            ValidationResult validationResult = NewValidationResult(_ => _.WithValidationFailures(NewValidationFailure(),
                NewValidationFailure()));

            GivenTheValidationResultForTheRequest(request, validationResult);

            BadRequestObjectResult badRequest = await WhenTheBatchUploadValidationIsQueued(request, null, null) as BadRequestObjectResult;

            badRequest
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task QueueBatchUploadValidationCreatesBatchUploadValidationJobForSuppliedBatchDetails()
        {
            Reference user = NewUser();
            string correlationId = NewRandomString();

            BatchUploadValidationRequest request = NewBatchUploadValidationRequest(_ => _.WithBatchId(NewRandomString())
                .WithFundingPeriodId(NewRandomString())
                .WithFundingStreamId(NewRandomString())
                .WithSpecificationId(NewRandomString()));

            Job job = NewJob();

            GivenTheValidationResultForTheRequest(request, NewValidationResult());
            AndTheJob(request, user, correlationId, job);

            OkObjectResult result = await WhenTheBatchUploadValidationIsQueued(request, user, correlationId) as OkObjectResult;

            result?
                .Value
                .Should()
                .BeEquivalentTo(new
                {
                    JobId = job.Id
                });
        }

        [TestMethod]
        public void ProcessGuardsAgainstMissingBatchIdInMessage()
        {
            Func<Task> invocation = () => WhenTheBatchUploadIsValidated(NewMessage(_ => _.WithUserProperty(FundingStreamId, NewRandomString())
                .WithUserProperty(FundingPeriodId, NewRandomString())));

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"No {BatchId} property in message");
        }

        [TestMethod]
        public void ProcessGuardsAgainstMissingFundingStreamIdInMessage()
        {
            Func<Task> invocation = () => WhenTheBatchUploadIsValidated(NewMessage(_ => _.WithUserProperty(BatchId, NewRandomString())
                .WithUserProperty(FundingPeriodId, NewRandomString())));

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"No {FundingStreamId} property in message");
        }

        [TestMethod]
        public void ProcessGuardsAgainstMissingFundingPeriodIdInMessage()
        {
            Func<Task> invocation = () => WhenTheBatchUploadIsValidated(NewMessage(_ => _.WithUserProperty(BatchId, NewRandomString())
                .WithUserProperty(FundingStreamId, NewRandomString())));

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"No {FundingPeriodId} property in message");
        }

        [TestMethod]
        public async Task ProcessReadsUkprnsFromBlobStorageAndQueriesPublishedProviderIdsForEachWritingTheResultsBackToBlobStorage()
        {
            string batchId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string ukprn1 = NewRandomString();
            string ukprn2 = NewRandomString();
            string ukprn3 = NewRandomString();
            string ukprn4 = NewRandomString();
            string ukprn5 = NewRandomString();
            string ukprn6 = NewRandomString();

            string publishedProviderId1 = NewRandomString();
            string publishedProviderId2 = NewRandomString();
            string publishedProviderId3 = NewRandomString();
            string publishedProviderId4 = NewRandomString();
            string publishedProviderId5 = NewRandomString();
            string publishedProviderId6 = NewRandomString();

            GivenThePagesOfUkprns(new BatchUploadBlobName(batchId),
                AsArray(ukprn1, ukprn2),
                AsArray(ukprn3),
                AsArray(ukprn4, ukprn5),
                AsArray(ukprn6));
            AndThePublisherProviderIds(fundingStreamId,
                fundingPeriodId,
                AsArray(ukprn1, ukprn2),
                (ukprn1, publishedProviderId1),
                (ukprn2, publishedProviderId2));
            AndThePublisherProviderIds(fundingStreamId,
                fundingPeriodId,
                AsArray(ukprn3),
                (ukprn3, publishedProviderId3));
            AndThePublisherProviderIds(fundingStreamId,
                fundingPeriodId,
                AsArray(ukprn4, ukprn5),
                (ukprn4, publishedProviderId4),
                (ukprn5, publishedProviderId5));
            AndThePublisherProviderIds(fundingStreamId,
                fundingPeriodId,
                AsArray(ukprn6),
                (ukprn6, publishedProviderId6));

            AndTheCloudBlobWasCreatedForTheBatchId(new BatchUploadProviderIdsBlobName(batchId));

            await WhenTheBatchUploadIsValidated(NewMessage(_ => _.WithUserProperty(BatchId, batchId)
                .WithUserProperty(FundingStreamId, fundingStreamId)
                .WithUserProperty(FundingPeriodId, fundingPeriodId)
                .WithUserProperty(SpecificationId, NewRandomString())));

            ThenThePublishedProviderIdsWereWrittenToBlobStorage(publishedProviderId1,
                publishedProviderId2,
                publishedProviderId3,
                publishedProviderId4,
                publishedProviderId5,
                publishedProviderId6);

            _batchUploadReader.VerifyAll();
        }

        [TestMethod]
        public void ProcessFailsIfAnyUkprnsAreMissingForTheFundingStreamAndPeriodInTheSuppliedBatchDetails()
        {
            string batchId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string ukprn1 = NewRandomString();
            string ukprn2 = NewRandomString();
            string ukprn3 = NewRandomString();
            string ukprn4 = NewRandomString();
            string ukprn5 = NewRandomString();
            string ukprn6 = NewRandomString();

            string publishedProviderId3 = NewRandomString();

            BatchUploadBlobName uploadedBlobName = new BatchUploadBlobName(batchId);

            GivenThePagesOfUkprns(uploadedBlobName,
                AsArray(ukprn1, ukprn2),
                AsArray(ukprn3),
                AsArray(ukprn4, ukprn5),
                AsArray(ukprn6));
            AndThePublisherProviderIds(fundingStreamId,
                fundingPeriodId,
                AsArray(ukprn3),
                (ukprn3, publishedProviderId3));

            Action invocation = () => WhenTheBatchUploadIsValidated(NewMessage(_ => _.WithUserProperty(BatchId, batchId)
                    .WithUserProperty(FundingStreamId, fundingStreamId)
                    .WithUserProperty(FundingPeriodId, fundingPeriodId)
                    .WithUserProperty(SpecificationId, NewRandomString())))
                .GetAwaiter()
                .GetResult();

            string outcome = $"Did not locate the following ukprns for {fundingStreamId} and {fundingPeriodId}:\n{ukprn1},{ukprn2},{ukprn4},{ukprn5},{ukprn6}";

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage(outcome);

            _batchUploadReader.VerifyAll();
        }

        private string[] AsArray(params string[] items) => items;

        private void GivenThePagesOfUkprns(string blobName,
            params string[][] pages)
        {
            _batchUploadReader.Setup(_ => _.LoadBatchUpload(blobName))
                .Returns(Task.CompletedTask)
                .Verifiable();

            ISetupSequentialResult<IEnumerable<string>> pageSequence = _batchUploadReader.SetupSequence(_ => _.NextPage());
            ISetupSequentialResult<bool> hasPagesSequence = _batchUploadReader.SetupSequence(_ => _.HasPages);

            foreach (string[] page in pages)
            {
                pageSequence = pageSequence.Returns(page);
                hasPagesSequence = hasPagesSequence.Returns(true);
            }
        }

        private void AndThePublisherProviderIds(string fundingStreamId,
            string fundingPeriodId,
            string[] ukprns,
            params (string ukprn, string publishedProviderId)[] publishedProviderIds)
            => _publishedFunding.Setup(_ => _.GetPublishedProviderIdsForUkprns(It.Is<string>(fs => fs == fundingStreamId),
                    It.Is<string>(fp => fp == fundingPeriodId),
                    It.Is<string[]>(pn => pn.SequenceEqual(ukprns))))
                .ReturnsAsync(publishedProviderIds.ToDictionary(_ => _.ukprn, _ => _.publishedProviderId));

        private void AndTheCloudBlobWasCreatedForTheBatchId(string blobName)
            => _blobClient.Setup(_ => _.GetBlockBlobReference(blobName, "batchuploads"))
                .Returns(_cloudBlob.Object);

        private void ThenThePublishedProviderIdsWereWrittenToBlobStorage(params string[] publishedProviderIds)
        {
            string json = publishedProviderIds.AsJson().Prettify();

            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            _cloudBlob.Verify(_ => _.UploadFromByteArrayAsync(It.Is<byte[]>(bytes =>
                        bytes.SequenceEqual(jsonBytes)),
                    0,
                    jsonBytes.Length),
                Times.Once);
        }

        private async Task WhenTheBatchUploadIsValidated(Message message)
            => await _service.Process(message);

        private async Task<IActionResult> WhenTheBatchUploadValidationIsQueued(BatchUploadValidationRequest request,
            Reference reference,
            string correlationId)
            => await _service.QueueBatchUploadValidation(request, reference, correlationId);

        private void GivenTheValidationResultForTheRequest(BatchUploadValidationRequest request,
            ValidationResult validationResult)
            => _validation.Setup(_ => _.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

        private void AndTheJob(BatchUploadValidationRequest request,
            Reference user,
            string correlationId,
            Job job)
        {
            (string key, string value)[] expectedProperties =
            {
                ("batch-id", request.BatchId),
                ("funding-stream-id", request.FundingStreamId),
                ("funding-period-id", request.FundingPeriodId),
                ("specification-id", request.SpecificationId)
            };

            _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(jcm =>
                    jcm.CorrelationId == correlationId &&
                    jcm.InvokerUserId == user.Id &&
                    jcm.SpecificationId == request.SpecificationId &&
                    jcm.InvokerUserDisplayName == user.Name &&
                    jcm.JobDefinitionId == JobConstants.DefinitionNames.BatchPublishedProviderValidationJob &&
                    HasTheProperties(jcm, expectedProperties)
                )))
                .ReturnsAsync(job);
        }

        private bool HasTheProperties(JobCreateModel jobCreateModel,
            params (string key, string value)[] properties)
        {
            return properties.All(_ => jobCreateModel.Properties.ContainsKey(_.key!) &&
                                       jobCreateModel.Properties[_.key] == _.value);
        }

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }

        private BatchUploadValidationRequest NewBatchUploadValidationRequest(Action<BatchUploadValidationRequestBuilder> setUp = null)
        {
            BatchUploadValidationRequestBuilder batchUploadValidationRequestBuilder = new BatchUploadValidationRequestBuilder();

            setUp?.Invoke(batchUploadValidationRequestBuilder);

            return batchUploadValidationRequestBuilder.Build();
        }

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder validationResultBuilder = new ValidationResultBuilder();

            setUp?.Invoke(validationResultBuilder);

            return validationResultBuilder.Build();
        }

        private ValidationFailure NewValidationFailure(Action<ValidationFailureBuilder> setUp = null)
        {
            ValidationFailureBuilder validationFailureBuilder = new ValidationFailureBuilder();

            setUp?.Invoke(validationFailureBuilder);

            return validationFailureBuilder.Build();
        }

        private Job NewJob() => new JobBuilder().Build();

        private Reference NewUser() => new ReferenceBuilder().Build();

        private static string NewRandomString() => new RandomString();
    }
}