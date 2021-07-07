using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.UnitTests.ApiClientHelpers.Jobs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Results.UnitTests
{
    [TestClass]
    public class SpecificationsWithProviderResultsServiceTests
    {
        private const string JobId = "jobId";
        private const string ProviderId = "provider-id";

        private Mock<IPoliciesApiClient> _policies;
        private Mock<ICalculationResultsRepository> _calculationResults;
        private Mock<IJobManagement> _jobs;

        private SpecificationsWithProviderResultsService _service;

        [TestInitialize]
        public void SetUp()
        {
            _policies = new Mock<IPoliciesApiClient>();
            _calculationResults = new Mock<ICalculationResultsRepository>();
            _jobs = new Mock<IJobManagement>();

            _service = new SpecificationsWithProviderResultsService(_calculationResults.Object,
                _policies.Object,
                _jobs.Object,
                new ProducerConsumerFactory(),
                new ResiliencePolicies
                {
                    CalculationProviderResultsSearchRepository = Policy.NoOpAsync(),
                    JobsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public void QueueMergeSpecificationInformationJobGuardsAgainstMissingMergeRequest()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheMergeSpecificationInformationJobIsQueued(null, NewUser(), NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("mergeRequest");
        }

        [TestMethod]
        public async Task QueueMergeSpecificationInformationJobCreatesNewJobWithSuppliedMergeRequest()
        {
            MergeSpecificationInformationRequest mergeRequest = NewMergeSpecificationInformationRequest(_ =>
                _.WithSpecificationInformation(NewSpecificationInformation(si =>
                        si.WithName(NewRandomString())))
                    .WithProviderIds(NewRandomString()));
            Job expectedJob = NewJob();
            Reference user = NewUser();
            string correlationId = NewRandomString();

            GivenTheJob(expectedJob, mergeRequest, user, correlationId);

            OkObjectResult okObjectResult = await WhenTheMergeSpecificationInformationJobIsQueued(mergeRequest, user, correlationId) as OkObjectResult;

            okObjectResult?.Value
                .Should()
                .BeSameAs(expectedJob);
        }

        [TestMethod]
        public void MergeSpecificationInformationGuardsAgainstMissingMergeRequest()
        {
            Func<Task> invocation = () => WhenTheSpecificationInformationIsMerged((MergeSpecificationInformationRequest)null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("mergeRequest");
        }

        [TestMethod]
        public async Task MergeSpecificationInformationMergesForSpecificProviderIdsWhenSupplied()
        {
            string providerOneId = NewRandomString();
            string providerTwoId = NewRandomString();
            string jobId = NewRandomString();

            string newFundingStreamIdOne = NewRandomString();
            string newFundingStreamIdTwo = NewRandomString();

            SpecificationInformation specificationInformation = NewSpecificationInformation(_ =>
                _.WithFundingStreamIds(newFundingStreamIdOne, newFundingStreamIdTwo));
            MergeSpecificationInformationRequest mergeRequest = NewMergeSpecificationInformationRequest(_ => _.WithSpecificationInformation(specificationInformation)
                .WithProviderIds(providerOneId, providerTwoId));

            SpecificationInformation informationWithoutFundingStreams = specificationInformation.DeepCopy();
            informationWithoutFundingStreams.FundingStreamIds = null;

            ProviderWithResultsForSpecifications providerWithResultsForSpecificationsOne = NewProviderWithResultsForSpecifications(_ =>
                _.WithSpecifications(informationWithoutFundingStreams));
            ProviderWithResultsForSpecifications providerWithResultsForSpecificationsTwo = NewProviderWithResultsForSpecifications(_ =>
                _.WithSpecifications(informationWithoutFundingStreams));

            GivenTheProviderWithResultsForSpecificationsByProviderId(providerWithResultsForSpecificationsOne, providerOneId);
            GivenTheProviderWithResultsForSpecificationsByProviderId(providerWithResultsForSpecificationsTwo, providerTwoId);

            Message message = NewMessage(_ => _.WithUserProperty(JobId, jobId)
                .WithMessageBody(mergeRequest.AsJsonBytes()));

            await WhenTheSpecificationInformationIsMerged(message);

            ThenTheJobTrackingWasStarted(jobId);
            AndTheProviderWithResultsForSpecificationsWereUpserted(providerWithResultsForSpecificationsOne);
            AndTheProviderWithResultsForSpecificationsWereUpserted(providerWithResultsForSpecificationsTwo);

            specificationInformation.IsDirty = true;

            providerWithResultsForSpecificationsOne
                .Specifications
                .Should()
                .BeEquivalentTo(specificationInformation);

            providerWithResultsForSpecificationsTwo
                .Specifications
                .Should()
                .BeEquivalentTo(specificationInformation);

            AndTheJobTrackingWasCompleted(jobId);
        }

        [TestMethod]
        public async Task MergeSpecificationInformationMergesCreatesNewMissingProviderWithResultsForSpecificationsForSpecificProviderIdsWhenSupplied()
        {
            string jobId = NewRandomString();
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();

            SpecificationInformation specificationInformation = NewSpecificationInformation();
            MergeSpecificationInformationRequest mergeRequest = NewMergeSpecificationInformationRequest(_ => _.WithSpecificationInformation(specificationInformation)
                .WithProviderIds(providerIdOne, providerIdTwo));

            DateTimeOffset expectedFundingPeriodEndDate = NewRandomDateTime();

            GivenTheFundingPeriodEndDate(specificationInformation.FundingPeriodId, expectedFundingPeriodEndDate);

            Message message = NewMessage(_ => _.WithUserProperty(JobId, jobId)
                .WithUserProperty(ProviderId, providerIdOne)
                .WithMessageBody(mergeRequest.AsJsonBytes()));

            await WhenTheSpecificationInformationIsMerged(message);

            ThenTheJobTrackingWasStarted(jobId);

            SpecificationInformation expectedSpecificationInformation = specificationInformation.DeepCopy();
            expectedSpecificationInformation.FundingPeriodEnd = expectedFundingPeriodEndDate;

            AndTheProviderWithResultsForSpecificationsWasUpserted(_ => _.Id == providerIdOne &&
                                                                       HasEquivalentSpecificationInformation(_, expectedSpecificationInformation));
            AndTheJobTrackingWasCompleted(jobId);
            AndTheGetFundingPeriodByIdCalled(specificationInformation.FundingPeriodId, 1);
        }

        [TestMethod]
        public async Task MergeSpecificationInformationMergesForAllProviderWhenProviderIdNotSupplied()
        {
            string jobId = NewRandomString();
            string specificationId = NewRandomString();

            SpecificationInformation specificationInformation = NewSpecificationInformation(_ => _.WithId(specificationId));
            MergeSpecificationInformationRequest mergeRequest = NewMergeSpecificationInformationRequest(_ => _.WithSpecificationInformation(specificationInformation));

            ProviderWithResultsForSpecifications providerOne = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerTwo = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerThree = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerFour = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerFive = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerSix = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerSeven = NewProviderWithResultsForSpecifications();
            ProviderResult providerResultEight = NewProviderResult();
            ProviderWithResultsForSpecifications providerEight = NewProviderWithResultsForSpecifications(providerInformation: new ProviderInformation { Id = providerResultEight.Provider.Id });
            ProviderResult providerResultNine = NewProviderResult();
            ProviderWithResultsForSpecifications providerNine = NewProviderWithResultsForSpecifications(providerInformation: new ProviderInformation { Id = providerResultNine.Provider.Id });

            DateTimeOffset expectedFundingPeriodEndDate = NewRandomDateTime();

            GivenTheFundingPeriodEndDate(specificationInformation.FundingPeriodId, expectedFundingPeriodEndDate);
            AndTheProviderWithResultsForSpecifications(specificationId,
                NewFeedIterator(AsArray(providerOne, providerTwo),
                    AsArray(providerThree, providerFour),
                    AsArray(providerFive, providerSix),
                    AsArray(providerSeven, providerEight)));

            AndTheProviderResultsForSpecification(specificationId, new[] { providerResultEight, providerResultNine });

            Message message = NewMessage(_ => _.WithUserProperty(JobId, jobId)
                .WithMessageBody(mergeRequest.AsJsonBytes()));

            await WhenTheSpecificationInformationIsMerged(message);

            ThenTheJobTrackingWasStarted(jobId);

            SpecificationInformation expectedSpecificationInformation = specificationInformation.DeepCopy();
            expectedSpecificationInformation.FundingPeriodEnd = expectedFundingPeriodEndDate;

            AndTheProviderWithResultsForSpecificationsWereUpserted(providerOne,
                providerTwo);
            AndTheProviderWithResultsForSpecificationsWereUpserted(providerThree,
                providerFour);
            AndTheProviderWithResultsForSpecificationsWereUpserted(providerFive,
                providerSix);
            AndTheProviderWithResultsForSpecificationsWereUpserted(providerSeven,
                providerEight);

            //only upserts dirty records
            AndTheProviderWithResultsForSpecificationsWereNotUpserted(providerNine);

            expectedSpecificationInformation.IsDirty = true;

            AndTheProviderWithResultsForSpecificationsHaveTheEquivalentSpecificationInformation(expectedSpecificationInformation,
                providerOne,
                providerTwo,
                providerThree,
                providerFour,
                providerFive,
                providerSix,
                providerSeven);

            AndTheJobTrackingWasCompleted(jobId);
            AndTheGetFundingPeriodByIdCalled(specificationInformation.FundingPeriodId, 1);
        }

        private void AndTheProviderWithResultsForSpecificationsHaveTheEquivalentSpecificationInformation(SpecificationInformation specificationInformation,
            params ProviderWithResultsForSpecifications[] providerWithResultsForSpecifications)
        {
            foreach (ProviderWithResultsForSpecifications providerWithResultsForSpecification in providerWithResultsForSpecifications)
            {
                providerWithResultsForSpecification
                    .Specifications
                    .Should()
                    .BeEquivalentTo(new[] { specificationInformation },
                        opt => opt.Excluding(_ => _.IsDirty));
            }
        }

        private void GivenTheFundingPeriodEndDate(string fundingPeriodId,
            DateTimeOffset fundingPeriodEndDate)
        {
            _policies.Setup(_ => _.GetFundingPeriodById(fundingPeriodId))
                .ReturnsAsync(new ApiResponse<FundingPeriod>(HttpStatusCode.OK, NewFundingPeriod(_ => _.WithEndDate(fundingPeriodEndDate))));
        }

        private void AndTheGetFundingPeriodByIdCalled(string fundingPeriodId, int callCount)
        {
            _policies
                .Verify(_ => _.GetFundingPeriodById(fundingPeriodId), Times.Exactly(callCount));
        }

        private bool HasEquivalentSpecificationInformation(ProviderWithResultsForSpecifications providerWithResultsForSpecifications,
            params SpecificationInformation[] specifications)
        {
            foreach (SpecificationInformation specificationInformation in specifications)
            {
                if (providerWithResultsForSpecifications.Specifications.Count(_ => _.Id == specificationInformation.Id &&
                                                                                   _.Name == specificationInformation.Name &&
                                                                                   _.FundingPeriodId == specificationInformation.FundingPeriodId &&
                                                                                   _.FundingPeriodEnd == specificationInformation.FundingPeriodEnd &&
                                                                                   _.LastEditDate == specificationInformation.LastEditDate) != 1)
                {
                    return false;
                }
            }

            return providerWithResultsForSpecifications.Specifications.Count() == specifications.Length;
        }

        private void ThenTheJobTrackingWasStarted(string jobId)
        {
            VerifyJobUpdateWasSent(jobId);
        }

        private void AndTheJobTrackingWasCompleted(string jobId)
        {
            VerifyJobUpdateWasSent(jobId, true);
        }

        private void AndTheProviderWithResultsForSpecificationsWasUpserted(Expression<Func<ProviderWithResultsForSpecifications, bool>> match)
        {
            _calculationResults.Verify(_ => _.UpsertSpecificationWithProviderResults(It.Is(match)),
                Times.Once);
        }

        private void AndTheProviderWithResultsForSpecificationsWereUpserted(params ProviderWithResultsForSpecifications[] providerWithResultsForSpecifications)
        {
            _calculationResults.Verify(_ => _.UpsertSpecificationWithProviderResults(It.Is<ProviderWithResultsForSpecifications[]>(results =>
                    results.SequenceEqual(providerWithResultsForSpecifications))),
                Times.Once);
        }

        private void AndTheProviderWithResultsForSpecificationsWereNotUpserted(ProviderWithResultsForSpecifications providerWithResultsForSpecification)
        {
            _calculationResults.Verify(_ => _.UpsertSpecificationWithProviderResults(It.Is<ProviderWithResultsForSpecifications[]>(results =>
                    results.First().Provider.Id == providerWithResultsForSpecification.Provider.Id)),
                Times.Never);
        }

        private void AndTheProviderWithResultsForSpecificationsWereUpserted(ProviderWithResultsForSpecifications providerWithResultsForSpecification)
        {
            _calculationResults.Verify(_ => _.UpsertSpecificationWithProviderResults(It.Is<ProviderWithResultsForSpecifications[]>(results =>
                    results.First().Provider.Id == providerWithResultsForSpecification.Provider.Id)),
                Times.Once);
        }

        private void VerifyJobUpdateWasSent(string jobId,
            bool? completed = null)
        {
            _jobs.Verify(_ => _.UpdateJobStatus(jobId,
                    0,
                    0,
                    completed,
                    null),
                Times.Once);
        }

        private async Task<IActionResult> WhenTheMergeSpecificationInformationJobIsQueued(MergeSpecificationInformationRequest mergeRequest,
            Reference user,
            string correlationId)
            => await _service.QueueMergeSpecificationInformationJob(mergeRequest, user, correlationId);

        private async Task WhenTheSpecificationInformationIsMerged(Message message)
            => await _service.Run(message);

        private async Task WhenTheSpecificationInformationIsMerged(MergeSpecificationInformationRequest mergeRequest)
            => await _service.MergeSpecificationInformation(mergeRequest);

        private void GivenTheProviderWithResultsForSpecificationsByProviderId(ProviderWithResultsForSpecifications providerWithResultsForSpecifications,
            string providerId)
        {
            _calculationResults.Setup(_ => _.GetProviderWithResultsForSpecificationsByProviderId(providerId))
                .ReturnsAsync(providerWithResultsForSpecifications);
        }

        private void AndTheProviderWithResultsForSpecifications(string specificationId,
            ICosmosDbFeedIterator feed)
        {
            _calculationResults.Setup(_ => _.GetProvidersWithResultsForSpecificationBySpecificationId(specificationId))
                .Returns(feed);
        }

        private void AndTheProviderResultsForSpecification(string specificationId,
            IEnumerable<ProviderResult> providerResults)
        {
            _calculationResults.Setup(_ => _.GetProviderResultsBySpecificationId(specificationId, -1))
                .ReturnsAsync(providerResults);
        }

        private void GivenTheJob(Job job,
            MergeSpecificationInformationRequest mergeRequest,
            Reference user,
            string correlationId,
            string providerId = null)
        {
            _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(jb =>
                    (providerId == null || HasProviderIdInProperties(jb, providerId)) &&
                    jb.CorrelationId == correlationId &&
                    jb.InvokerUserId == user.Id &&
                    jb.InvokerUserDisplayName == user.Name &&
                    jb.MessageBody == mergeRequest.AsJson(true) &&
                    jb.JobDefinitionId == JobConstants.DefinitionNames.MergeSpecificationInformationForProviderJob)))
                .ReturnsAsync(job);
        }

        private static TItem[] AsArray<TItem>(params TItem[] items) => items;

        private static TItem[][] AsPages<TItem>(params TItem[][] pages) => pages;

        private bool HasProviderIdInProperties(JobCreateModel jobCreateModel,
            string providerId)
            => jobCreateModel.Properties?.ContainsKey(ProviderId) == true &&
               jobCreateModel.Properties[ProviderId] == providerId;

        private Reference NewUser() => new ReferenceBuilder()
            .Build();

        private string NewRandomString() => new RandomString();

        private Job NewJob() => new JobBuilder()
            .Build();

        private FundingPeriod NewFundingPeriod(Action<FundingPeriodBuilder> setUp = null)
        {
            FundingPeriodBuilder fundingPeriodBuilder = new FundingPeriodBuilder();

            setUp?.Invoke(fundingPeriodBuilder);

            return fundingPeriodBuilder.Build();
        }

        private DateTimeOffset NewRandomDateTime() => new RandomDateTime();

        private Message NewMessage(SpecificationInformation specificationInformation,
            params (string key, string value)[] properties)
        {
            Message message = new Message(specificationInformation.AsJsonBytes());

            foreach ((string key, string value) property in properties)
            {
                message.UserProperties.Add(property.key, property.value);
            }

            return message;
        }

        private MergeSpecificationInformationRequest NewMergeSpecificationInformationRequest(Action<MergeSpecificationInformationRequestBuilder> setUp = null)
        {
            MergeSpecificationInformationRequestBuilder mergeSpecificationInformationRequestBuilder = new MergeSpecificationInformationRequestBuilder();

            setUp?.Invoke(mergeSpecificationInformationRequestBuilder);

            return mergeSpecificationInformationRequestBuilder.Build();
        }

        private SpecificationInformation NewSpecificationInformation(Action<SpecificationInformationBuilder> setUp = null)
        {
            SpecificationInformationBuilder specificationInformationBuilder = new SpecificationInformationBuilder();

            setUp?.Invoke(specificationInformationBuilder);

            return specificationInformationBuilder.Build();
        }

        private ProviderWithResultsForSpecifications NewProviderWithResultsForSpecifications(Action<ProviderWithResultsForSpecificationsBuilder> setUp = null, ProviderInformation providerInformation = null)
        {
            ProviderWithResultsForSpecificationsBuilder providerWithResultsForSpecificationsBuilder = new ProviderWithResultsForSpecificationsBuilder()
                .WithProviderInformation(providerInformation ?? NewProviderInformation());

            setUp?.Invoke(providerWithResultsForSpecificationsBuilder);

            return providerWithResultsForSpecificationsBuilder.Build();
        }

        private ProviderResult NewProviderResult(Action<ProviderResultBuilder> setUp = null)
        {
            ProviderResultBuilder providerResultBuilder = new ProviderResultBuilder()
                .WithProviderSummary(NewProviderSummary());

            setUp?.Invoke(providerResultBuilder);

            return providerResultBuilder.Build();
        }

        private ProviderInformation NewProviderInformation(Action<ProviderInformationBuilder> setUp = null)
        {
            ProviderInformationBuilder providerInformationBuilder = new ProviderInformationBuilder();

            setUp?.Invoke(providerInformationBuilder);

            return providerInformationBuilder.Build();
        }

        private ProviderSummary NewProviderSummary(Action<ProviderSummaryBuilder> setUp = null)
        {
            ProviderSummaryBuilder providerSummaryBuilder = new ProviderSummaryBuilder();

            setUp?.Invoke(providerSummaryBuilder);

            return providerSummaryBuilder.Build();
        }

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }

        private ICosmosDbFeedIterator NewFeedIterator(params ProviderWithResultsForSpecifications[][] pages)
        {
            Mock<ICosmosDbFeedIterator> feed = new Mock<ICosmosDbFeedIterator>();

            ISetupSequentialResult<bool> hasResults = feed.SetupSequence(_ => _.HasMoreResults);
            ISetupSequentialResult<Task<IEnumerable<ProviderWithResultsForSpecifications>>> next =
                feed.SetupSequence(_ => _.ReadNext<ProviderWithResultsForSpecifications>(It.IsAny<CancellationToken>()));

            foreach (ProviderWithResultsForSpecifications[] page in pages)
            {
                hasResults = hasResults.Returns(true);
                next = next.ReturnsAsync(page);
            }

            return feed.Object;
        }
    }
}