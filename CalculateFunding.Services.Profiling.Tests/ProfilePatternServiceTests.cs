using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
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
using Newtonsoft.Json;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Profiling.Tests
{
    [TestClass]
    public class ProfilePatternServiceTests
    {
        private Mock<IProfilePatternRepository> _profilePatterns;
        private Mock<ICacheProvider> _caching;
        private Mock<IValidator<CreateProfilePatternRequest>> _createValidation;
        private Mock<IValidator<UpsertProfilePatternRequest>> _upsertValidation;

        private ProfilePatternService _service;

        [TestInitialize]
        public void SetUp()
        {
            _profilePatterns = new Mock<IProfilePatternRepository>();
            _caching = new Mock<ICacheProvider>();
            _createValidation = new Mock<IValidator<CreateProfilePatternRequest>>();
            _upsertValidation = new Mock<IValidator<UpsertProfilePatternRequest>>();

            _service = new ProfilePatternService(_profilePatterns.Object,
                _caching.Object, 
                _createValidation.Object,
                _upsertValidation.Object,
                new ProfilingResiliencePolicies
                {
                    Caching = Policy.NoOpAsync(),
                    ProfilePatternRepository = Policy.NoOpAsync()
                },
                Logger.None);
        }
        
        [TestMethod]
        public async Task GetPatternsFetchesFromCacheIfCached()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            
            FundingStreamPeriodProfilePattern[] expectedPatterns = new []
            {
                NewProfilePattern(),
                NewProfilePattern(),
                NewProfilePattern(),
                NewProfilePattern()
            };
            
            GivenTheCacheEntry(fundingStreamId, fundingPeriodId, expectedPatterns);

            IActionResult result = await WhenTheProfilePatternsAreQueried(fundingStreamId, fundingPeriodId);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)(result)).Value
                .Should()
                .BeEquivalentTo(expectedPatterns);
            
            AndCosmosWasNotQueriedByFundingStreamAndPeriod();
            AndNothingWasCached();
        }
        
        [TestMethod]
        public async Task GetPatternsQueriesCosmosAndCachesResultIfNotCached()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            
            FundingStreamPeriodProfilePattern[] expectedPatterns = new []
            {
                NewProfilePattern(),
                NewProfilePattern(),
                NewProfilePattern(),
                NewProfilePattern()
            };
            
            GivenTheProfilePatterns(fundingStreamId, fundingPeriodId, expectedPatterns);

            IActionResult result = await WhenTheProfilePatternsAreQueried(fundingStreamId, fundingPeriodId);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)(result)).Value
                .Should()
                .BeEquivalentTo(expectedPatterns);
            
            AndTheProfilePatternsWereCached(fundingStreamId, fundingPeriodId, expectedPatterns);
        }
        
        [TestMethod]
        public async Task GetPatternsReturns404IfNotCachedAndNotInCosmos()
        {
            IActionResult result = await WhenTheProfilePatternsAreQueried(NewRandomString(), NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetFetchesFromCacheIfCached()
        {
            string id = NewRandomString();
            FundingStreamPeriodProfilePattern expectedPattern = NewProfilePattern();
            
            GivenTheCacheEntry(id, expectedPattern);

            IActionResult result = await WhenTheProfilePatternIsQueried(id);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)(result)).Value
                .Should()
                .BeSameAs(expectedPattern);
            
            AndCosmosWasNotQueried();
            AndNothingWasCached();
        }
        
        [TestMethod]
        public async Task GetQueriesCosmosAndCachesResultIfNotCached()
        {
            FundingStreamPeriodProfilePattern expectedPattern = NewProfilePattern();
            
            GivenTheProfilePattern(expectedPattern);

            IActionResult result = await WhenTheProfilePatternIsQueried(expectedPattern.Id);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            ((OkObjectResult)(result)).Value
                .Should()
                .BeSameAs(expectedPattern);
            
            AndTheProfilePatternWasCached(expectedPattern);
        }
        
        [TestMethod]
        public async Task GetReturns404IfNotCachedAndNotInCosmos()
        {
            IActionResult result = await WhenTheProfilePatternIsQueried(NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task DeleteDelegatesToRepository()
        {
            string id = NewRandomString();
            HttpStatusCode expectedStatusCode = (HttpStatusCode)(int)new RandomNumberBetween(200, 299);

            GivenTheStatusCodeForTheDeleteRequest(id, expectedStatusCode);

            StatusCodeResult result = await WhenTheProfilePatternIsDeleted(id) as StatusCodeResult;

            result?
                .StatusCode
                .Should()
                .Be((int) expectedStatusCode);
            
            AndTheCacheWasInvalidated(id);
        }

        [TestMethod]
        public async Task CreateReturnsBadRequestIfRequestFailsValidation()
        {
            string invalidProperty = NewRandomString();
            string errorMessage = NewRandomString();

            CreateProfilePatternRequest request = NewCreateRequest(_ =>
                _.WithPattern(NewProfilePattern()));

            ValidationResult expectedValidationResult = NewValidationResult(_ =>
                _.WithFailures(NewValidationFailure(vf => vf.WithPropertyName(invalidProperty)
                    .WithErrorMessage(errorMessage))));

            GivenTheValidationResultForTheCreateRequest(request, expectedValidationResult);

            SerializableError serializableError = (await WhenTheProfilePatternIsCreated(request) as BadRequestObjectResult)?
                .Value as SerializableError;

            serializableError?[invalidProperty]
                .Should()
                .BeEquivalentTo(new[] {errorMessage});

            AndNoProfilePatternsWereSaved();
            AndTheCacheWasNotInvalidated();
        }

        [TestMethod]
        public async Task CreateDelegatesToRepositoryIfPassesValidationAndSavesOk()
        {
            CreateProfilePatternRequest request = NewCreateRequest(_ =>
                _.WithPattern(NewProfilePattern()));
            
            GivenTheValidationResultForTheCreateRequest(request, NewValidationResult());
            AndTheStatusCodeForSavingTheProfilePattern(request.Pattern, HttpStatusCode.OK);
            
            OkResult result = await WhenTheProfilePatternIsCreated(request) as OkResult;

            result
                .Should()
                .NotBeNull();
            
            AndTheCacheWasInvalidated(request.Pattern.Id);
        }

        [TestMethod]
        public void CreateThrowsInvalidOperationExceptionIfSaveFails()
        {
            CreateProfilePatternRequest request = NewCreateRequest(_ =>
                _.WithPattern(NewProfilePattern()));
            HttpStatusCode invalidStatusCode = HttpStatusCode.Conflict;
            
            GivenTheValidationResultForTheCreateRequest(request, NewValidationResult());
            AndTheStatusCodeForSavingTheProfilePattern(request.Pattern, invalidStatusCode);

            Func<Task<IActionResult>> invocation = () => WhenTheProfilePatternIsCreated(request);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Unable to save profile pattern. StatusCode {invalidStatusCode}");
            
            AndTheCacheWasNotInvalidated();
        }
        
        [TestMethod]
        public void UpsertThrowsInvalidOperationExceptionIfSaveFails()
        {
            UpsertProfilePatternRequest request = NewEditRequest(_ =>
                _.WithPattern(NewProfilePattern()));
            HttpStatusCode invalidStatusCode = HttpStatusCode.Conflict;

            GivenTheValidationResultForTheUpsertRequest(request, NewValidationResult());
            AndTheStatusCodeForSavingTheProfilePattern(request.Pattern, invalidStatusCode);

            Func<Task<IActionResult>> invocation = () => WhenTheProfilePatternIsEdited(request);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Unable to save profile pattern. StatusCode {invalidStatusCode}");
            
            AndTheCacheWasNotInvalidated();
        }

        private async Task<IActionResult> WhenTheProfilePatternIsEdited(UpsertProfilePatternRequest request)
        {
            return await _service.UpsertProfilePattern(request);
        }

        private async Task<IActionResult> WhenTheProfilePatternIsCreated(CreateProfilePatternRequest request)
        {
            return await _service.CreateProfilePattern(request);
        }

        private void AndTheCacheWasInvalidated(string id)
        {
            _caching.Verify(_ => _.RemoveAsync<FundingStreamPeriodProfilePattern>($"{ProfilingCacheKeys.ProfilePattern}{id}"),
                Times.Once);
        }

        private void AndTheCacheWasNotInvalidated()
        {
            _caching.Verify(_ => _.RemoveAsync<FundingStreamPeriodProfilePattern>(It.IsAny<string>()),
                Times.Never);     
        }
        
        private void AndTheStatusCodeForSavingTheProfilePattern(FundingStreamPeriodProfilePattern pattern, HttpStatusCode statusCode)
        {
            _profilePatterns.Setup(_ => _.SaveFundingStreamPeriodProfilePattern(pattern))
                .ReturnsAsync(statusCode);
        }

        private void AndNoProfilePatternsWereSaved()
        {
            _profilePatterns.Verify(_ => _.SaveFundingStreamPeriodProfilePattern(It.IsAny<FundingStreamPeriodProfilePattern>()),
                Times.Never);
        }

        private void AndCosmosWasNotQueried()
        {
            _profilePatterns.Verify(_ => _.GetProfilePattern(It.IsAny<string>()),
                Times.Never);
        }
        
        private void AndCosmosWasNotQueriedByFundingStreamAndPeriod()
        {
            _profilePatterns.Verify(_ => _.GetProfilePatternsForFundingStreamAndFundingPeriod(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        private void AndNothingWasCached()
        {
            _caching.Verify(_ => _.SetAsync(It.IsAny<string>(), 
                It.IsAny<FundingStreamPeriodProfilePattern>(), 
                It.IsAny<DateTimeOffset>(),
                It.IsAny<JsonSerializerSettings>()),
                Times.Never);
        }

        private void AndTheProfilePatternWasCached(FundingStreamPeriodProfilePattern pattern)
        {
            _caching.Verify(_ => _.SetAsync(PatternCacheKeyFor(pattern.Id), 
                    pattern, 
                    It.IsAny<DateTimeOffset>(), 
                    null),
                Times.Once);
        }
        
        private void AndTheProfilePatternsWereCached(string fundingStreamId,
            string fundingPeriodId,
            IEnumerable<FundingStreamPeriodProfilePattern> patterns)
        {
            _caching.Verify(_ => _.SetAsync(PatternsCacheKeyFor(fundingStreamId, fundingPeriodId), 
                    patterns, 
                    It.IsAny<DateTimeOffset>(), 
                    null),
                Times.Once);
        }

        private void GivenTheValidationResultForTheCreateRequest(ProfilePatternRequestBase request, ValidationResult validationResult)
        {
            _createValidation.Setup(_ => _.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
        }

        private void GivenTheValidationResultForTheUpsertRequest(ProfilePatternRequestBase request, ValidationResult validationResult)
        {
            _upsertValidation.Setup(_ => _.ValidateAsync(request, default))
                .ReturnsAsync(validationResult);
        }

        private void GivenTheCacheEntry(string id, FundingStreamPeriodProfilePattern pattern)
        {
            _caching.Setup(_ => _.GetAsync<FundingStreamPeriodProfilePattern>(PatternCacheKeyFor(id), null))
                .ReturnsAsync(pattern);
        }

        private static string PatternsCacheKeyFor(string fundingStreamId, string fundingPeriodId) 
            => $"{ProfilingCacheKeys.FundingStreamAndPeriod}{fundingStreamId}-{fundingPeriodId}";

        private static string PatternCacheKeyFor(string id) => $"{ProfilingCacheKeys.ProfilePattern}{id}";

        private void GivenTheCacheEntry(string fundingStreamId, string fundingPeriodId, IEnumerable<FundingStreamPeriodProfilePattern> patterns)
        {
            _caching.Setup(_ => _.GetAsync<FundingStreamPeriodProfilePattern[]>(PatternsCacheKeyFor(fundingStreamId, fundingPeriodId), null))
                .ReturnsAsync(patterns.ToArray());
        }
        
        private void GivenTheProfilePattern(FundingStreamPeriodProfilePattern pattern)
        {
            _profilePatterns.Setup(_ => _.GetProfilePattern(pattern.Id))
                .ReturnsAsync(pattern);
        }

        private void GivenTheProfilePatterns(string fundingStreamId, string fundingPeriodId, IEnumerable<FundingStreamPeriodProfilePattern> patterns)
        {
            _profilePatterns.Setup(_ => _.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(patterns);
        }

        private async Task<IActionResult> WhenTheProfilePatternIsQueried(string id)
        {
            return await _service.GetProfilePattern(id);
        }

        private async Task<IActionResult> WhenTheProfilePatternsAreQueried(string fundingStreamId, string fundingPeriodId)
        {
            return await _service.GetProfilePatterns(fundingStreamId, fundingPeriodId);
        }

        private async Task<IActionResult> WhenTheProfilePatternIsDeleted(string id)
        {
            return await _service.DeleteProfilePattern(id);
        }

        private void GivenTheStatusCodeForTheDeleteRequest(string id, HttpStatusCode statusCode)
        {
            _profilePatterns.Setup(_ => _.DeleteProfilePattern(id))
                .ReturnsAsync(statusCode);
        }

        private FundingStreamPeriodProfilePattern NewProfilePattern()
        {
            return new FundingStreamPeriodProfilePatternBuilder()
                .Build();
        }

        private UpsertProfilePatternRequest NewEditRequest(Action<UpsertProfilePatternRequestBuilder> setUp = null)
        {
            UpsertProfilePatternRequestBuilder builder = new UpsertProfilePatternRequestBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private CreateProfilePatternRequest NewCreateRequest(Action<CreateProfilePatternRequestBuilder> setUp = null)
        {
            CreateProfilePatternRequestBuilder builder = new CreateProfilePatternRequestBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder builder = new ValidationResultBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ValidationFailure NewValidationFailure(Action<ValidationFailureBuilder> setUp = null)
        {
            ValidationFailureBuilder builder = new ValidationFailureBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}