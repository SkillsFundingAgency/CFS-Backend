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
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
using CalculateFunding.Tests.Common.Helpers;
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
        public void QueueMergeSpecificationInformationForProviderJobGuardsAgainstMissingSpecificationInformation()
        {
            Func<Task<IActionResult>> invocation = () => WhenAMergeSpecificationInformationForProviderJobIsQueued(null,
                NewRandomString(),
                NewUser(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationInformation");
        }

        [TestMethod]
        public async Task QueueMergeSpecificationInformationForProviderHasNoPropertiesWhenProviderIdNotSupplied()
        {
            Reference user = NewUser();
            string correlationId = NewRandomString();
            SpecificationInformation specificationInformation = NewSpecificationInformation();

            Job expectedJob = NewJob();

            GivenTheJob(expectedJob, specificationInformation, user, correlationId);

            OkObjectResult result = await WhenAMergeSpecificationInformationForProviderJobIsQueued(specificationInformation,
                null,
                user,
                correlationId) as OkObjectResult;

            result?.Value
                .Should()
                .BeSameAs(expectedJob);
        }

        [TestMethod]
        public async Task QueueMergeSpecificationInformationForProviderIncludesProviderIdInJobPropertiesWhenSupplied()
        {
            Reference user = NewUser();
            string correlationId = NewRandomString();
            string providerId = NewRandomString();
            SpecificationInformation specificationInformation = NewSpecificationInformation();

            Job expectedJob = NewJob();

            GivenTheJob(expectedJob, specificationInformation, user, correlationId, providerId);

            OkObjectResult result = await WhenAMergeSpecificationInformationForProviderJobIsQueued(specificationInformation,
                providerId,
                user,
                correlationId) as OkObjectResult;

            result?.Value
                .Should()
                .BeSameAs(expectedJob);
        }

        [TestMethod]
        public void MergeSpecificationInformationGuardsAgainstMissingSpecificationInformation()
        {
            Func<Task> invocation = () => WhenTheSpecificationInformationIsMerged(null, NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationInformation");
        }

        [TestMethod]
        public async Task MergeSpecificationInformationMergesForSpecificProviderIdWhenSupplied()
        {
            string providerId = NewRandomString();
            string jobId = NewRandomString();

            SpecificationInformation specificationInformation = NewSpecificationInformation();
            ProviderWithResultsForSpecifications providerWithResultsForSpecifications = NewProviderWithResultsForSpecifications();

            GivenTheProviderWithResultsForSpecificationsByProviderId(providerWithResultsForSpecifications, providerId);

            Message message = NewMessage(_ => _.WithUserProperty(JobId, jobId)
                .WithUserProperty(ProviderId, providerId)
                .WithMessageBody(specificationInformation.AsJsonBytes()));

            await WhenTheSpecificationInformationIsMerged(message);
            
            ThenTheJobTrackingWasStarted(jobId);
            AndTheProviderWithResultsForSpecificationsWereUpserted(providerWithResultsForSpecifications);

            providerWithResultsForSpecifications
                .Specifications
                .Should()
                .BeEquivalentTo(specificationInformation);
            
            AndTheJobTrackingWasCompleted(jobId);
        }
        
        [TestMethod]
        public async Task MergeSpecificationInformationMergesCreatesNewMissingProviderWithResultsForSpecificationsForSpecificProviderIdWhenSupplied()
        {
            string jobId = NewRandomString();
            string providerId = NewRandomString();

            SpecificationInformation specificationInformation = NewSpecificationInformation();

            DateTimeOffset expectedFundingPeriodEndDate = NewRandomDateTime();
            
            GivenTheFundingPeriodEndDate(specificationInformation.FundingPeriodId, expectedFundingPeriodEndDate);

            Message message = NewMessage(_ => _.WithUserProperty(JobId, jobId)
                .WithUserProperty(ProviderId, providerId)
                .WithMessageBody(specificationInformation.AsJsonBytes()));

            await WhenTheSpecificationInformationIsMerged(message);
            
            ThenTheJobTrackingWasStarted(jobId);
            
            SpecificationInformation expectedSpecificationInformation = specificationInformation.DeepCopy();
            expectedSpecificationInformation.FundingPeriodEnd = expectedFundingPeriodEndDate;
            
            AndTheProviderWithResultsForSpecificationsWasUpserted(_ => _.Id == providerId &&
                                                                      HasEquivalentSpecificationInformation(_, expectedSpecificationInformation));
            AndTheJobTrackingWasCompleted(jobId);
        }
        
        [TestMethod]
        public async Task MergeSpecificationInformationMergesForAllProviderWhenProviderIdNotSupplied()
        {
            string jobId = NewRandomString();
            string specificationId = NewRandomString();

            SpecificationInformation specificationInformation = NewSpecificationInformation(_ => _.WithId(specificationId));
            
            ProviderWithResultsForSpecifications providerOne = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerTwo = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerThree = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerFour = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerFive = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerSix = NewProviderWithResultsForSpecifications();
            ProviderWithResultsForSpecifications providerSeven = NewProviderWithResultsForSpecifications();

            DateTimeOffset expectedFundingPeriodEndDate = NewRandomDateTime();
            
            GivenTheFundingPeriodEndDate(specificationInformation.FundingPeriodId, expectedFundingPeriodEndDate);
             AndTheProviderWithResultsForSpecifications(specificationId,  NewFeedIterator(AsArray(providerOne, providerTwo),
                AsArray(providerThree, providerFour),
                AsArray(providerFive, providerSix),
                AsArray(providerSeven)));

            Message message = NewMessage(_ => _.WithUserProperty(JobId, jobId)
                .WithMessageBody(specificationInformation.AsJsonBytes()));

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
            AndTheProviderWithResultsForSpecificationsWereUpserted(providerSeven);

            AndTheProviderWithResultsForSpecificationsHaveTheEquivalentSpecificationInformation(expectedSpecificationInformation,
                providerOne,
                providerTwo,
                providerThree,
                providerFour,
                providerFive,
                providerSix,
                providerSeven);
            
            AndTheJobTrackingWasCompleted(jobId);
        }

        private void AndTheProviderWithResultsForSpecificationsHaveTheEquivalentSpecificationInformation(SpecificationInformation specificationInformation,
            params ProviderWithResultsForSpecifications[] providerWithResultsForSpecifications)
        {
            foreach (ProviderWithResultsForSpecifications providerWithResultsForSpecification in providerWithResultsForSpecifications)
            {
                providerWithResultsForSpecification
                    .Specifications
                    .Should()
                    .BeEquivalentTo(specificationInformation);
            }
        }

        private void GivenTheFundingPeriodEndDate(string fundingPeriodId,
            DateTimeOffset fundingPeriodEndDate)
        {
            _policies.Setup(_ => _.GetFundingPeriodById(fundingPeriodId))
                .ReturnsAsync(new ApiResponse<FundingPeriod>(HttpStatusCode.OK, NewFundingPeriod(_ => _.WithEndDate(fundingPeriodEndDate))));
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
            VerifyJobUpdateWasSent(jobId, false);
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

        private void VerifyJobUpdateWasSent(string jobId,
            bool completed)
        {
            _jobs.Verify(_ => _.AddJobLog(jobId, 
                    It.Is<JobLogUpdateModel>(jb => 
                jb.CompletedSuccessfully == completed)),
                Times.Once);
        }

        private async Task WhenTheSpecificationInformationIsMerged(Message message)
            => await _service.MergeSpecificationInformation(message);

        private async Task WhenTheSpecificationInformationIsMerged(SpecificationInformation specificationInformation,
            string providerId,
            ConcurrentDictionary<string, FundingPeriod> fundingPeriods = null)
            => await _service.MergeSpecificationInformation(specificationInformation, providerId, fundingPeriods);

        private async Task<IActionResult> WhenAMergeSpecificationInformationForProviderJobIsQueued(SpecificationInformation specificationInformation,
            string providerId,
            Reference user,
            string correlationId)
            => await _service.QueueMergeSpecificationInformationForProviderJob(specificationInformation, user, correlationId, providerId);

        private void GivenTheProviderWithResultsForSpecificationsByProviderId(ProviderWithResultsForSpecifications providerWithResultsForSpecifications,
            string providerId)
        {
            _calculationResults.Setup(_ => _.GetProviderWithResultsForSpecificationsByProviderId(providerId))
                .ReturnsAsync(providerWithResultsForSpecifications);
        }
        
        private void AndTheProviderWithResultsForSpecifications(string specificationId, ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> feed)
        {
            _calculationResults.Setup(_ => _.GetProvidersWithResultsForSpecificationBySpecificationId(specificationId))
                .Returns(feed);
        }
        
        private void GivenTheJob(Job job,
            SpecificationInformation specificationInformation,
            Reference user,
            string correlationId,
            string providerId = null)
        {
            _jobs.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(jb =>
                    (providerId == null || HasProviderIdInProperties(jb, providerId)) &&
                    jb.CorrelationId == correlationId &&
                    jb.InvokerUserId == user.Id &&
                    jb.InvokerUserDisplayName == user.Name &&
                    jb.MessageBody == specificationInformation.AsJson(true) &&
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
            .NewJob();

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

        private SpecificationInformation NewSpecificationInformation(Action<SpecificationInformationBuilder> setUp = null)
        {
            SpecificationInformationBuilder specificationInformationBuilder = new SpecificationInformationBuilder();

            setUp?.Invoke(specificationInformationBuilder);

            return specificationInformationBuilder.Build();
        }

        private ProviderWithResultsForSpecifications NewProviderWithResultsForSpecifications(Action<ProviderWithResultsForSpecificationsBuilder> setUp = null)
        {
            ProviderWithResultsForSpecificationsBuilder providerWithResultsForSpecificationsBuilder = new ProviderWithResultsForSpecificationsBuilder()
                .WithProviderInformation(NewProviderInformation());

            setUp?.Invoke(providerWithResultsForSpecificationsBuilder);
            
            return providerWithResultsForSpecificationsBuilder.Build();
        }

        private ProviderInformation NewProviderInformation(Action<ProviderInformationBuilder> setUp = null)
        {
            ProviderInformationBuilder providerInformationBuilder = new ProviderInformationBuilder();
            
            setUp?.Invoke(providerInformationBuilder);

            return providerInformationBuilder.Build();
        }

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);
            
            return messageBuilder.Build();
        }

        private ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> NewFeedIterator(params ProviderWithResultsForSpecifications[][] pages)
        {
            Mock<ICosmosDbFeedIterator<ProviderWithResultsForSpecifications>> feed = new Mock<ICosmosDbFeedIterator<ProviderWithResultsForSpecifications>>();

            ISetupSequentialResult<bool> hasResults = feed.SetupSequence(_ => _.HasMoreResults);
            ISetupSequentialResult<Task<IEnumerable<ProviderWithResultsForSpecifications>>> next = 
                feed.SetupSequence(_ => _.ReadNext(It.IsAny<CancellationToken>()));

            foreach (ProviderWithResultsForSpecifications[] page in pages)
            {
                hasResults = hasResults.Returns(true);
                next = next.ReturnsAsync(page);
            }
            
            return feed.Object;
        }
    }
}