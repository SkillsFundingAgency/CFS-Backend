using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Services.Profiling.Services;
using CalculateFunding.Services.Profiling.Tests.Services;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Profiling.Tests
{
    [TestClass]
    public class ReProfilingServiceTests
    {
        private Mock<IProfilePatternService> _profilePatterns;
        private Mock<IReProfilingStrategyLocator> _strategies;
        private Mock<ICalculateProfileService> _profiling;
        private Mock<IReProfilingStrategy> _reProfilingStrategy;

        private ReProfilingService _service;

        [TestInitialize]
        public void SetUp()
        {
            _profilePatterns = new Mock<IProfilePatternService>();
            _strategies = new Mock<IReProfilingStrategyLocator>();
            _profiling = new Mock<ICalculateProfileService>();

            _service = new ReProfilingService(_profilePatterns.Object,
                _strategies.Object,
                _profiling.Object);

            _reProfilingStrategy = new Mock<IReProfilingStrategy>();

            //TODO: check whether we need resilience setup here also
        }

        [TestMethod]
        public async Task ReturnsNotFoundIfUnableToLocateMatchingProfilingPatternToReProfile()
        {
            ActionResult<ReProfileResponse> result = await WhenTheFundingLineIsReProfiled(NewReProfileRequest());

            result
                .Result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Profile pattern not found");
            ;
        }

        [TestMethod]
        public async Task ReturnsBadRequestIfTheMatchingPatternHasNoReProfilingConfiguration()
        {
            ReProfileRequest request = NewReProfileRequest();

            GivenTheProfilePattern(request, NewFundingStreamPeriodProfilePattern());

            ActionResult<ReProfileResponse> result = await WhenTheFundingLineIsReProfiled(request);

            result
                .Result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Re-profiling is not enabled or has not been configured");
        }

        [TestMethod]
        public async Task ReturnsBadRequestIfTheMatchingPatternHasNoEnabledReProfilingConfiguration()
        {
            ReProfileRequest request = NewReProfileRequest();

            GivenTheProfilePattern(request,
                NewFundingStreamPeriodProfilePattern(_ =>
                    _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                        cfg.WithIsEnabled(false)))));

            ActionResult<ReProfileResponse> result = await WhenTheFundingLineIsReProfiled(request);

            result
                .Result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Re-profiling is not enabled or has not been configured");
        }

        [TestMethod]
        public async Task ReturnsBadRequestIfTheReProfilingConfigurationSuppliesAnUnsupportedStrategy()
        {
            ReProfileRequest request = NewReProfileRequest();

            GivenTheProfilePattern(request,
                NewFundingStreamPeriodProfilePattern(_ =>
                    _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                        cfg.WithIsEnabled(true)))));

            ActionResult<ReProfileResponse> result = await WhenTheFundingLineIsReProfiled(request);

            result
                .Result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Re-profiling is not enabled for this scenario or the strategy was not found");
        }

        [TestMethod]
        public void GuardsAgainstProfiledAmountsAndCarryOverNotBeingSameAsFundingValueInTheSuppliedRequestAfterReProfiling()
        {
            decimal newFundingTotal = NewRandomTotal();

            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal + 100)
                .WithExistingFundingValue(newFundingTotal));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            string key = NewRandomString();

            FundingStreamPeriodProfilePattern profilePattern = NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithIncreasedAmountStrategyKey(key))));

            GivenTheProfilePattern(request, profilePattern);
            AndTheReProfilingStrategy(key);
            AndTheProfiling(request, profilePattern, profileResponse);
            AndTheReProfilingStrategyResponse(profileResponse, request, profilePattern, NewReProfileStrategyResult());

            Func<Task> invocation = async() => await WhenTheFundingLineIsReProfiled(request);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Profile amounts (0) and carry over amount (0) does not equal funding line total requested ({request.FundingLineTotal}) from strategy.");
        }

        [TestMethod]
        public async Task ProfilesFundingLinesNormallyThenReProfilesUsingTheseResultsWithLessThanStrategyIfFundingDropped()
        {
            decimal newFundingTotal = NewRandomTotal();
            ReProfileFutureDistributionPeriodsWithAdjustments reProfileFutureDistributionPeriodsWithAdjustments = new ReProfileFutureDistributionPeriodsWithAdjustments();
            AndTheStrategy(reProfileFutureDistributionPeriodsWithAdjustments);

            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal + 100));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            string key = "DecreasedAmountStrategyKey";

            FundingStreamPeriodProfilePattern profilePattern = NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithDecreasedAmountStrategyKey(key))));

            DistributionPeriods distributionPeriods1 = NewDistributionPeriods();
            DeliveryProfilePeriod deliveryProfilePeriod1 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(10));
            DeliveryProfilePeriod deliveryProfilePeriod2 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(newFundingTotal - 20));

            GivenTheProfilePattern(request, profilePattern);
            AndTheReProfilingStrategy(key);
            AndTheProfiling(request, profilePattern, profileResponse);
            AndTheReProfilingStrategyResponse(profileResponse, request, profilePattern, NewReProfileStrategyResult(_ => 
                _.WithDistributionPeriods(distributionPeriods1)
                    .WithDeliveryProfilePeriods(deliveryProfilePeriod1, deliveryProfilePeriod2)
                    .WithCarryOverAmount(10)));

            ActionResult<ReProfileResponse> reProfileResponse = await WhenTheFundingLineIsReProfiled(request);
            
            reProfileResponse?
                .Value
                .Should()
                .BeEquivalentTo(
                    GetProfileResponse(
                        new[] { distributionPeriods1 },
                        new[] { deliveryProfilePeriod1, deliveryProfilePeriod2 },
                        10,
                        profilePattern,
                        reProfileFutureDistributionPeriodsWithAdjustments.StrategyKey,
                        key
                    )
                );
        }

        [TestMethod]
        public async Task ProfilesFundingLinesNormallyThenReProfilesUsingTheseResultsWithMoreThanStrategyIfFundingIncreased()
        {
            decimal newFundingTotal = NewRandomTotal();
            ReProfileFutureDistributionPeriodsWithAdjustments reProfileFutureDistributionPeriodsWithAdjustments = new ReProfileFutureDistributionPeriodsWithAdjustments();
            AndTheStrategy(reProfileFutureDistributionPeriodsWithAdjustments);
            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal * -1));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            string key = "IncreasedAmountStrategyKey";

            FundingStreamPeriodProfilePattern profilePattern = NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithIncreasedAmountStrategyKey(key))));

            DistributionPeriods distributionPeriods1 = NewDistributionPeriods();
            DeliveryProfilePeriod deliveryProfilePeriod1 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(10));
            DeliveryProfilePeriod deliveryProfilePeriod2 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(newFundingTotal - 20));

            GivenTheProfilePattern(request, profilePattern);
            AndTheReProfilingStrategy(key);
            AndTheProfiling(request, profilePattern, profileResponse);
            AndTheReProfilingStrategyResponse(profileResponse, request, profilePattern, NewReProfileStrategyResult(_ => 
                _.WithDistributionPeriods(distributionPeriods1)
                    .WithDeliveryProfilePeriods(deliveryProfilePeriod1, deliveryProfilePeriod2)
                    .WithCarryOverAmount(10)));

            ActionResult<ReProfileResponse> reProfileResponse = await WhenTheFundingLineIsReProfiled(request);
            
            reProfileResponse?
                .Value
                .Should()
                .BeEquivalentTo(
                    GetProfileResponse(
                        new[] { distributionPeriods1 },
                        new[] { deliveryProfilePeriod1, deliveryProfilePeriod2 },
                        10,
                        profilePattern,
                        reProfileFutureDistributionPeriodsWithAdjustments.StrategyKey,
                        key
                    )
                );
        }

        [TestMethod]
        public async Task ProfilesFundingLinesNormallyThenReProfilesUsingTheseResultsWithSameAmountStrategyIfFundingTheSame()
        {
            decimal newFundingTotal = NewRandomTotal();
            string key = "SameAmountStrategyKey";
            ReProfileFlatDistributionForRemainingPeriods reProfileFlatDistributionForRemainingPeriods = new ReProfileFlatDistributionForRemainingPeriods();
            AndTheStrategy(reProfileFlatDistributionForRemainingPeriods);

            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            FundingStreamPeriodProfilePattern profilePattern = NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithSameAmountStrategyKey(key))));

            DistributionPeriods distributionPeriods1 = NewDistributionPeriods();
            DeliveryProfilePeriod deliveryProfilePeriod1 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(10));
            DeliveryProfilePeriod deliveryProfilePeriod2 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(newFundingTotal - 20));

            GivenTheProfilePattern(request, profilePattern);
            AndTheReProfilingStrategy(key);
            AndTheProfiling(request, profilePattern, profileResponse);
            AndTheReProfilingStrategyResponse(profileResponse, request, profilePattern, NewReProfileStrategyResult(_ => 
                _.WithDistributionPeriods(distributionPeriods1)
                    .WithDeliveryProfilePeriods(deliveryProfilePeriod1, deliveryProfilePeriod2)
                    .WithCarryOverAmount(10)));

            ActionResult<ReProfileResponse> reProfileResponse = await WhenTheFundingLineIsReProfiled(request);
            
            reProfileResponse?
                .Value
                .Should()
                .BeEquivalentTo(
                    GetProfileResponse(
                        new[] { distributionPeriods1 },
                        new[] { deliveryProfilePeriod1, deliveryProfilePeriod2},
                        10,
                        profilePattern,
                        reProfileFlatDistributionForRemainingPeriods.StrategyKey,
                        key
                    )
                );
        }

        [TestMethod]
        public async Task ReturnsSkipProfilingResultWithSkipProfilingStrategySetInConfiguration()
        {
            decimal newFundingTotal = NewRandomTotal();
            string key = nameof(SkipReProfilingStrategy);
            SkipReProfilingStrategy strategy = new SkipReProfilingStrategy();

            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            FundingStreamPeriodProfilePattern profilePattern = NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithSameAmountStrategyKey(key))));

            DistributionPeriods distributionPeriods1 = NewDistributionPeriods();
            DeliveryProfilePeriod deliveryProfilePeriod1 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(10));
            DeliveryProfilePeriod deliveryProfilePeriod2 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(newFundingTotal - 20));

            GivenTheProfilePattern(request, profilePattern);
            AndTheReProfilingStrategy(key, strategy);

            ActionResult<ReProfileResponse> reProfileResponse = await WhenTheFundingLineIsReProfiled(request);

            reProfileResponse?
                .Value
                .Should()
                .BeEquivalentTo(
                    new ReProfileResponse { SkipReProfiling = true}
                );
        }

        [TestMethod]
        [DynamicData(nameof(ProfilePatternExamples), DynamicDataSourceType.Method)]
        public async Task ProfilesFundingLinesNormallyThenReProfilesUsingTheseResultsWithMidYearStrategy(MidYearType midYearType, IReProfilingStrategy strategy, string key, FundingStreamPeriodProfilePattern profilePattern)
        {
            decimal newFundingTotal = NewRandomTotal();

            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal * -1)
                .WithMidYearCatchup(midYearType));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            DistributionPeriods distributionPeriods1 = NewDistributionPeriods();
            DeliveryProfilePeriod deliveryProfilePeriod1 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(10));
            DeliveryProfilePeriod deliveryProfilePeriod2 = NewDeliveryProfilePeriod(_ => _.WithProfiledValue(newFundingTotal - 20));

            GivenTheProfilePattern(request, profilePattern);
            AndTheStrategy(strategy);

            AndTheReProfilingStrategy(key);
            AndTheProfiling(request, profilePattern, profileResponse);
            AndTheReProfilingStrategyResponse(profileResponse, request, profilePattern, NewReProfileStrategyResult(_ => 
                _.WithDistributionPeriods(distributionPeriods1)
                    .WithDeliveryProfilePeriods(deliveryProfilePeriod1, deliveryProfilePeriod2)
                    .WithCarryOverAmount(10)));

            ActionResult<ReProfileResponse> reProfileResponse = await WhenTheFundingLineIsReProfiled(request);
            
            reProfileResponse?
                .Value
                .Should()
                .BeEquivalentTo(
                    GetProfileResponse(
                        new[] { distributionPeriods1 },
                        new[] { deliveryProfilePeriod1, deliveryProfilePeriod2 },
                        10,
                        profilePattern,
                        strategy.StrategyKey,
                        key
                    )
                );
        }

        private ReProfileResponse GetProfileResponse(DistributionPeriods[] distributionPeriods,
            DeliveryProfilePeriod[] deliveryProfilePeriods,
            decimal carryOveramount,
            FundingStreamPeriodProfilePattern profilePattern,
            string strategy,
            string strategyKey)
                => new ReProfileResponse
                        {
                            DistributionPeriods = distributionPeriods,
                            CarryOverAmount = carryOveramount,
                            DeliveryProfilePeriods = deliveryProfilePeriods,
                            ProfilePatternKey = profilePattern.ProfilePatternKey,
                            ProfilePatternDisplayName = profilePattern.ProfilePatternDisplayName,
                            Strategy = strategy,
                            StrategyConfigKey = strategyKey
                };

        private async Task<ActionResult<ReProfileResponse>> WhenTheFundingLineIsReProfiled(ReProfileRequest request)
            => await _service.ReProfile(request);

        private void GivenTheProfilePattern(ReProfileRequest request,
            FundingStreamPeriodProfilePattern profilePattern)
            => _profilePatterns.Setup(_ => _.GetProfilePattern(request.FundingStreamId,
                    request.FundingPeriodId,
                    request.FundingLineCode,
                    request.ProfilePatternKey))
                .ReturnsAsync(profilePattern);

        private void AndTheReProfilingStrategy(string key, IReProfilingStrategy strategy = null)
            => _strategies.Setup(_ => _.GetStrategy(key))
                .Returns(strategy ?? _reProfilingStrategy.Object);

        private void AndTheStrategy(IReProfilingStrategy strategy) => _reProfilingStrategy.Setup(_ => _.StrategyKey)
                .Returns(strategy.StrategyKey);

        private void AndTheReProfilingStrategyResponse(AllocationProfileResponse profileResponse,
            ReProfileRequest request,
            FundingStreamPeriodProfilePattern profilePattern,
            ReProfileStrategyResult response)
            => _reProfilingStrategy.Setup(_ => _.ReProfile(It.Is<ReProfileContext>(ctx =>
                    ReferenceEquals(ctx.Request, request) &&
                    ReferenceEquals(ctx.ProfilePattern, profilePattern) &&
                    ReferenceEquals(ctx.ProfileResult, profileResponse))))
                .Returns(response);

        private void AndTheProfiling(ReProfileRequest request,
            FundingStreamPeriodProfilePattern profilePattern,
            AllocationProfileResponse response)
            => _profiling.Setup(_ => _.ProfileAllocation(It.Is<ProfileRequest>(req =>
                        req.FundingStreamId == request.FundingStreamId &&
                        req.FundingPeriodId == request.FundingPeriodId &&
                        req.FundingLineCode == request.FundingLineCode &&
                        req.FundingValue == request.FundingLineTotal &&
                        req.ProfilePatternKey == request.ProfilePatternKey),
                    profilePattern,
                    request.FundingLineTotal))
                .Returns(response);

        public static IEnumerable<object[]> ProfilePatternExamples()
        {
            yield return new object[]
            {
                MidYearType.Opener,
                new ReProfileFlatDistributionForRemainingPeriods(),
                "InitialFundingStrategyKey",
                NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithMidYearStrategyKey("InitialFundingStrategyKey"))))
            };

            yield return new object[]
            {
                MidYearType.OpenerCatchup,
                new ReProfileFlatDistributionForRemainingPeriods(),
                "InitialFundingStrategyWithCatchupKey",
                NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithMidYearCatchUpStrategyKey("InitialFundingStrategyWithCatchupKey"))))
            };

            yield return new object[]
            {
                MidYearType.Converter,
                new ReProfileFlatDistributionForRemainingPeriods(),
                "ConverterFundingStrategyKey",
                NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithMidYearConverterStrategyKey("ConverterFundingStrategyKey"))))
            };

            yield return new object[]
            {
                MidYearType.Closure,
                new ReProfileFlatDistributionForRemainingPeriods(),
                "InitialClosureFundingStrategyKey",
                NewFundingStreamPeriodProfilePattern(_ =>
                _.WithReProfilingConfiguration(NewProfilePatternReProfilingConfiguration(cfg =>
                    cfg.WithIsEnabled(true)
                        .WithMidYearClosureStrategyKey("InitialClosureFundingStrategyKey"))))
            };
        }

        private static FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder profilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(profilePatternBuilder);

            return profilePatternBuilder.Build();
        }

        private static ProfilePatternReProfilingConfiguration NewProfilePatternReProfilingConfiguration(Action<ProfilePatternReProfilingConfigurationBuilder> setUp = null)
        {
            ProfilePatternReProfilingConfigurationBuilder reProfilingConfigurationBuilder = new ProfilePatternReProfilingConfigurationBuilder();

            setUp?.Invoke(reProfilingConfigurationBuilder);

            return reProfilingConfigurationBuilder.Build();
        }

        private ReProfileRequest NewReProfileRequest(Action<ReProfileRequestBuilder> setUp = null)
        {
            ReProfileRequestBuilder reProfileRequestBuilder = new ReProfileRequestBuilder();

            setUp?.Invoke(reProfileRequestBuilder);

            return reProfileRequestBuilder.Build();
        }

        private ReProfileStrategyResult NewReProfileStrategyResult(Action<ReProfileStrategyResultBuilder> setUp = null)
        {
            ReProfileStrategyResultBuilder strategyResultBuilder = new ReProfileStrategyResultBuilder();

            setUp?.Invoke(strategyResultBuilder);

            return strategyResultBuilder.Build();
        }

        private DistributionPeriods NewDistributionPeriods(Action<DistributionPeriodsBuilder> setUp = null)
        {
            DistributionPeriodsBuilder periodsBuilder = new DistributionPeriodsBuilder();

            setUp?.Invoke(periodsBuilder);
            
            return periodsBuilder.Build();
        }

        private DeliveryProfilePeriod NewDeliveryProfilePeriod(Action<DeliveryProfilePeriodBuilder> setUp = null)
        {
            DeliveryProfilePeriodBuilder profilePeriodBuilder = new DeliveryProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);
            
            return profilePeriodBuilder.Build();
        } 

        private AllocationProfileResponse NewAllocationProfileResponse() => new AllocationProfileResponseBuilder()
            .Build();

        private string NewRandomString() => new RandomString();

        private decimal NewRandomTotal() => new RandomNumberBetween(999, int.MaxValue);
    }
}