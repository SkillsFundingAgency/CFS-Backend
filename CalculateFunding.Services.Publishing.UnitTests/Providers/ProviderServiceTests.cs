using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using Provider = CalculateFunding.Models.Publishing.Provider;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class ProviderServiceTests
    {
        private IProvidersApiClient _providers;
        private IProviderService _providerService;
        private IPoliciesService _policiesService;
        private IPublishedFundingDataService _publishedFundingDataService;
        private ILogger _logger;
        private IMapper _mapper;
        private const string FundingStreamId = "PSG";
        private const string FundingPeriodId = "AY-2021";

        [TestInitialize]
        public void SetUp()
        {
            _providers = Substitute.For<IProvidersApiClient>();
            _policiesService = Substitute.For<IPoliciesService>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _logger = Substitute.For<ILogger>();
            _mapper = new MapperConfiguration(_ =>
            {
                _.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper();

            _providerService = new ProviderService(_providers,
                _policiesService,
                _publishedFundingDataService,
                new ResiliencePolicies
                {
                    ProvidersApiClient = Policy.NoOpAsync()
                },
                _mapper,
                _logger);
        }

        [DynamicData(nameof(EmptyIdExamples), DynamicDataSourceType.Method)]
        [TestMethod]
        public void QueryMethodThrowsExceptionIfSuppliedProviderVersionIdMissing(
            string providerVersionId)
        {
            Func<Task<IEnumerable<Provider>>> invocation =
                () => WhenTheProvidersAreQueriedByVersionId(providerVersionId);

            invocation
                .Should()
                .Throw<ArgumentNullException>();
        }

        [DynamicData(nameof(EmptyApiResponseExamples), DynamicDataSourceType.Method)]
        [TestMethod]
        public void QueryMethodThrowsExceptionIfProviderVersionResponseOrContentMissing(
            ApiResponse<ProviderVersion> providerVersionResponse)
        {
            string providerVersionId = NewRandomString();

            GivenTheApiResponse(() => _providers.GetProvidersByVersion(providerVersionId), providerVersionResponse);

            Func<Task<IEnumerable<Provider>>> invocation =
                () => WhenTheProvidersAreQueriedByVersionId(providerVersionId);

            invocation
                .Should()
                .Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task QueryMethodDelegatesToApiClientAndReturnsProvidersFromResponseContentProviderVersion()
        {
            ApiProvider firstExpectedProvider = NewApiProvider();
            ApiProvider secondExpectedProvider = NewApiProvider();
            ApiProvider thirdExpectedProvider = NewApiProvider();
            ApiProvider fourthExpectedProvider = NewApiProvider();
            ApiProvider fifthExpectedProvider = NewApiProvider();

            string providerVersionId = NewRandomString();

            GivenTheApiResponseProviderVersionContainsTheProviders(providerVersionId,
                firstExpectedProvider,
                secondExpectedProvider,
                thirdExpectedProvider,
                fourthExpectedProvider,
                fifthExpectedProvider);

            IEnumerable<Provider> providers = await WhenTheProvidersAreQueriedByVersionId(providerVersionId);
            IEnumerable<Provider> expectedProviders = _mapper.Map<IEnumerable<Provider>>(new ApiProvider[] { firstExpectedProvider,
                                                                                       secondExpectedProvider,
                                                                                       thirdExpectedProvider,
                                                                                       fourthExpectedProvider,
                                                                                       fifthExpectedProvider });

            providers
                .Should()
                .BeEquivalentTo(expectedProviders);
        }

        [TestMethod]
        public async Task WhenPublishedProvidersRequestedAndPublishedProvidersExistForSpecificationPublishedProvidersReturned()
        {
            Provider successor = NewProvider();
            IEnumerable<PublishedProvider> publishedProviders = new[] {
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider())))),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider())))),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithSuccessor(successor.ProviderId)))))) };

            IEnumerable<ApiProvider> apiproviders = _mapper.Map<IEnumerable<ApiProvider>>(publishedProviders.Select(_ => _.Current.Provider));

            ApiProvider excludedProvider = _mapper.Map<ApiProvider>(NewProvider());
            apiproviders = apiproviders.Concat(new[] { excludedProvider });

            publishedProviders = publishedProviders.Concat(new[] { NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(successor)))) });

            string providerVersionId = NewRandomString();
            string specificationId = NewRandomString();
            SpecificationSummary specification = new SpecificationSummary { Id = specificationId,
                FundingPeriod = new Reference { Id = FundingPeriodId },
                ProviderVersionId = providerVersionId
            };

            GivenTheApiResponseProviderVersionContainsTheProviders(providerVersionId, apiproviders.ToArray());

            AndPublishedProvidersForFundingStreamAndFundingPeriod(publishedProviders);

            AndTheApiResponseScopedProviderIdsContainsProviderIds(specificationId, apiproviders.Select(_ => _.ProviderId));

            AndTheFundingPeriodId();
            

            (IDictionary<string, PublishedProvider> PublishedProvidersForFundingStream,
             IDictionary<string, PublishedProvider> ScopedPublishedProviders) = await WhenPublishedProvidersAreReturned(specification);

            PublishedProvidersForFundingStream.Count()
                .Should()
                .Be(4);

            PublishedProvidersForFundingStream.Keys
                .Should()
                .BeEquivalentTo(publishedProviders.Select(_ => _.Current.ProviderId));

            ScopedPublishedProviders.Count()
                .Should()
                .Be(4);

            ScopedPublishedProviders.Keys
                .Should()
                .BeEquivalentTo(publishedProviders.Select(_ => _.Current.ProviderId));
        }

        [DataRow("", FundingPeriodId, FundingStreamId)]
        [DataRow("specId", "", FundingStreamId)]
        [DataRow("specId", FundingPeriodId, "")]
        [DataRow("specId", FundingPeriodId, FundingStreamId)]
        [TestMethod]
        public async Task WhenGeneratingMissingPublishedProvidersAnyMissingProvidersAreReturned(string specificationId, string fundingPeriodId, string fundingStreamId)
        {
            IEnumerable<PublishedProvider> publishedProviders = new[] {
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider())))),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider())))),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider())))) };

            IEnumerable<Provider> scopedProviders = publishedProviders.Select(_ => _.Current.Provider);


            Provider[] missingProviders = new[] { NewProvider() };
            scopedProviders = scopedProviders.Concat(missingProviders);

            string providerVersionId = NewRandomString();

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = specificationId,
                FundingPeriod = new Reference { Id = fundingPeriodId },
                ProviderVersionId = providerVersionId
            };

            if (string.IsNullOrWhiteSpace(specificationId) || string.IsNullOrWhiteSpace(fundingPeriodId) || string.IsNullOrWhiteSpace(fundingStreamId))
            {
                string message = $"Specified argument was out of the range of valid values. (Parameter 'specificationId')";

                if (string.IsNullOrWhiteSpace(fundingPeriodId))
                {
                    message = $"Specified argument was out of the range of valid values. (Parameter 'specificationFundingPeriodId')";
                }

                if (string.IsNullOrWhiteSpace(fundingStreamId))
                {
                    message = $"Specified argument was out of the range of valid values. (Parameter 'fundingStreamId')";
                }

                Func<Task> invocation = async() => await WhenGenerateMissingProviders(scopedProviders,
                specification,
                new Reference { Id = fundingStreamId },
                publishedProviders.ToDictionary(_ => _.Current.ProviderId));

                invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .WithMessage(message);

                return;
            }

            IDictionary<string, PublishedProvider> missingPublishedProviders = await WhenGenerateMissingProviders(scopedProviders,
                specification,
                new Reference { Id = fundingStreamId },
                publishedProviders.ToDictionary(_ => _.Current.ProviderId));

            missingPublishedProviders.Keys.Should().BeEquivalentTo(missingProviders.Select(_ => _.ProviderId));
        }

        [DynamicData(nameof(GenerateApiProviders), DynamicDataSourceType.Method)]
        [TestMethod]
        public async Task WhenGetScopedProviderIdsForSpecificationThenScopedProviderIdsAreReturned(ApiResponse<IEnumerable<string>> apiScopedProviders)
        {
            string specificationId = NewRandomString();

            IEnumerable<Provider> scopedProviders = apiScopedProviders?.Content?.Select(_ => NewProvider(p => p.WithProviderId(_)));

            GivenTheApiResponseScopedProviderIdsContainsProviderIds(specificationId, apiScopedProviders);

            if(apiScopedProviders == null)
            {
                Func<Task> invocation
                        = () => WhenScopedProvidersAreQueriedBySpeciciation(specificationId);

                invocation
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Scoped provider response was null");

                return;
            }

            if(apiScopedProviders.StatusCode != HttpStatusCode.OK)
            {
                Func<Task> invocation
                        = () => WhenScopedProvidersAreQueriedBySpeciciation(specificationId);

                invocation
                .Should()
                .Throw<RetriableException>()
                .WithMessage($"Scoped provider response was not OK, but instead '{apiScopedProviders.StatusCode}'");

                return;
            }

            if(apiScopedProviders?.Content == null)
            {
                Func<Task> invocation
                        = () => WhenScopedProvidersAreQueriedBySpeciciation(specificationId);

                invocation
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage("Scoped provider response content was null");

                return;
            }

            IEnumerable<string> scopedProviderIds = await WhenScopedProvidersAreQueriedBySpeciciation(specificationId);

            scopedProviderIds.Should().BeEquivalentTo(scopedProviders.Select(_ => _.ProviderId));
        }

        private void AndPublishedProvidersForFundingStreamAndFundingPeriod(IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedFundingDataService.GetCurrentPublishedProviders(FundingStreamId, FundingPeriodId)
                .Returns(publishedProviders);
        }

        private void AndTheFundingPeriodId()
        {
            _policiesService.GetFundingPeriodId(Arg.Any<string>())
                .Returns(FundingPeriodId);
        }

        private async Task<(IDictionary<string, PublishedProvider> PublishedProvidersForFundingStream, IDictionary<string, PublishedProvider> ScopedPublishedProviders)> WhenPublishedProvidersAreReturned(SpecificationSummary specification)
        {
            return await _providerService.GetPublishedProviders(new Reference { Id = FundingStreamId }, specification);
        }

        private async Task<IEnumerable<string>> WhenScopedProvidersAreQueriedBySpeciciation(string specification)
        {
            return await _providerService.GetScopedProviderIdsForSpecification(specification);
        }

        private async Task<IDictionary<string, PublishedProvider>> WhenGenerateMissingProviders(IEnumerable<Provider> scopedProviders,
            SpecificationSummary specification,
            Reference fundingStream,
            IDictionary<string, PublishedProvider> publishedProviders)
        {
            return await _providerService.GenerateMissingPublishedProviders(scopedProviders, specification, fundingStream, publishedProviders);
        }

        private async Task<IEnumerable<Provider>> WhenTheProvidersAreQueriedByVersionId(string providerVersionId)
        {
            return await _providerService.GetProvidersByProviderVersionsId(providerVersionId);
        }

        private void GivenTheApiResponse<T>(Func<Task<ApiResponse<T>>> action, ApiResponse<T> response)
        {
            action()
                .Returns(response);
        }

        private void GivenTheApiResponseProviderVersionContainsTheProviders(string providerVersionId,
            params ApiProvider[] providers)
        {
            GivenTheApiResponse(() => _providers.GetProvidersByVersion(providerVersionId), new ApiResponse<ProviderVersion>(HttpStatusCode.OK,
                new ProviderVersion
                {
                    Providers = providers
                }));
        }

        private void GivenTheApiResponseScopedProviderIdsContainsProviderIds(string specificationId,
            ApiResponse<IEnumerable<string>> providerIds)
        {
            GivenTheApiResponse(() => _providers.GetScopedProviderIds(specificationId), providerIds);
        }

        private void AndTheApiResponseScopedProviderIdsContainsProviderIds(string specificationId,
            IEnumerable<string> providerIds)
        {
            GivenTheApiResponse(() => _providers.GetScopedProviderIds(specificationId), new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK,
                providerIds));

        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static ApiProvider NewApiProvider()
        {
            return new ApiProvider();
        }

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private static IEnumerable<ApiResponse<IEnumerable<string>>[]> GenerateApiProviders()
        {
            yield return new ApiResponse<IEnumerable<string>>[] { null };
            yield return new[] { new ApiResponse<IEnumerable<string>>(HttpStatusCode.BadRequest) };
            yield return new[] { new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK) };
            yield return new[] { new ApiResponse<IEnumerable<string>> (HttpStatusCode.OK, new[] { Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString() }) };
        }

        public static IEnumerable<ApiResponse<ProviderVersion>[]> EmptyApiResponseExamples()
        {
            yield return new ApiResponse<ProviderVersion>[] {null};
            yield return new[] {new ApiResponse<ProviderVersion>(HttpStatusCode.OK)};
        }

        public static IEnumerable<object[]> EmptyIdExamples()
        {
            yield return new object[] {""};
            yield return new object[] {string.Empty};
        }
        
        private string NewRandomString() => new RandomString();
    }
}