using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using CalculateFunding.Services.Profiling.Services;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Profiling.Tests
{
    [TestClass]
    public class CalculateProfileServiceBatchProcessTests
    {
        private Mock<IProfilePatternRepository> _profilePatterns;
        private Mock<IFundingValueProfiler> _fundingValueProfiler;
        private Mock<ICacheProvider> _cache;
        private Mock<IValidator<ProfileBatchRequest>> _batchRequestValidation;

        private CalculateProfileService _service;

        [TestInitialize]
        public void SetUp()
        {
            _profilePatterns = new Mock<IProfilePatternRepository>();
            _cache = new Mock<ICacheProvider>();
            _batchRequestValidation = new Mock<IValidator<ProfileBatchRequest>>();
            _fundingValueProfiler = new Mock<IFundingValueProfiler>();

            _service = new CalculateProfileService(_profilePatterns.Object,
                _cache.Object,
                _batchRequestValidation.Object,
                Logger.None,
                new ProfilingResiliencePolicies
                {
                    Caching = Policy.NoOpAsync(),
                    ProfilePatternRepository = Policy.NoOpAsync()
                },
                new ProducerConsumerFactory(),
                _fundingValueProfiler.Object);
        }

        [TestMethod]
        public void GuardsAgainstMissingRequest()
        {
            Func<IActionResult> invocation = () => WhenTheBatchProfileRequestIsProcessed(null)
                .GetAwaiter()
                .GetResult();

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("ProfileBatchRequest");
        }

        [TestMethod]
        public async Task RespondsWithBadRequestIfRequestFailsValidation()
        {
            string propertyOne = NewRandomString();
            string propertyTwo = NewRandomString();
            string errorOne = NewRandomString();
            string errorTwo = NewRandomString();

            ProfileBatchRequest profileBatchRequest = NewProfileBatchRequest();

            GivenTheValidationResult(profileBatchRequest,
                NewValidationResult(_ => _.WithFailures(
                    NewValidationFailure(vf => vf
                        .WithPropertyName(propertyOne)
                        .WithErrorMessage(errorOne)),
                    NewValidationFailure(vf => vf
                        .WithPropertyName(propertyTwo)
                        .WithErrorMessage(errorTwo)))));

            BadRequestObjectResult badRequestObjectResult = await WhenTheBatchProfileRequestIsProcessed(profileBatchRequest) as BadRequestObjectResult;

            badRequestObjectResult?.Value
                .Should()
                .BeEquivalentTo(NewSerializeableError(_ => _.WithError(propertyOne, errorOne)
                    .WithError(propertyTwo, errorTwo)));
        }

        [TestMethod]
        public async Task ProfilesEachProviderFundingValueInTheSuppliedBatches()
        {
            decimal fundingValueOne = NewRandomFundingValue();
            decimal fundingValueTwo = NewRandomFundingValue();
            decimal fundingValueThree = NewRandomFundingValue();

            ProfileBatchRequest request = NewProfileBatchRequest(_ => _.WithFundingValues(fundingValueOne,
                    fundingValueTwo,
                    fundingValueThree));

            FundingStreamPeriodProfilePattern profilePattern = NewProfilePattern();

            AllocationProfileResponse allocationProfileResponseOne = NewAllocationProfileResponse();
            AllocationProfileResponse allocationProfileResponseTwo = NewAllocationProfileResponse();
            AllocationProfileResponse allocationProfileResponseThree = NewAllocationProfileResponse();

            GivenTheValidationResult(request, NewValidationResult());
            AndTheProfilePattern(request, profilePattern);
            AndTheProfilingResponse(request, profilePattern, fundingValueOne, allocationProfileResponseOne);
            AndTheProfilingResponse(request, profilePattern, fundingValueTwo, allocationProfileResponseTwo);
            AndTheProfilingResponse(request, profilePattern, fundingValueThree, allocationProfileResponseThree);

            OkObjectResult result = await WhenTheBatchProfileRequestIsProcessed(request) as OkObjectResult;

            result?.Value
                .Should()
                .BeEquivalentTo(new[]
                {
                    new BatchAllocationProfileResponse(request.GetFundingValueKey(fundingValueOne),  fundingValueOne, allocationProfileResponseOne),
                    new BatchAllocationProfileResponse(request.GetFundingValueKey(fundingValueTwo), fundingValueTwo, allocationProfileResponseTwo),
                    new BatchAllocationProfileResponse(request.GetFundingValueKey(fundingValueThree), fundingValueThree, allocationProfileResponseThree)
                });
        }

        private async Task<IActionResult> WhenTheBatchProfileRequestIsProcessed(ProfileBatchRequest profileBatchRequest)
            => await _service.ProcessProfileAllocationBatchRequest(profileBatchRequest);

        private void GivenTheValidationResult(ProfileBatchRequest request,
            ValidationResult validationResult)
            => _batchRequestValidation.Setup(_ => _.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);

        private void AndTheProfilePattern(ProfileBatchRequest request,
            FundingStreamPeriodProfilePattern profilePattern)
            => _profilePatterns.Setup(_ => _.GetProfilePattern(request.FundingPeriodId,
                    request.FundingStreamId,
                    request.FundingLineCode,
                    request.ProviderType,
                    request.ProviderSubType))
                .ReturnsAsync(profilePattern);

        private void AndTheProfilingResponse(ProfileRequestBase request,
            FundingStreamPeriodProfilePattern profilePattern,
            decimal fundingValue,
            AllocationProfileResponse allocationProfileResponse)
            => _fundingValueProfiler.Setup(_ => _.ProfileAllocation(request, profilePattern, fundingValue))
                .Returns(allocationProfileResponse);

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

        private FundingStreamPeriodProfilePattern NewProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder profilePeriodPatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(profilePeriodPatternBuilder);

            return profilePeriodPatternBuilder.Build();
        }

        private ProfileBatchRequest NewProfileBatchRequest(Action<ProfileBatchRequestBuilder> setUp = null)
        {
            ProfileBatchRequestBuilder profileBatchRequestBuilder = new ProfileBatchRequestBuilder();

            setUp?.Invoke(profileBatchRequestBuilder);

            return profileBatchRequestBuilder.Build();
        }

        private AllocationProfileResponse NewAllocationProfileResponse(Action<AllocationProfileResponseBuilder> setUp = null)
        {
            AllocationProfileResponseBuilder allocationProfileResponseBuilder = new AllocationProfileResponseBuilder();

            setUp?.Invoke(allocationProfileResponseBuilder);

            return allocationProfileResponseBuilder.Build();
        }

        private SerializableError NewSerializeableError(Action<SerializeableErrorBuilder> setUp = null)
        {
            SerializeableErrorBuilder serializeableErrorBuilder = new SerializeableErrorBuilder();

            setUp?.Invoke(serializeableErrorBuilder);

            return serializeableErrorBuilder.Build();
        }

        private decimal NewRandomFundingValue() => new RandomNumberBetween(1, int.MaxValue);

        private string NewRandomString() => new RandomString();
    }
}