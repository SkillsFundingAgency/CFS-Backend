using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.Requests;
using CalculateFunding.Services.Providers.Interfaces;
using CalculateFunding.Services.Providers.MappingProfiles;
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
        private IMapper _mapper;

        [TestInitialize]
        public void SetUp()
        {
            _providerVersionsMetadata = new Mock<IProviderVersionsMetadataRepository>();
            _providerVersionsService = new Mock<IProviderVersionService>();
            _providerVersionsSearch = new Mock<IProviderVersionSearchService>();
            _setRequestValidation = new Mock<IValidator<SetFundingStreamCurrentProviderVersionRequest>>();
            MapperConfiguration mappingConfig = new MapperConfiguration(c => { c.AddProfile<ProviderVersionsMappingProfile>(); });
            _mapper = mappingConfig.CreateMapper();

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
                Logger.None,
                _mapper);
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
            int? providerSnapshotId = NewRandomNumber();

            ValidationResult validationResult = NewValidationResult();

            GivenTheValidationResultForSetRequest(fundingStreamId, providerVersionId, validationResult);

            IActionResult noContent = await WhenTheCurrentProviderVersionIsSetForFundingStream(fundingStreamId, providerVersionId, providerSnapshotId);

            noContent
                .Should()
                .BeOfType<NoContentResult>();

            AndTheCurrentProviderVersionWasUpserted(fundingStreamId, providerVersionId, providerSnapshotId);
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

        [TestMethod]
        public async Task GetCurrentProviderMetadataForFundingStream_GuardsAgainstNoMatchingCurrentProviderVersion()
        {
            IActionResult result = await WhenGetCurrentProviderMetadataForFundingStream(NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }


        [TestMethod]
        public async Task GetCurrentProviderMetadataForFundingStream_FetchesTheMetadataForCurrentProviderVersion()
        {
            string fundingStreamId = NewRandomString();
            string providerVersionId = NewRandomString();
            int? providerSnapshotId = NewRandomNumber();
            CurrentProviderVersion currentProviderVersion = NewCurrentProviderVersion(_ => _.ForFundingStreamId(fundingStreamId)
                                                                                            .WithProviderVersionId(providerVersionId)
                                                                                            .WithProviderSnapshotId(providerSnapshotId));
            GivenTheCurrentProviderVersionForFundingStream(fundingStreamId, currentProviderVersion);

            IActionResult result = await WhenGetCurrentProviderMetadataForFundingStream(fundingStreamId);

            CurrentProviderVersionMetadata actualMetadata = result
                                                                .Should()
                                                                .BeOfType<OkObjectResult>()
                                                                .Which
                                                                .Value.As<CurrentProviderVersionMetadata>();
            actualMetadata.FundingStreamId.Should().Be(fundingStreamId);
            actualMetadata.ProviderVersionId.Should().Be(providerVersionId);
            actualMetadata.ProviderSnapshotId.Should().Be(providerSnapshotId);
        }

        [TestMethod]
        public async Task GetCurrentProviderMetadataForAllFundingStreams_GuardsAgainstNoCurrentProviderVersions()
        {
            IActionResult result = await WhenGetCurrentProviderMetadataForFundingStream(NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetCurrentProviderMetadataForAllFundingStreams_FetchesTheMetadataForAllCurrentProviderVersions()
        {
            string fundingStreamId1 = NewRandomString();
            string providerVersionId1 = NewRandomString();
            int? providerSnapshotId1 = NewRandomNumber();
            CurrentProviderVersion currentProviderVersion1 = NewCurrentProviderVersion(_ => _.ForFundingStreamId(fundingStreamId1)
                                                                                            .WithProviderVersionId(providerVersionId1)
                                                                                            .WithProviderSnapshotId(providerSnapshotId1));
            string fundingStreamId2 = NewRandomString();
            string providerVersionId2 = NewRandomString();
            int? providerSnapshotId2 = NewRandomNumber();
            CurrentProviderVersion currentProviderVersion2 = NewCurrentProviderVersion(_ => _.ForFundingStreamId(fundingStreamId2)
                                                                                            .WithProviderVersionId(providerVersionId2)
                                                                                            .WithProviderSnapshotId(providerSnapshotId2));

            CurrentProviderVersionMetadata expectedMetadata1 = _mapper.Map<CurrentProviderVersionMetadata>(currentProviderVersion1);
            CurrentProviderVersionMetadata expectedMetadata2 = _mapper.Map<CurrentProviderVersionMetadata>(currentProviderVersion2);


            GivenAllCurrentProviderVersions(currentProviderVersion1, currentProviderVersion2);

            IActionResult result = await WhenGetCurrentProviderMetadataForAllFundingStreams();

            IEnumerable<CurrentProviderVersionMetadata> actualMetadata = result
                                                                .Should()
                                                                .BeOfType<OkObjectResult>()
                                                                .Which
                                                                .Value.As<IEnumerable<CurrentProviderVersionMetadata>>();
            actualMetadata.Count().Should().Be(2);
            actualMetadata.Single(x => x.FundingStreamId == fundingStreamId1)
                .Should().BeEquivalentTo(expectedMetadata1);
            actualMetadata.Single(x => x.FundingStreamId == fundingStreamId2)
                .Should().BeEquivalentTo(expectedMetadata2);
        }

        private async Task<IActionResult> WhenGetCurrentProviderMetadataForAllFundingStreams()
           => await _service.GetCurrentProviderMetadataForAllFundingStreams();

        private async Task<IActionResult> WhenGetCurrentProviderMetadataForFundingStream(string fundingStreamId)
           => await _service.GetCurrentProviderMetadataForFundingStream(fundingStreamId);

        private async Task<IActionResult> WhenTheCurrentProviderVersionForFundingStreamIsSearched(string fundingStreamId,
            SearchModel search)
            => await _service.SearchCurrentProviderVersionsForFundingStream(fundingStreamId, search);
        
        private async Task<IActionResult> WhenTheCurrentProviderForFundingStreamIsQueried(string fundingStreamId,
            string providerId)
            => await _service.GetCurrentProviderForFundingStream(fundingStreamId, providerId);

        private async Task<IActionResult> WhenTheCurrentProviderVersionIsSetForFundingStream(string fundingStreamId,
            string providerVersionId, int? providerSnapshotId = null)
            => await _service.SetCurrentProviderVersionForFundingStream(fundingStreamId, providerVersionId, providerSnapshotId);

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
            string providerVersionId, int? providerSnapshotId)
            => _providerVersionsMetadata.Verify(_ => _.UpsertCurrentProviderVersion(It.Is<CurrentProviderVersion>(cpv =>
                    cpv.Id == $"Current_{fundingStreamId}" &&
                    cpv.ProviderVersionId == providerVersionId &&
                    cpv.ProviderSnapshotId == providerSnapshotId)),
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

        private void GivenAllCurrentProviderVersions(params CurrentProviderVersion[] currentProviderVersions)
        {
            _providerVersionsMetadata.Setup(_ => _.GetAllCurrentProviderVersions())
                .ReturnsAsync(currentProviderVersions);
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

        private static int NewRandomNumber() => new RandomNumberBetween(0, int.MaxValue);
    }
}