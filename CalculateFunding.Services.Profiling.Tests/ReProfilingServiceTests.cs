using System;
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
            ReProfileRequest request = NewReProfileRequest();
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

            Func<Task<ActionResult<ReProfileResponse>>> invocation = () => WhenTheFundingLineIsReProfiled(request);

            invocation
                .Should()
                .ThrowAsync<InvalidOperationException>()
                .Result
                .Which
                .Message
                .Should()
                .Be($"Profile amounts (0) and carry over amount (0) does not equal funding line total requested ({request.FundingLineTotal}) from strategy.");
        }

        [TestMethod]
        public async Task ProfilesFundingLinesNormallyThenReProfilesUsingTheseResultsWithLessThanStrategyIfFundingDropped()
        {
            decimal newFundingTotal = NewRandomTotal();
            
            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal + 100));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            string key = NewRandomString();

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
                .BeEquivalentTo(new ReProfileResponse
                {
                    DistributionPeriods = new [] { distributionPeriods1 },
                    CarryOverAmount = 10,
                    DeliveryProfilePeriods = new [] { deliveryProfilePeriod1, deliveryProfilePeriod2 },
                    ProfilePatternKey = profilePattern.ProfilePatternKey,
                    ProfilePatternDisplayName = profilePattern.ProfilePatternDisplayName
                });
        }

        [TestMethod]
        public async Task ProfilesFundingLinesNormallyThenReProfilesUsingTheseResultsWithMoreThanStrategyIfFundingIncreased()
        {
            decimal newFundingTotal = NewRandomTotal();
            
            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal - 100));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            string key = NewRandomString();

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
                .BeEquivalentTo(new ReProfileResponse
                {
                    DistributionPeriods = new [] { distributionPeriods1 },
                    CarryOverAmount = 10,
                    DeliveryProfilePeriods = new [] { deliveryProfilePeriod1, deliveryProfilePeriod2 },
                    ProfilePatternKey = profilePattern.ProfilePatternKey,
                    ProfilePatternDisplayName = profilePattern.ProfilePatternDisplayName
                });
        }

        [TestMethod]
        public async Task ProfilesFundingLinesNormallyThenReProfilesUsingTheseResultsWithSameAmountStrategyIfFundingTheSame()
        {
            decimal newFundingTotal = NewRandomTotal();
            
            ReProfileRequest request = NewReProfileRequest(_ => _.WithFundingValue(newFundingTotal)
                .WithExistingFundingValue(newFundingTotal));
            AllocationProfileResponse profileResponse = NewAllocationProfileResponse();

            string key = NewRandomString();

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
                .BeEquivalentTo(new ReProfileResponse
                {
                    DistributionPeriods = new [] { distributionPeriods1 },
                    CarryOverAmount = 10,
                    DeliveryProfilePeriods = new [] { deliveryProfilePeriod1, deliveryProfilePeriod2 },
                    ProfilePatternKey = profilePattern.ProfilePatternKey,
                    ProfilePatternDisplayName = profilePattern.ProfilePatternDisplayName
                });
        }

        private Task<ActionResult<ReProfileResponse>> WhenTheFundingLineIsReProfiled(ReProfileRequest request)
            => _service.ReProfile(request);

        private void GivenTheProfilePattern(ReProfileRequest request,
            FundingStreamPeriodProfilePattern profilePattern)
            => _profilePatterns.Setup(_ => _.GetProfilePattern(request.FundingStreamId,
                    request.FundingPeriodId,
                    request.FundingLineCode,
                    request.ProfilePatternKey))
                .ReturnsAsync(profilePattern);

        private void AndTheReProfilingStrategy(string key)
            => _strategies.Setup(_ => _.GetStrategy(key))
                .Returns(_reProfilingStrategy.Object);

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
                    profilePattern))
                .Returns(response);

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder profilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(profilePatternBuilder);

            return profilePatternBuilder.Build();
        }

        private ProfilePatternReProfilingConfiguration NewProfilePatternReProfilingConfiguration(Action<ProfilePatternReProfilingConfigurationBuilder> setUp = null)
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