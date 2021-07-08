using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using TechTalk.SpecFlow;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable PossibleNullReferenceException

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class RefreshStepDefinition
    {
        private readonly IVariationServiceStepContext _variationServiceStepContext;
        private readonly IPublishFundingStepContext _publishFundingStepContext;
        private readonly CurrentSpecificationStepContext _currentSpecificationStepContext;
        private readonly CurrentJobStepContext _currentJobStepContext;
        private readonly CurrentUserStepContext _currentUserStepContext;
        private readonly IRefreshService _refreshService;
        private readonly ICurrentCorrelationStepContext _currentCorrelationStepContext;
        private readonly PublishedProviderVersionInMemoryRepository _providerVersionInMemoryRepository;
        private readonly SpecificationInMemoryRepository _specificationInMemoryRepository;

        public RefreshStepDefinition(IPublishFundingStepContext publishFundingStepContext,
            CurrentSpecificationStepContext currentSpecificationStepContext,
            CurrentJobStepContext currentJobStepContext,
            CurrentUserStepContext currentUserStepContext,
            IVariationServiceStepContext variationServiceStepContext,
            IRefreshService refreshService,
            IVersionRepository<PublishedProviderVersion> publishedProviderVersionRepository,
            ICurrentCorrelationStepContext currentCorrelationStepContext,
            ISpecificationService specificationService)
        {
            _publishFundingStepContext = publishFundingStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
            _currentJobStepContext = currentJobStepContext;
            _currentUserStepContext = currentUserStepContext;
            _variationServiceStepContext = variationServiceStepContext;
            _refreshService = refreshService;
            _currentCorrelationStepContext = currentCorrelationStepContext;
            _providerVersionInMemoryRepository =
                (PublishedProviderVersionInMemoryRepository)publishedProviderVersionRepository;
            _specificationInMemoryRepository = (SpecificationInMemoryRepository)specificationService;
        }

        [Given(@"variations are enabled")]
        public void GivenVariationsAreEnabled()
        {
            _publishFundingStepContext.SetFeatureIsEnabled("EnableVariations", true);
        }

        [Given]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        public void GiveTheVariationPointersForTheCurrentSpecification(
            IEnumerable<ProfileVariationPointer> profileVariationPointers)
        {
            profileVariationPointers
                .Should()
                .NotBeNullOrEmpty();

            _variationServiceStepContext.SpecificationsInMemoryClient.SetProfileVariationPointers(
                _currentSpecificationStepContext.SpecificationId,
                profileVariationPointers.ToArray());
        }

        [When(@"funding is refreshed")]
        public async Task WhenFundingIsRefreshed()
        {
            Message message = new Message();

            message.UserProperties.Add("user-id", _currentUserStepContext.UserId);
            message.UserProperties.Add("user-name", _currentUserStepContext.UserName);
            message.UserProperties.Add("specification-id", _currentSpecificationStepContext.SpecificationId);
            message.UserProperties.Add("jobId", _currentJobStepContext.JobId);

            _currentCorrelationStepContext.CorrelationId = Guid.NewGuid().ToString();
            message.UserProperties.Add("sfa-correlationId", _currentCorrelationStepContext.CorrelationId);

            await _refreshService.Run(message);
        }

        [Given(@"the following variation pointers exist")]
        public void GivenTheFollowingVariationPointersExistForProvider(
            IEnumerable<ProfileVariationPointer> variationPointers)
        {
            _specificationInMemoryRepository.AddVariationPointers(_currentSpecificationStepContext.SpecificationId,
                variationPointers.ToArray());
        }

        [Then(@"the upserted provider version for '(.*)' has the following funding line profile periods")]
        public void ThenTheUpsertedProviderVersionForHasTheFollowingFundingLineProfilePeriods(string providerId,
            IEnumerable<ExpectedFundingLineProfileValues> expectedFundingLineProfileValues)
        {
            PublishedProviderVersion publishedProviderVersion = GetUpsertedPublishedProviderVersion(providerId);

            foreach (ExpectedFundingLineProfileValues expectedFundingLineProfileValue in expectedFundingLineProfileValues)
            {
                FundingLine fundingLine = publishedProviderVersion.FundingLines.SingleOrDefault(_ =>
                    _.FundingLineCode == expectedFundingLineProfileValue.FundingLineCode);

                fundingLine
                    .Should()
                    .NotBeNull();

                foreach (ExpectedDistributionPeriod expectedDistributionPeriod in expectedFundingLineProfileValue.ExpectedDistributionPeriods)
                {
                    DistributionPeriod distributionPeriod = fundingLine.DistributionPeriods.SingleOrDefault(_ =>
                        _.DistributionPeriodId == expectedDistributionPeriod.DistributionPeriodId);

                    distributionPeriod
                        .Should()
                        .NotBeNull();

                    foreach (ExpectedProfileValue expectedProfileValue in expectedDistributionPeriod.ExpectedProfileValues)
                    {
                        ProfilePeriod profilePeriod = distributionPeriod
                            .ProfilePeriods.SingleOrDefault(
                                _ => _.Type == expectedProfileValue.Type &&
                                     _.TypeValue == expectedProfileValue.TypeValue &&
                                     _.Year == expectedProfileValue.Year &&
                                     _.Occurrence == expectedProfileValue.Occurrence);

                        profilePeriod
                            .Should()
                            .NotBeNull();

                        profilePeriod
                            .ProfiledValue
                            .Should()
                            .Be(expectedProfileValue.ProfiledValue, $"expected period {profilePeriod.TypeValue} in {profilePeriod.Year}, occurrence {profilePeriod.Occurrence} should be the same");
                    }
                }
            }
        }

        [Then(@"the upserted provider version for '(.*)' has the following predecessors")]
        public void ThenTheUpsertedProviderVersionForHasTheFollowingPredecessors(string providerId,
            IEnumerable<ExpectedPredecessor> expectedPredecessors)
        {
            PublishedProviderVersion publishedProviderVersion = GetUpsertedPublishedProviderVersion(providerId);

            publishedProviderVersion
                .Predecessors
                .Should()
                .NotBeNull()
                .And
                .Subject
                .Should()
                .BeEquivalentTo(expectedPredecessors.Select(_ => _.ProviderId));
        }


        [Then(@"the upserted provider version for '(.*)' has the funding line totals")]
        public void ThenTheUpsertedProviderVersionForHasTheFundingLineTotals(string providerId,
            IEnumerable<ExpectedFundingLineTotal> expectedFundingLineTotals)
        {
            PublishedProviderVersion publishedProviderVersion = GetUpsertedPublishedProviderVersion(providerId);

            foreach (ExpectedFundingLineTotal expectedFundingLineTotal in expectedFundingLineTotals)
                publishedProviderVersion.FundingLines
                    .SingleOrDefault(_ => _.FundingLineCode == expectedFundingLineTotal.FundingLineCode)
                    ?.Value
                    .Should()
                    .NotBeNull()
                    .And
                    .Subject
                    .Should()
                    .Be(expectedFundingLineTotal.Value);
        }

        [Then(@"the provider variation reasons were recorded")]
        public void ThenTheProviderVariationReasonsWereRecorded(
            IEnumerable<ExpectedVariationReasons> expectedVariationReasons)
        {
            expectedVariationReasons
                .Should()
                .NotBeNullOrEmpty();

            Dictionary<string, IEnumerable<VariationReason>> variationReasons =
                expectedVariationReasons.ToDictionary(_ => _.ProviderId, _ => _.Reasons);

            foreach (PublishedProviderVersion publishedProviderVersion in _providerVersionInMemoryRepository
                .UpsertedPublishedProviderVersions)
            {
                string key = publishedProviderVersion.ProviderId;
                PublishedProviderVersion latestPublishedProviderVersion = publishedProviderVersion;

                if (variationReasons.ContainsKey(key))
                    latestPublishedProviderVersion
                        .VariationReasons
                        .Should()
                        .NotBeNullOrEmpty()
                        .And
                        .Subject
                        .Should()
                        .BeEquivalentTo(variationReasons[key]);
                else
                    latestPublishedProviderVersion
                        .VariationReasons
                        .Should()
                        .BeNullOrEmpty();
            }
        }

        [Then(@"the upserted provider version for '(.*)' has no funding line over payments for funding line '(.*)'")]
        public void ThenTheUpsertedProviderVersionForHasNoFundingLineOverPaymentsForFundingLine(string providerId,
            string fundingLineCode)
        {
            PublishedProviderVersion publishedProviderVersion = GetUpsertedPublishedProviderVersion(providerId);

            (publishedProviderVersion.CarryOvers ?? new List<ProfilingCarryOver>())
                .Any(_ => _.FundingLineCode == fundingLineCode)
                .Should()
                .BeFalse();
        }

        [Then(@"the upserted provider version for '(.*)' has the following funding line over payments")]
        public void ThenTheUpsertedProviderVersionForHasTheFollowingFundingLineOverPayments(string providerId,
            IEnumerable<ExpectedFundingLineOverPayment> expectedFundingLineOverPayments)
        {
            PublishedProviderVersion publishedProviderVersion = GetUpsertedPublishedProviderVersion(providerId);

            IEnumerable<ProfilingCarryOver> carryOvers = publishedProviderVersion.CarryOvers ?? new List<ProfilingCarryOver>();

            foreach (ExpectedFundingLineOverPayment expectedFundingLineOverPayment in expectedFundingLineOverPayments)
            {
                string fundingLineCode = expectedFundingLineOverPayment.FundingLineCode;

                ProfilingCarryOver carryOver = carryOvers.FirstOrDefault(_ => _.FundingLineCode == fundingLineCode);

                carryOver
                    .Should()
                    .BeEquivalentTo(new ProfilingCarryOver
                    {
                        FundingLineCode = fundingLineCode,
                        Type = ProfilingCarryOverType.DSGReProfiling,
                        Amount = expectedFundingLineOverPayment.OverPayment
                    });
            }
        }

        private PublishedProviderVersion GetUpsertedPublishedProviderVersion(string providerId)
        {
            return _providerVersionInMemoryRepository.UpsertedPublishedProviderVersions
                       .SingleOrDefault(_ => _.ProviderId == providerId) ??
                   throw new ArgumentOutOfRangeException(nameof(providerId), providerId);
        }
    }
}