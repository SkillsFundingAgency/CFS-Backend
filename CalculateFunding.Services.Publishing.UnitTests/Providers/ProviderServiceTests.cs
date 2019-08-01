using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Providers;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Providers
{
    [TestClass]
    public class ProviderServiceTests
    {
        private IProvidersApiClient _providers;
        private IProviderService _providerService;

        [TestInitialize]
        public void SetUp()
        {
            _providers = Substitute.For<IProvidersApiClient>();

            _providerService = new ProviderService(_providers,
                new ResiliencePolicies
                {
                    ProvidersApiClient = Policy.NoOpAsync()
                });
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

            GivenTheApiResponse(providerVersionId, providerVersionResponse);

            Func<Task<IEnumerable<Provider>>> invocation =
                () => WhenTheProvidersAreQueriedByVersionId(providerVersionId);

            invocation
                .Should()
                .Throw<ArgumentNullException>();
        }

        [TestMethod]
        public async Task QueryMethodDelegatesToApiClientAndReturnsProvidersFromResponseContentProviderVersion()
        {
            Provider firstExpectedProvider = NewProvider();
            Provider secondExpectedProvider = NewProvider();
            Provider thirdExpectedProvider = NewProvider();
            Provider fourthExpectedProvider = NewProvider();
            Provider fifthExpectedProvider = NewProvider();

            string providerVersionId = NewRandomString();

            GivenTheApiResponseProviderVersionContainsTheProviders(providerVersionId,
                firstExpectedProvider,
                secondExpectedProvider,
                thirdExpectedProvider,
                fourthExpectedProvider,
                fifthExpectedProvider);

            IEnumerable<Provider> providers = await WhenTheProvidersAreQueriedByVersionId(providerVersionId);

            providers
                .Should()
                .BeEquivalentTo(firstExpectedProvider,
                    secondExpectedProvider,
                    thirdExpectedProvider,
                    fourthExpectedProvider,
                    fifthExpectedProvider);
        }

        private async Task<IEnumerable<Provider>> WhenTheProvidersAreQueriedByVersionId(string providerVersionId)
        {
            return await _providerService.GetProvidersByProviderVersionsId(providerVersionId);
        }

        private void GivenTheApiResponse(string providerVersionId, ApiResponse<ProviderVersion> response)
        {
            _providers.GetProvidersByVersion(providerVersionId)
                .Returns(response);
        }

        private void GivenTheApiResponseProviderVersionContainsTheProviders(string providerVersionId,
            params Provider[] providers)
        {
            GivenTheApiResponse(providerVersionId, new ApiResponse<ProviderVersion>(HttpStatusCode.OK,
                new ProviderVersion
                {
                    Providers = providers
                }));
        }

        private Provider NewProvider()
        {
            return new Provider();
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