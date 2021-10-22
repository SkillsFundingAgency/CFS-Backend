using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Api.Profiling.IntegrationTests.Data;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Config.ApiClient.Profiling;
using CalculateFunding.IntegrationTests.Common;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.Profiling.IntegrationTests.ReProfiling
{
    [TestClass]
    [TestCategory(nameof(IntegrationTest))]
    public class ZeroPercentagePeriodHandlingTests : IntegrationTest
    {
        private const string StrategyKey = nameof(ReProfileFlatDistributionWithZeroPercentProfilePatternPeriodHandling);

        private ProfilePatternDataContext _profilePatternDataContext;
        private IProfilingApiClient _profiling;
        private int _year;
        private string _month;

        [ClassInitialize]
        public static void FixtureSetUp(TestContext testContext)
        {
            SetUpConfiguration();
            SetUpServices((sc,
                        c)
                    => sc.AddProfilingInterServiceClient(c),
                AddCacheProvider,
                AddNullLogger,
                AddUserProvider);
        }

        [TestInitialize]
        public void SetUp()
        {
            _profilePatternDataContext = new ProfilePatternDataContext(Configuration);

            TrackForTeardown(_profilePatternDataContext);

            _profiling = GetService<IProfilingApiClient>();

            _year = NewRandomYear();
            _month = NewRandomMonth();
        }

        [TestMethod]
        [DynamicData(nameof(ZeroPercentPeriodExamples), DynamicDataSourceType.Method)]
        public async Task ZeroPercentPeriodsAreSkippedWhenReProfilingWithThatStrategy(decimal?[] existingProfiling,
            decimal[] profilePattern,
            decimal previousFundingValue,
            decimal currentFundingValue,
            int? variationPointer,
            decimal[] expectedProfiling,
            decimal? expectedCarryOver)
        {
            ProfilePatternTemplateParameters profilePatternTemplateParameters = NewProfilePatternTemplateParameters(_ => _
                .WithDecreasedAmountStrategyKey(StrategyKey)
                .WithIncreasedAmountStrategyKey(StrategyKey)
                .WithSameAmountStrategyKey(StrategyKey)
                .WithReProfilingEnabled(true)
                .WithProfilePattern(AsProfilePattern(profilePattern)));

            await GivenTheProfilePattern(profilePatternTemplateParameters);

            ReProfileRequest request = NewReProfileRequest(_ => _
                .WithNoProfilePatternKey()
                .WithFundingLineCode(profilePatternTemplateParameters.FundingLineId)
                .WithFundingStreamId(profilePatternTemplateParameters.FundingStream)
                .WithFundingPeriodId(profilePatternTemplateParameters.FundingPeriodId)
                .WithConfigurationType(ProfileConfigurationType.Custom)
                .WithFundingValue(currentFundingValue)
                .WithExistingFundingValue(previousFundingValue)
                .WithVariationPointerIndex(variationPointer)
                .WithExistingProfilePeriods(AsExistingProfilePeriods(existingProfiling)));

            ApiResponse<ReProfileResponse> response = await WhenTheFundingLineIsReProfiled(request);

            response.StatusCode
                .IsSuccess()
                .Should()
                .BeTrue($"ReProfile request failed with status code {response.StatusCode}");

            ReProfileResponse reProfileResponse = response?.Content;

            reProfileResponse
                .Should()
                .NotBeNull();

            AndTheFundingLinePeriodAmountsShouldBe(reProfileResponse, expectedProfiling);
            AndTheCarryOverShouldBe(reProfileResponse, expectedCarryOver);
        }

        public static IEnumerable<object[]> ZeroPercentPeriodExamples()
        {
            yield return new object[]
            {
                NewDecimals(3142.7M, 3142.7M, 3142.7M, (decimal?)null, null, null, null, null, null, null),
                NewDecimals(14.285M, 14.285M, 14.285M, 14.285M, 14.285M, 14.285M, 14.29M, 0, 0, 0),
                22000M,
                24000M,
                null,
                NewDecimals(3142.7M, 3142.7M, 3142.7M, 3641.98M, 3641.98M, 3641.98M, 3645.96M, 0, 0, 0),
                null
            };
            yield return new object[]
            {
                NewDecimals(3142.7M, 3142.7M, 3142.7M, (decimal?)null, null, null, null, null, null, null),
                NewDecimals(14.285M, 14.285M, 14.285M, 14.285M, 14.285M, 14.285M, 0, 14.29M, 0, 0),
                22000M,
                24000M,
                3,
                NewDecimals(3142.7M, 3142.7M, 3142.7M, 3641.98M, 3641.98M, 3641.98M, 0, 3645.96M, 0, 0),
                null
            };
            yield return new object[]
            {
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, (decimal?)null, null, null, null),
                NewDecimals(12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 0, 0, 0, 0),
                36000M,
                22000M,
                8,
                NewDecimals(3000, 3000, 3000, 3000, 3000, 3000, 3000, 3000, -500.0M, -500.0M, -500.0M, -500.0M),
                null
            };
            yield return new object[]
            {
                NewDecimals(2750M, 2750, 2750, 2750, 2750, 2750, 2750, (decimal?)null, null, null, null, null),
                NewDecimals(12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 12.5M, 0, 0, 0, 0),
                22000M,
                36000M,
                7,
                NewDecimals(2750M, 2750M, 2750M, 2750M, 2750M, 2750M, 2750M, 16750M, 0, 0, 0, 0),
                null
            };
        }

        protected void AndTheFundingLinePeriodAmountsShouldBe(ReProfileResponse response,
            params decimal[] expectedAmounts)
        {
            DeliveryProfilePeriod[] orderedProfilePeriods = new YearMonthOrderedDeliveryProfilePeriods(response?.DeliveryProfilePeriods).ToArray();

            orderedProfilePeriods
                .Length
                .Should()
                .Be(expectedAmounts.Length);

            for (int amount = 0; amount < expectedAmounts.Length; amount++)
            {
                orderedProfilePeriods[amount]
                    .ProfileValue
                    .Should()
                    .Be(expectedAmounts[amount], "Profiled value at index {0} should match expected value", amount);
            }
        }

        protected void AndTheCarryOverShouldBe(ReProfileResponse response,
            decimal? expectedOverPayment)
        {
            response.CarryOverAmount
                .Should()
                .Be(expectedOverPayment.GetValueOrDefault());
        }

        protected ProfilePeriodPattern[] AsProfilePattern(params decimal[] profilePeriodPercentages)
        {
            return profilePeriodPercentages.Select((percentage,
                        index) =>
                    NewProfilePeriodPattern(_ => _.WithPercentage(percentage)
                        .WithType(PeriodType.CalendarMonth)
                        .WithYear(_year)
                        .WithPeriod(_month)
                        .WithOccurrence(index)
                        .Build()))
                .ToArray();
        }

        protected ExistingProfilePeriod[] AsExistingProfilePeriods(decimal?[] periodValues)
        {
            return periodValues.Select((amount,
                        index) =>
                    NewExistingProfilePeriod(_ => _.WithProfiledValue(amount)
                        .WithPeriodType(PeriodType.CalendarMonth)
                        .WithOccurrence(index)
                        .WithTypeValue(_month)
                        .WithYear(_year)))
                .ToArray();
        }

        private async Task GivenTheProfilePattern(ProfilePatternTemplateParameters profilePatternTemplateParameters)
            => await _profilePatternDataContext.CreateContextData(profilePatternTemplateParameters);

        private async Task<ApiResponse<ReProfileResponse>> WhenTheFundingLineIsReProfiled(ReProfileRequest reProfileRequest)
            => await _profiling.ReProfile(reProfileRequest);

        private ProfilePatternTemplateParameters NewProfilePatternTemplateParameters(Action<ProfilePatternTemplateParametersBuilder> setUp = null)
        {
            ProfilePatternTemplateParametersBuilder profilePatternTemplateParametersBuilder = new ProfilePatternTemplateParametersBuilder();

            setUp?.Invoke(profilePatternTemplateParametersBuilder);

            return profilePatternTemplateParametersBuilder.Build();
        }

        private ProfilePeriodPattern NewProfilePeriodPattern(Action<ProfilePeriodPatternBuilder> setUp = null)
        {
            ProfilePeriodPatternBuilder profilePeriodPatternBuilder = new ProfilePeriodPatternBuilder();

            setUp?.Invoke(profilePeriodPatternBuilder);

            return profilePeriodPatternBuilder.Build();
        }

        private ReProfileRequest NewReProfileRequest(Action<ReProfileRequestBuilder> setUp = null)
        {
            ReProfileRequestBuilder reProfileRequestBuilder = new ReProfileRequestBuilder();

            setUp?.Invoke(reProfileRequestBuilder);

            return reProfileRequestBuilder.Build();
        }

        private ExistingProfilePeriod NewExistingProfilePeriod(Action<ExistingProfilePeriodBuilder> setUp = null)
        {
            ExistingProfilePeriodBuilder existingProfilePeriodBuilder = new ExistingProfilePeriodBuilder();

            setUp?.Invoke(existingProfilePeriodBuilder);

            return existingProfilePeriodBuilder.Build();
        }

        private static TItem[] NewDecimals<TItem>(params TItem[] items) => items; 

        private static int NewRandomYear() => NewRandomDateTime().Year;

        private static DateTime NewRandomDateTime() => new RandomDateTime();

        private static string NewRandomMonth() => NewRandomDateTime().ToString("MMMM");
    }
}