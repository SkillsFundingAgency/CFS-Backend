using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.UnitTests.Errors;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ApiProfilePeriod = CalculateFunding.Common.ApiClient.Profiling.Models.ProfilingPeriod;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class BatchProfilingContextTests
    {
        private BatchProfilingContext _context;

        [TestInitialize]
        public void SetUp()
        {
            _context = new BatchProfilingContext();
        }

        [TestMethod]
        [DynamicData(nameof(AddProviderProfilingRequestDataExamples), DynamicDataSourceType.Method)]
        public void AddsRequestDataForSuppliedPublishedProviderVersions(PublishedProviderVersion providerVersion,
            bool isNew,
            ProviderProfilingRequestData expectedRequestData)
        {
            WhenTheRequestDataIsAdded(providerVersion, isNew);

            _context.ProfilingRequests
                .Should()
                .BeEquivalentTo(expectedRequestData);
        }

        [TestMethod]
        [DynamicData(nameof(InitialisesItemsBatchExamples), DynamicDataSourceType.Method)]
        public void InitialisesRequestDataIntoProfilingBatches(ProviderProfilingRequestData[] requestData,
            ProfilingBatch[] expectedProfilingBatches)
        {
            GivenTheRequestData(requestData);

            WhenTheItemsAreInitialised(1, 10);

            _context.ProfilingBatches?
                .Values
                .Should()
                .BeEquivalentTo<ProfilingBatch>(expectedProfilingBatches);
        }

        [TestMethod]
        [DynamicData(nameof(InitialisesItemsRequestExamples), DynamicDataSourceType.Method)]
        public void InitialisesProfilingBatchesIntoBatchProfilingRequestModelsWithSuppliedBatchSize(ProviderProfilingRequestData[] requestData,
            int batchSize,
            IEnumerable<IEnumerable<BatchProfilingRequestModel>> expectedRequestModelPages)
        {
            GivenTheRequestData(requestData);

            WhenTheItemsAreInitialised(1, batchSize);

            int count = 0;

            foreach (IEnumerable<BatchProfilingRequestModel> expectedRequestModelPage in expectedRequestModelPages)
            {
                count++;

                _context.HasPages
                    .Should()
                    .BeTrue();

                _context.NextPage()
                    .Should()
                    .BeEquivalentTo(expectedRequestModelPage, $"Page number {count}");
            }
        }

        [TestMethod]
        public void ReconcileGuardsNoMatchingProfileBatchForAResponse()
        {
            Action invocation = () => WhenTheResponseIsReconciled(NewBatchProfilingResponseModel());

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .StartWith("Unable to reconcile profiling response. Could not locate batch for response");
        }

        [TestMethod]
        [DynamicData(nameof(ReconcileResponseExamples), DynamicDataSourceType.Method)]
        public void ReconcilesBatchProfilingResponsesToWaitingProfilingBatches(ProfilingBatch[] profilingBatches,
            BatchProfilingResponseModel response,
            ProfilePatternKey[] expectedProfilePatternKeys,
            DistributionPeriod[] expectedDistributionPeriods)
        {
            GivenTheProfileBatches(profilingBatches);

            WhenTheResponseIsReconciled(response);

            ProfilingBatch reconciledBatch = _context.ProfilingBatches[response.Key];

            foreach (FundingLine fundingLine in reconciledBatch.FundingLines)
            {
                fundingLine.DistributionPeriods
                    .Should()
                    .BeEquivalentTo<DistributionPeriod>(expectedDistributionPeriods);
            }

            foreach (PublishedProviderVersion providerVersion in reconciledBatch.PublishedProviders)
            {
                providerVersion.ProfilePatternKeys
                    .Should()
                    .BeEquivalentTo<ProfilePatternKey>(expectedProfilePatternKeys);
            }
        }

        private void WhenTheItemsAreInitialised(int pageSize,
            int batchSize)
            => _context.InitialiseItems(pageSize, batchSize);

        private void WhenTheResponseIsReconciled(BatchProfilingResponseModel response)
            => _context.ReconcileBatchProfilingResponse(response);

        private void GivenTheRequestData(ProviderProfilingRequestData[] requestData)
            => _context.ProfilingRequests.AddRange(requestData);

        private void GivenTheProfileBatches(params ProfilingBatch[] profilingBatches)
            => _context.ProfilingBatches = profilingBatches.ToDictionary(_ => _.Key);

        private static IEnumerable<object[]> ReconcileResponseExamples()
        {
            string fundingPeriodId = nameof(fundingPeriodId);
            string fundingStreamId = nameof(fundingStreamId);
            string providerType = nameof(providerType);
            string providerSubType = nameof(providerSubType);
            string profilePatternKey = nameof(profilePatternKey);
            string fundingLineCode = nameof(fundingLineCode);
            decimal fundingValue = 111.0M;

            string key = $"{fundingPeriodId}-{fundingStreamId}-{profilePatternKey}-{providerType}-{providerSubType}-{fundingLineCode}-{fundingValue:N4}";

            ProfilePatternKey expectedProfilePatternKey = NewProfilePatternKey(_ => _.WithKey(profilePatternKey)
                .WithFundingLineCode(fundingLineCode));

            ProfilingBatch profilingBatchOne = NewProfilingBatch(_ => _.WithProviderType(providerType)
                .WithProviderSubType(providerSubType)
                .WithFundingValue(fundingValue)
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingLineCode(fundingLineCode)
                .WithFundingLines(NewFundingLine(), NewFundingLine(), NewFundingLine())
                .WithProfilePatternKey(profilePatternKey)
                .WithPublishedProviders(NewPublishedProviderVersion(), NewPublishedProviderVersion()));
            ProfilingBatch profilingBatchTwo = NewProfilingBatch(_ => _.WithProviderType(NewRandomString()));
            ProfilingBatch profilingBatchThree = NewProfilingBatch(_ => _.WithProviderType(NewRandomString()));

            string distributionPeriodOneId = NewRandomString();
            string distributionPeriodTwoId = NewRandomString();

            decimal distributionPeriodOneValue = NewRandomFundingValue();
            decimal distributionPeriodTwoValue = NewRandomFundingValue();

            BatchProfilingResponseModel response = NewBatchProfilingResponseModel(_ => _
                .WithKey(key)
                .WithFundingValue(fundingValue)
                .WithProfilePatternKey(profilePatternKey)
                .WithDistributionPeriods(NewDistributionPeriods(dp => dp
                        .WithDistributionPeriodCode(distributionPeriodOneId)
                        .WithValue(distributionPeriodOneValue)),
                    NewDistributionPeriods(dp => dp
                        .WithDistributionPeriodCode(distributionPeriodTwoId)
                        .WithValue(distributionPeriodTwoValue)))
                .WithDeliveryProfilePeriods(NewApiProfilePeriod(pp => pp
                        .WithValue(111.0M)
                        .WithOccurrence(1)
                        .WithPeriod("Jan")
                        .WithType("CalendarMonth")
                        .WithYear(2021)
                        .WithDistributionPeriod(distributionPeriodOneId)),
                    NewApiProfilePeriod(pp => pp
                        .WithValue(222.0M)
                        .WithOccurrence(2)
                        .WithPeriod("Feb")
                        .WithType("CalendarMonth")
                        .WithYear(2021)
                        .WithDistributionPeriod(distributionPeriodOneId)),
                    NewApiProfilePeriod(pp => pp
                        .WithValue(333.0M)
                        .WithOccurrence(3)
                        .WithPeriod("Mar")
                        .WithType("CalendarMonth")
                        .WithYear(2021)
                        .WithDistributionPeriod(distributionPeriodTwoId)),
                    NewApiProfilePeriod(pp => pp
                        .WithValue(444.0M)
                        .WithOccurrence(4)
                        .WithPeriod("Apr")
                        .WithType("CalendarMonth")
                        .WithYear(2021)
                        .WithDistributionPeriod(distributionPeriodTwoId))));

            DistributionPeriod expectedDistributionPeriodOne = NewDistributionPeriod(_ => _
                .WithDistributionPeriodId(distributionPeriodOneId)
                .WithValue(distributionPeriodOneValue)
                .WithProfilePeriods(NewProfilePeriod(pp => pp
                        .WithOccurence(1)
                        .WithYear(2021)
                        .WithType(ProfilePeriodType.CalendarMonth)
                        .WithAmount(111.0M)
                        .WithDistributionPeriodId(distributionPeriodOneId)
                        .WithTypeValue("Jan")),
                    NewProfilePeriod(pp => pp
                        .WithOccurence(2)
                        .WithYear(2021)
                        .WithType(ProfilePeriodType.CalendarMonth)
                        .WithAmount(222)
                        .WithDistributionPeriodId(distributionPeriodOneId)
                        .WithTypeValue("Feb"))));
            DistributionPeriod expectedDistributionPeriodTwo = NewDistributionPeriod(_ => _
                .WithDistributionPeriodId(distributionPeriodTwoId)
                .WithValue(distributionPeriodTwoValue)
                .WithProfilePeriods(NewProfilePeriod(pp => pp
                        .WithOccurence(3)
                        .WithYear(2021)
                        .WithType(ProfilePeriodType.CalendarMonth)
                        .WithAmount(333.0M)
                        .WithDistributionPeriodId(distributionPeriodTwoId)
                        .WithTypeValue("Mar")),
                    NewProfilePeriod(pp => pp
                        .WithOccurence(4)
                        .WithYear(2021)
                        .WithType(ProfilePeriodType.CalendarMonth)
                        .WithAmount(444)
                        .WithDistributionPeriodId(distributionPeriodTwoId)
                        .WithTypeValue("Apr"))));

            yield return new object[]
            {
                new[]
                {
                    profilingBatchTwo,
                    profilingBatchOne,
                    profilingBatchThree
                },
                response,
                new[]
                {
                    expectedProfilePatternKey
                },
                new[]
                {
                    expectedDistributionPeriodOne,
                    expectedDistributionPeriodTwo
                }
            };
        }

        private static decimal NewRandomFundingValue() => new RandomNumberBetween(1, int.MaxValue);

        private static IEnumerable<object[]> InitialisesItemsBatchExamples()
        {
            string fundingPeriodOneId = nameof(fundingPeriodOneId);
            string fundingStreamOneId = nameof(fundingStreamOneId);
            string fundingLineCodeOne = nameof(fundingLineCodeOne);
            string fundingLineCodeTwo = nameof(fundingLineCodeTwo);
            string providerTypeOne = nameof(providerTypeOne);
            string providerTypeTwo = nameof(providerTypeTwo);
            string providerSubTypeOne = nameof(providerSubTypeOne);
            string profilePatternOne = nameof(profilePatternOne);
            string profilePatternTwo = nameof(profilePatternTwo);

            decimal fundingValueOne = 111;
            decimal fundingValueTwo = 222;
            decimal fundingValueThree = 333;

            FundingLine fundingLineOne = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueOne)
                .WithName(nameof(fundingLineOne)));
            FundingLine fundingLineTwo = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueTwo)
                .WithName(nameof(fundingLineTwo)));
            FundingLine fundingLineThree = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueThree)
                .WithName(nameof(fundingLineThree)));
            FundingLine fundingLineFour = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueOne)
                .WithName(nameof(fundingLineFour)));
            FundingLine fundingLineFive = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueThree)
                .WithName(nameof(fundingLineFive)));
            FundingLine fundingLineSix = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueThree)
                .WithName(nameof(fundingLineSix)));
            FundingLine fundingLineSeven = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueOne)
                .WithName(nameof(fundingLineSeven)));
            FundingLine fundingLineEight = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueTwo)
                .WithName(nameof(fundingLineEight)));

            PublishedProviderVersion publishedProviderVersionOne = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamOneId)
                .WithProviderId(nameof(publishedProviderVersionOne))
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithProfilePatternKeys(
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeOne)
                            .WithKey(profilePatternOne)),
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeTwo)
                            .WithKey(profilePatternTwo))));
            PublishedProviderVersion publishedProviderVersionTwo = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamOneId)
                .WithProviderId(nameof(publishedProviderVersionTwo))
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithProfilePatternKeys(
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeOne)
                            .WithKey(profilePatternOne)),
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeTwo)
                            .WithKey(profilePatternTwo))));
            PublishedProviderVersion publishedProviderVersionThree = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamOneId)
                .WithProviderId(nameof(publishedProviderVersionThree))
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithProfilePatternKeys(
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeOne)
                            .WithKey(profilePatternOne)),
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeTwo)
                            .WithKey(profilePatternTwo))));
            PublishedProviderVersion publishedProviderVersionFour = NewPublishedProviderVersion(_ =>
                _.WithFundingStreamId(fundingStreamOneId)
                    .WithProviderId(nameof(publishedProviderVersionFour))
                    .WithFundingPeriodId(fundingPeriodOneId)
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(pk =>
                            pk.WithFundingLineCode(fundingLineCodeOne)
                                .WithKey(profilePatternOne)),
                        NewProfilePatternKey(pk =>
                            pk.WithFundingLineCode(fundingLineCodeTwo)
                                .WithKey(profilePatternTwo))));

            ProviderProfilingRequestData requestDataOne = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionOne)
                .WithFundingLinesToProfile(fundingLineOne, fundingLineTwo));
            ProviderProfilingRequestData requestDataTwo = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeTwo)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionTwo)
                .WithFundingLinesToProfile(fundingLineThree, fundingLineFour));
            ProviderProfilingRequestData requestDataThree = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionThree)
                .WithFundingLinesToProfile(fundingLineFive, fundingLineSix));
            ProviderProfilingRequestData requestDataFour = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionFour)
                .WithFundingLinesToProfile(fundingLineSeven, fundingLineEight));

            ProfilingBatch profilingBatchOne = NewProfilingBatch(_ => _.WithFundingLines(fundingLineOne, fundingLineSeven)
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithFundingStreamId(fundingStreamOneId)
                .WithProfilePatternKey(profilePatternOne)
                .WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithFundingLineCode(fundingLineCodeOne)
                .WithFundingValue(fundingValueOne)
                .WithPublishedProviders(publishedProviderVersionOne, publishedProviderVersionFour));

            ProfilingBatch profilingBatchTwo = NewProfilingBatch(_ => _.WithFundingLines(fundingLineTwo, fundingLineEight)
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithFundingStreamId(fundingStreamOneId)
                .WithProfilePatternKey(profilePatternTwo)
                .WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithFundingLineCode(fundingLineCodeTwo)
                .WithFundingValue(fundingValueTwo)
                .WithPublishedProviders(publishedProviderVersionOne, publishedProviderVersionFour));

            ProfilingBatch profilingBatchThree = NewProfilingBatch(_ => _.WithFundingLines(fundingLineThree)
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithFundingStreamId(fundingStreamOneId)
                .WithProfilePatternKey(profilePatternOne)
                .WithProviderType(providerTypeTwo)
                .WithProviderSubType(providerSubTypeOne)
                .WithFundingLineCode(fundingLineCodeOne)
                .WithFundingValue(fundingValueThree)
                .WithPublishedProviders(publishedProviderVersionTwo));

            ProfilingBatch profilingBatchFour = NewProfilingBatch(_ => _.WithFundingLines(fundingLineFour)
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithFundingStreamId(fundingStreamOneId)
                .WithProfilePatternKey(profilePatternTwo)
                .WithProviderType(providerTypeTwo)
                .WithProviderSubType(providerSubTypeOne)
                .WithFundingLineCode(fundingLineCodeTwo)
                .WithFundingValue(fundingValueOne)
                .WithPublishedProviders(publishedProviderVersionTwo));

            ProfilingBatch profilingBatchFive = NewProfilingBatch(_ => _.WithFundingLines(fundingLineFive)
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithFundingStreamId(fundingStreamOneId)
                .WithProfilePatternKey(profilePatternOne)
                .WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithFundingLineCode(fundingLineCodeOne)
                .WithFundingValue(fundingValueThree)
                .WithPublishedProviders(publishedProviderVersionThree));

            ProfilingBatch profilingBatchSix = NewProfilingBatch(_ => _.WithFundingLines(fundingLineSix)
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithFundingStreamId(fundingStreamOneId)
                .WithProfilePatternKey(profilePatternTwo)
                .WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithFundingLineCode(fundingLineCodeTwo)
                .WithFundingValue(fundingValueThree)
                .WithPublishedProviders(publishedProviderVersionThree));

            yield return new object[]
            {
                new[]
                {
                    requestDataOne,
                    requestDataTwo,
                    requestDataThree,
                    requestDataFour
                },
                new[]
                {
                    profilingBatchOne,
                    profilingBatchTwo,
                    profilingBatchThree,
                    profilingBatchFour,
                    profilingBatchFive,
                    profilingBatchSix
                }
            };
        }

        private static IEnumerable<object[]> InitialisesItemsRequestExamples()
        {
            string fundingPeriodOneId = nameof(fundingPeriodOneId);
            string fundingStreamOneId = nameof(fundingStreamOneId);
            string fundingLineCodeOne = nameof(fundingLineCodeOne);
            string fundingLineCodeTwo = nameof(fundingLineCodeTwo);
            string providerTypeOne = nameof(providerTypeOne);
            string providerTypeTwo = nameof(providerTypeTwo);
            string providerSubTypeOne = nameof(providerSubTypeOne);
            string profilePatternOne = nameof(profilePatternOne);
            string profilePatternTwo = nameof(profilePatternTwo);

            decimal fundingValueOne = 111;
            decimal fundingValueTwo = 222;
            decimal fundingValueThree = 333;

            FundingLine fundingLineOne = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueOne)
                .WithName(nameof(fundingLineOne)));
            FundingLine fundingLineTwo = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueTwo)
                .WithName(nameof(fundingLineTwo)));
            FundingLine fundingLineThree = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueThree)
                .WithName(nameof(fundingLineThree)));
            FundingLine fundingLineFour = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueOne)
                .WithName(nameof(fundingLineFour)));
            FundingLine fundingLineFive = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueThree)
                .WithName(nameof(fundingLineFive)));
            FundingLine fundingLineSix = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueThree)
                .WithName(nameof(fundingLineSix)));
            FundingLine fundingLineSeven = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeOne)
                .WithValue(fundingValueOne)
                .WithName(nameof(fundingLineSeven)));
            FundingLine fundingLineEight = NewFundingLine(_ => _.WithFundingLineCode(fundingLineCodeTwo)
                .WithValue(fundingValueTwo)
                .WithName(nameof(fundingLineEight)));

            PublishedProviderVersion publishedProviderVersionOne = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamOneId)
                .WithProviderId(nameof(publishedProviderVersionOne))
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithProfilePatternKeys(
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeOne)
                            .WithKey(profilePatternOne)),
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeTwo)
                            .WithKey(profilePatternTwo))));
            PublishedProviderVersion publishedProviderVersionTwo = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamOneId)
                .WithProviderId(nameof(publishedProviderVersionTwo))
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithProfilePatternKeys(
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeOne)
                            .WithKey(profilePatternOne)),
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeTwo)
                            .WithKey(profilePatternTwo))));
            PublishedProviderVersion publishedProviderVersionThree = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamOneId)
                .WithProviderId(nameof(publishedProviderVersionThree))
                .WithFundingPeriodId(fundingPeriodOneId)
                .WithProfilePatternKeys(
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeOne)
                            .WithKey(profilePatternOne)),
                    NewProfilePatternKey(pk =>
                        pk.WithFundingLineCode(fundingLineCodeTwo)
                            .WithKey(profilePatternTwo))));
            PublishedProviderVersion publishedProviderVersionFour = NewPublishedProviderVersion(_ =>
                _.WithFundingStreamId(fundingStreamOneId)
                    .WithProviderId(nameof(publishedProviderVersionFour))
                    .WithFundingPeriodId(fundingPeriodOneId)
                    .WithProfilePatternKeys(
                        NewProfilePatternKey(pk =>
                            pk.WithFundingLineCode(fundingLineCodeOne)
                                .WithKey(profilePatternOne)),
                        NewProfilePatternKey(pk =>
                            pk.WithFundingLineCode(fundingLineCodeTwo)
                                .WithKey(profilePatternTwo))));

            ProviderProfilingRequestData requestDataOne = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionOne)
                .WithFundingLinesToProfile(fundingLineOne, fundingLineTwo));
            ProviderProfilingRequestData requestDataTwo = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeTwo)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionTwo)
                .WithFundingLinesToProfile(fundingLineThree, fundingLineFour));
            ProviderProfilingRequestData requestDataThree = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionThree)
                .WithFundingLinesToProfile(fundingLineFive, fundingLineSix));
            ProviderProfilingRequestData requestDataFour = NewProviderProfilingRequestData(_ => _.WithProviderType(providerTypeOne)
                .WithProviderSubType(providerSubTypeOne)
                .WithProfilePatternKeys((fundingLineCodeOne, profilePatternOne),
                    (fundingLineCodeTwo, profilePatternTwo))
                .WithPublishedProviderVersion(publishedProviderVersionFour)
                .WithFundingLinesToProfile(fundingLineSeven, fundingLineEight));

            yield return new object[]
            {
                new[]
                {
                    requestDataOne,
                    requestDataTwo,
                    requestDataThree,
                    requestDataFour
                },
                10,
                new[]
                {
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueOne, fundingValueThree)
                        .WithFundingLineCode(fundingLineCodeOne)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternOne)
                        .WithProviderType(providerTypeOne)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueTwo, fundingValueThree)
                        .WithFundingLineCode(fundingLineCodeTwo)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternTwo)
                        .WithProviderType(providerTypeOne)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueThree)
                        .WithFundingLineCode(fundingLineCodeOne)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternOne)
                        .WithProviderType(providerTypeTwo)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueOne)
                        .WithFundingLineCode(fundingLineCodeTwo)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternTwo)
                        .WithProviderType(providerTypeTwo)
                        .WithProviderSubType(providerSubTypeOne)))
                }
            };
            yield return new object[]
            {
                new[]
                {
                    requestDataOne,
                    requestDataTwo,
                    requestDataThree,
                    requestDataFour
                },
                1,
                new[]
                {
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueOne)
                        .WithFundingLineCode(fundingLineCodeOne)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternOne)
                        .WithProviderType(providerTypeOne)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueThree)
                        .WithFundingLineCode(fundingLineCodeOne)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternOne)
                        .WithProviderType(providerTypeOne)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueTwo)
                        .WithFundingLineCode(fundingLineCodeTwo)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternTwo)
                        .WithProviderType(providerTypeOne)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueThree)
                        .WithFundingLineCode(fundingLineCodeTwo)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternTwo)
                        .WithProviderType(providerTypeOne)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueThree)
                        .WithFundingLineCode(fundingLineCodeOne)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternOne)
                        .WithProviderType(providerTypeTwo)
                        .WithProviderSubType(providerSubTypeOne))),
                    NewArray(NewBatchProfilingRequestModel(_ => _.WithFundingValues(fundingValueOne)
                        .WithFundingLineCode(fundingLineCodeTwo)
                        .WithFundingPeriodId(fundingPeriodOneId)
                        .WithFundingStreamId(fundingStreamOneId)
                        .WithProfilePatternKey(profilePatternTwo)
                        .WithProviderType(providerTypeTwo)
                        .WithProviderSubType(providerSubTypeOne)))
                }
            };
        }

        private static IEnumerable<object[]> AddProviderProfilingRequestDataExamples()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string profilePatternKey = NewRandomString();
            string providerType = NewRandomString();
            string providerSubType = NewRandomString();
            string fundingLineCodeOne = NewRandomString();

            FundingLine paymentFundingLineOne = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                .WithFundingLineCode(fundingLineCodeOne));
            FundingLine paymentFundingLineTwo = NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment));

            ProfilePatternKey fundingLinePatternKey = NewProfilePatternKey(ppk =>
                ppk.WithKey(profilePatternKey)
                    .WithFundingLineCode(fundingLineCodeOne));

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithFundingPeriodId(fundingPeriodId)
                    .WithFundingStreamId(fundingStreamId)
                    .WithProfilePatternKeys(fundingLinePatternKey)
                    .WithProvider(NewProvider(p =>
                        p.WithProviderType(providerType)
                            .WithProviderSubType(providerSubType)))
                    .WithFundingLines(paymentFundingLineOne,
                        NewFundingLine(fl =>
                            fl.WithFundingLineType(FundingLineType.Information)),
                        paymentFundingLineTwo));

            yield return new object[]
            {
                publishedProviderVersion,
                false,
                NewProviderProfilingRequestData(_ => _.WithProfilePatternKeys((fundingLineCodeOne, profilePatternKey))
                    .WithPublishedProviderVersion(publishedProviderVersion)
                    .WithFundingLinesToProfile(paymentFundingLineOne, paymentFundingLineTwo))
            };
            yield return new object[]
            {
                publishedProviderVersion,
                true,
                NewProviderProfilingRequestData(_ => _.WithPublishedProviderVersion(publishedProviderVersion)
                    .WithFundingLinesToProfile(paymentFundingLineOne, paymentFundingLineTwo)
                    .WithProviderSubType(providerSubType)
                    .WithProviderType(providerType))
            };
        }

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private static ProfilePatternKey NewProfilePatternKey(Action<ProfilePatternKeyBuilder> setUp = null)
        {
            ProfilePatternKeyBuilder profilePatternKeyBuilder = new ProfilePatternKeyBuilder();

            setUp?.Invoke(profilePatternKeyBuilder);

            return profilePatternKeyBuilder.Build();
        }

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        public static ProviderProfilingRequestData NewProviderProfilingRequestData(Action<ProviderProfilingRequestDataBuilder> setUp = null)
        {
            ProviderProfilingRequestDataBuilder profilingRequestDataBuilder = new ProviderProfilingRequestDataBuilder();

            setUp?.Invoke(profilingRequestDataBuilder);

            return profilingRequestDataBuilder.Build();
        }

        private static ProfilingBatch NewProfilingBatch(Action<ProfilingBatchBuilder> setUp = null)
        {
            ProfilingBatchBuilder profilingBatchBuilder = new ProfilingBatchBuilder();

            setUp?.Invoke(profilingBatchBuilder);

            return profilingBatchBuilder.Build();
        }

        private static BatchProfilingRequestModel NewBatchProfilingRequestModel(Action<BatchProfilingRequestModelBuilder> setUp = null)
        {
            BatchProfilingRequestModelBuilder batchProfilingRequestModelBuilder = new BatchProfilingRequestModelBuilder();

            setUp?.Invoke(batchProfilingRequestModelBuilder);

            return batchProfilingRequestModelBuilder.Build();
        }

        private static BatchProfilingResponseModel NewBatchProfilingResponseModel(Action<BatchProfilingResponseModelBuilder> setUp = null)
        {
            BatchProfilingResponseModelBuilder profilingResponseModelBuilder = new BatchProfilingResponseModelBuilder();

            setUp?.Invoke(profilingResponseModelBuilder);

            return profilingResponseModelBuilder.Build();
        }

        private static DistributionPeriods NewDistributionPeriods(Action<DistributionPeriodsBuilder> setUp = null)
        {
            DistributionPeriodsBuilder deliveryProfilePeriodBuilder = new DistributionPeriodsBuilder();

            setUp?.Invoke(deliveryProfilePeriodBuilder);

            return deliveryProfilePeriodBuilder.Build();
        }

        private static DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);

            return distributionPeriodBuilder.Build();
        }

        private static ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
        }

        private static ApiProfilePeriod NewApiProfilePeriod(Action<ApiProfilePeriodBuilder> setUp = null)
        {
            ApiProfilePeriodBuilder apiProfilePeriodBuilder = new ApiProfilePeriodBuilder();

            setUp?.Invoke(apiProfilePeriodBuilder);

            return apiProfilePeriodBuilder.Build();
        }

        private void WhenTheRequestDataIsAdded(PublishedProviderVersion publishedProviderVersion,
            bool isNew)
            => _context.AddProviderProfilingRequestData(publishedProviderVersion,
                new Dictionary<string, GeneratedProviderResult>
                {
                    {
                        publishedProviderVersion.ProviderId, new GeneratedProviderResult
                        {
                            FundingLines = publishedProviderVersion.FundingLines
                        }
                    }
                },
                isNew);

        private static TItem[] NewArray<TItem>(params TItem[] requests) => requests;

        private static string NewRandomString() => new RandomString();
    }
}