using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class FundingStreamProviderVersionServiceTests
    {
        private FundingStreamProviderVersionService _service;
        private Mock<IProviderVersionsMetadataRepository> _providerVersionsMetadata;
        private Mock<IProviderVersionService> _providerVersionsService;
        private Mock<IProviderVersionSearchService> _providerVersionsSearch;
        private Mock<IValidator<SetFundingStreamCurrentProviderVersionRequest>> _setRequestValidation;

        [TestInitialize]
        public void SetUp()
        {
            _providerVersionsMetadata = new Mock<IProviderVersionsMetadataRepository>();
            _providerVersionsService = new Mock<IProviderVersionService>();
            _providerVersionsSearch = new Mock<IProviderVersionSearchService>();
            _setRequestValidation = new Mock<IValidator<SetFundingStreamCurrentProviderVersionRequest>>();

            _service = new FundingStreamProviderVersionService(_providerVersionsMetadata.Object,
                _providerVersionsService.Object,
                _providerVersionsSearch.Object,
                _setRequestValidation.Object,
                new ProvidersResiliencePolicies
                {
                    BlobRepositoryPolicy = Policy.NoOpAsync(),
                    ProviderVersionsSearchRepository = Policy.NoOpAsync(),
                    ProviderVersionMetadataRepository = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        [DynamicData(nameof(InvalidIdExamples), DynamicDataSourceType.Method)]
        public void GetCurrentProvidersForFundingStream_GuardsAgainstMissingFundingStreamId(string invalidFundingStreamId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCurrentFundingStreamProvidersAreQueried(invalidFundingStreamId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [TestMethod]
        public async Task GetCurrentProvidersForFundingStream_GuardsAgainstNoMatchingCurrentProviderVersion()
        {
            IActionResult result = await WhenTheCurrentFundingStreamProvidersAreQueried(NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetCurrentProvidersForFundingStream_FetchesAllProvidersForTheCurrentProviderVersion()
        {
            string fundingStreamId = NewRandomString();
            string providerVersionId = NewRandomString();
            CurrentProviderVersion currentProviderVersion = NewCurrentProviderVersion(_ => _.WithProviderVersionId(providerVersionId));
            IActionResult expectedAllProvidersResults = NewActionResult();

            GivenTheCurrentProviderVersionForFundingStream(fundingStreamId, currentProviderVersion);
            AndTheProvidersResponseForTheProviderVersion(providerVersionId, expectedAllProvidersResults);

            IActionResult result = await WhenTheCurrentFundingStreamProvidersAreQueried(fundingStreamId);

            result
                .Should()
                .BeSameAs(expectedAllProvidersResults);
        }

        [TestMethod]
        public async Task SetCurrentProviderVersionForFundingStream_GuardsAgainstAnInvalidRequest()
        {
            string fundingStreamId = NewRandomString();
            string providerVersionId = NewRandomString();

            ValidationResult validationResult = NewValidationResult(_ => _.WithValidationFailures(
                NewValidationFailure()));

            GivenTheValidationResultForSetRequest(fundingStreamId, providerVersionId, validationResult);

            IActionResult badRequest = await WhenTheCurrentProviderVersionIsSetForFundingStream(fundingStreamId, providerVersionId);

            badRequest
                .Should()
                .BeOfType<BadRequestObjectResult>();

            AndNoCurrentProviderVersionWasUpserted();
        }

        [TestMethod]
        public async Task SetCurrentProviderVersionForFundingStream_UpsertsTheCurrentProviderVersion()
        {
            string fundingStreamId = NewRandomString();
            string providerVersionId = NewRandomString();

            ValidationResult validationResult = NewValidationResult();

            GivenTheValidationResultForSetRequest(fundingStreamId, providerVersionId, validationResult);

            IActionResult noContent = await WhenTheCurrentProviderVersionIsSetForFundingStream(fundingStreamId, providerVersionId);

            noContent
                .Should()
                .BeOfType<NoContentResult>();

            AndTheCurrentProviderVersionWasUpserted(fundingStreamId, providerVersionId);
        }

        [TestMethod]
        [DynamicData(nameof(InvalidIdExamples), DynamicDataSourceType.Method)]
        public void GetCurrentProviderForFundingStream_GuardsAgainstMissingFundingStreamId(string invalidProviderId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCurrentProviderForFundingStreamIsQueried(invalidProviderId,
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [TestMethod]
        [DynamicData(nameof(InvalidIdExamples), DynamicDataSourceType.Method)]
        public void GetCurrentProviderForFundingStream_GuardsAgainstMissingProviderId(string invalidProviderId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCurrentProviderForFundingStreamIsQueried(NewRandomString(),
                invalidProviderId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("providerId");
        }

        [TestMethod]
        public async Task GetCurrentProviderForFundingStream_GuardsAgainstNoMatchingCurrentProviderVersion()
        {
            IActionResult result = await WhenTheCurrentProviderForFundingStreamIsQueried(NewRandomString(),
                NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();       
        }
        
        
        [TestMethod]
        public async Task GetCurrentProviderForFundingStream_FetchesTheProviderInTheCurrentProviderVersion()
        {
            string fundingStreamId = NewRandomString();
            string providerId = NewRandomString();
            string providerVersionId = NewRandomString();
            CurrentProviderVersion currentProviderVersion = NewCurrentProviderVersion(_ => _.WithProviderVersionId(providerVersionId));
            IActionResult expectedProviderResult = NewActionResult();
            
            GivenTheCurrentProviderVersionForFundingStream(fundingStreamId, currentProviderVersion);
            AndTheProviderResponseForProviderVersionId(providerVersionId, providerId, expectedProviderResult);

            IActionResult result = await WhenTheCurrentProviderForFundingStreamIsQueried(fundingStreamId, providerId);

            result
                .Should()
                .BeSameAs(expectedProviderResult);
        }

        [TestMethod]
        [DynamicData(nameof(InvalidIdExamples), DynamicDataSourceType.Method)]
        public void SearchCurrentProviderVersionsForFundingStream_GuardsAgainstMissingFundingStreamId(string invalidFundingStreamId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCurrentProviderVersionForFundingStreamIsSearched(invalidFundingStreamId, NewSearchModel());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }
        
        [TestMethod]
        public void SearchCurrentProviderVersionsForFundingStream_GuardsAgainstMissingSearch()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCurrentProviderVersionForFundingStreamIsSearched(NewRandomString(), null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("search");
        }
        
        [TestMethod]
        public async Task SearchCurrentProviderVersionsForFundingStream_GuardsAgainstNoMatchingCurrentProviderVersion()
        {
           IActionResult notFound = await WhenTheCurrentProviderVersionForFundingStreamIsSearched(NewRandomString(), NewSearchModel());

           notFound
               .Should()
               .BeOfType<NotFoundResult>();
        }
        
        [TestMethod]
        public async Task SearchCurrentProviderVersionsForFundingStream_SearchesInTheCurrentProviderVersion()
        {
            string fundingStreamId = NewRandomString();
            string providerVersionId = NewRandomString();
            SearchModel search = new SearchModel();
            CurrentProviderVersion currentProviderVersion = NewCurrentProviderVersion(_ => _.WithProviderVersionId(providerVersionId));
            IActionResult expectedSearchResult = NewActionResult();
            
            GivenTheCurrentProviderVersionForFundingStream(fundingStreamId, currentProviderVersion);
            AndTheSearchResponseForTheProviderVersionId(providerVersionId, search, expectedSearchResult);

            IActionResult result = await WhenTheCurrentProviderVersionForFundingStreamIsSearched(fundingStreamId, search);

            result
                .Should()
                .BeSameAs(expectedSearchResult);
        }

        private async Task<IActionResult> WhenTheCurrentProviderVersionForFundingStreamIsSearched(string fundingStreamId,
            SearchModel search)
            => await _service.SearchCurrentProviderVersionsForFundingStream(fundingStreamId, search);
        
        private async Task<IActionResult> WhenTheCurrentProviderForFundingStreamIsQueried(string fundingStreamId,
            string providerId)
            => await _service.GetCurrentProviderForFundingStream(fundingStreamId, providerId);

        private async Task<IActionResult> WhenTheCurrentProviderVersionIsSetForFundingStream(string fundingStreamId,
            string providerVersionId)
            => await _service.SetCurrentProviderVersionForFundingStream(fundingStreamId, providerVersionId);

        private async Task<IActionResult> WhenTheCurrentFundingStreamProvidersAreQueried(string fundingStreamId)
            => await _service.GetCurrentProvidersForFundingStream(fundingStreamId);

        private void GivenTheValidationResultForSetRequest(string fundingStreamId,
            string providerVersionId,
            ValidationResult validationResult)
            => _setRequestValidation.Setup(_ => _.ValidateAsync(
                    It.Is<SetFundingStreamCurrentProviderVersionRequest>(req =>
                        req.FundingStreamId == fundingStreamId &&
                        req.ProviderVersionId == providerVersionId),
                    default))
                .ReturnsAsync(validationResult);

        private static IEnumerable<object[]> InvalidIdExamples()
        {
            yield return new object[]
            {
                null
            };
            yield return new object[]
            {
                ""
            };
            yield return new object[]
            {
                "   "
            };
        }

        private void AndTheSearchResponseForTheProviderVersionId(string providerVersionId,
            SearchModel search,
            IActionResult searchResponse)
        {
            _providerVersionsSearch.Setup(_ => _.SearchProviders(providerVersionId, search))
                .ReturnsAsync(searchResponse);
        }

        private void AndTheProviderResponseForProviderVersionId(string providerVersionId,
            string providerId,
            IActionResult providerResult)
        {
            _providerVersionsSearch.Setup(_ => _.GetProviderById(providerVersionId, providerId))
                .ReturnsAsync(providerResult);
        }

        private void AndTheCurrentProviderVersionWasUpserted(string fundingStreamId,
            string providerVersionId)
            => _providerVersionsMetadata.Verify(_ => _.UpsertCurrentProviderVersion(It.Is<CurrentProviderVersion>(cpv =>
                    cpv.Id == $"Current_{fundingStreamId}" &&
                    cpv.ProviderVersionId == providerVersionId)),
                Times.Once);

        private void AndNoCurrentProviderVersionWasUpserted()
            => _providerVersionsMetadata.Verify(_ => _.UpsertCurrentProviderVersion(It.IsAny<CurrentProviderVersion>()),
                Times.Never);

        private void GivenTheCurrentProviderVersionForFundingStream(string fundingStreamId,
            CurrentProviderVersion currentProviderVersion)
        {
            _providerVersionsMetadata.Setup(_ => _.GetCurrentProviderVersion(fundingStreamId))
                .ReturnsAsync(currentProviderVersion);
        }

        private void AndTheProvidersResponseForTheProviderVersion(string providerVersionId,
            IActionResult result)
        {
            _providerVersionsService.Setup(_ => _.GetAllProviders(providerVersionId))
                .ReturnsAsync(result);
        }

        private CurrentProviderVersion NewCurrentProviderVersion(Action<CurrentProviderVersionBuilder> setUp = null)
        {
            CurrentProviderVersionBuilder currentProviderVersionBuilder = new CurrentProviderVersionBuilder();

            setUp?.Invoke(currentProviderVersionBuilder);

            return currentProviderVersionBuilder.Build();
        }

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder validationResultBuilder = new ValidationResultBuilder();

            setUp?.Invoke(validationResultBuilder);

            return validationResultBuilder.Build();
        }

        private ValidationFailure NewValidationFailure() => new ValidationFailureBuilder()
            .Build();

        private SearchModel NewSearchModel() => new SearchModelBuilder()
            .Build();

        private IActionResult NewActionResult() => new Mock<IActionResult>().Object;

        private static string NewRandomString() => new RandomString();
    }
}