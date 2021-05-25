using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.UnitTests.ApiClientHelpers.Policies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using ApiProviderVersion = CalculateFunding.Common.ApiClient.Providers.Models.ProviderVersion;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using System.Linq;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    [TestClass]
    public class ConverterEligibleProviderServiceTests
    {
        private Mock<IProvidersApiClient> _providers;

        private ConverterEligibleProviderService _eligibleProviders;

        [TestInitialize]
        public void SetUp()
        {
            _providers = new Mock<IProvidersApiClient>();

            _eligibleProviders = new ConverterEligibleProviderService(_providers.Object,
                new DatasetsResiliencePolicies
                {
                    ProvidersApiClient = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void GuardsAgainstNotProviderVersionIdBeingSupplied()
        {
            Func<Task<IEnumerable<ProviderConverterDetail>>> invocation = 
                () => WhenTheEligibleConvertersAreQueried(null, NewFundingConfiguration());

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be("Must supply a provider version id to query eligible providers");
        }

        [TestMethod]
        public void GuardsAgainstNoProviderVersionLocatedWithSuppliedId()
        {
            string providerVersionId = NewRandomString();

            Func<Task<IEnumerable<ProviderConverterDetail>>> invocation = 
                () => WhenTheEligibleConvertersAreQueried(providerVersionId, NewFundingConfiguration());

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be($"Unable to get provider ids for converters. Could not locate provider version {providerVersionId}");   
        }

        [TestMethod]
        public void GuardsAgainstNotFundingConfigurationBeingSupplied()
        {
            Func<Task<IEnumerable<ProviderConverterDetail>>> invocation = 
                () => WhenTheEligibleConvertersAreQueried(NewRandomString(), null);

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be("Must supply a funding configuration to query eligible providers");   
        }

        [TestMethod]
        public void GuardsAgainstNotIndicativeStatusListOnSuppliedFundingConfiguration()
        {
            string providerVersionId = NewRandomString();

            Func<Task<IEnumerable<ProviderConverterDetail>>> invocation = 
                () => WhenTheEligibleConvertersAreQueried(providerVersionId, NewFundingConfiguration());
            
            GivenTheProviderVersion(providerVersionId, NewApiProviderVersion());

            invocation
                .Should()
                .ThrowAsync<NonRetriableException>()
                .Result
                .Which
                .Message
                .Should()
                .Be("The funding configuration needs a list of indicative status labels to query eligible providers");       
        }

        [TestMethod]
        [DynamicData(nameof(EligibleProviderQueryExamples), DynamicDataSourceType.Method)]
        public async Task ProjectsProvidersWithIndicativeStatusAndASinglePredecessorInNoOtherProvider(
            ApiProviderVersion providerVersion,
            FundingConfiguration fundingConfiguration,
            IEnumerable<ProviderConverter> expectedEligibleConverters)
        {
            string providerVersionId = NewRandomString();
            
            GivenTheProviderVersion(providerVersionId, providerVersion);

            IEnumerable<ProviderConverter> eligibleConverters = await WhenTheEligibleConvertersAreQueried(providerVersionId, fundingConfiguration);

            eligibleConverters
                .Should()
                .BeEquivalentTo(expectedEligibleConverters);
        }

        private async Task<IEnumerable<ProviderConverterDetail>> WhenTheEligibleConvertersAreQueried(string providerVersionId,
            FundingConfiguration fundingConfiguration)
            => await _eligibleProviders.GetEligibleConvertersForProviderVersion(providerVersionId, fundingConfiguration);

        private void GivenTheProviderVersion(string providerVersionId,
            ApiProviderVersion providerVersion)
            => _providers.Setup(_ => _.GetProvidersByVersion(providerVersionId))
                .ReturnsAsync(new ApiResponse<ApiProviderVersion>(HttpStatusCode.OK, providerVersion));

        private static IEnumerable<object[]> EligibleProviderQueryExamples()
        {
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();
            string providerIdThree = NewRandomString();
            string providerIdFour = NewRandomString();

            string indicativeStatusOne = NewRandomString();
            string indicativeStatusTwo = NewRandomString();
            
            yield return new object []
            {
                NewApiProviderVersion(_ => _.WithProviders(NewApiProvider(),
                    NewApiProvider(prov => prov.WithProviderId(providerIdOne)
                        .WithStatus(indicativeStatusOne)
                        .WithPredecessors(providerIdTwo)),
                    NewApiProvider(prov => prov.WithPredecessors(providerIdTwo)),
                    NewApiProvider(prov => prov.WithStatus(indicativeStatusOne)
                        .WithPredecessors(providerIdTwo, NewRandomString())))),
                NewFundingConfiguration(_ => _.WithIndicativeOpenerProviderStatus(indicativeStatusOne)),
                EligibleConverters()
            };
            yield return new object []
            {
                NewApiProviderVersion(_ => _.WithProviders(NewApiProvider(),
                    NewApiProvider(prov => prov.WithProviderId(providerIdOne)
                        .WithStatus(indicativeStatusTwo)
                        .WithPredecessors(providerIdTwo)),
                    NewApiProvider(prov => prov.WithPredecessors(NewRandomString())))),
                NewFundingConfiguration(_ => _.WithIndicativeOpenerProviderStatus(indicativeStatusTwo)),
                EligibleConverters(NewEligibleConverter(_ => _.WithTargetProviderId(providerIdOne)
                    .WithPreviousProviderIdentifier(providerIdTwo)))
            };
            yield return new object []
            {
                NewApiProviderVersion(_ => _.WithProviders(NewApiProvider(),
                    NewApiProvider(prov => prov.WithProviderId(providerIdOne)
                        .WithStatus(indicativeStatusOne)
                        .WithPredecessors(providerIdTwo)),
                    NewApiProvider(prov => prov.WithProviderId(providerIdThree)
                        .WithStatus(indicativeStatusTwo)
                        .WithPredecessors(providerIdFour)),
                    NewApiProvider(prov => prov.WithPredecessors(NewRandomString())))),
                NewFundingConfiguration(_ => _.WithIndicativeOpenerProviderStatus(indicativeStatusOne, indicativeStatusTwo)),
                EligibleConverters(NewEligibleConverter(_ => _.WithTargetProviderId(providerIdOne)
                    .WithPreviousProviderIdentifier(providerIdTwo)),
                    NewEligibleConverter(_ => _.WithTargetProviderId(providerIdThree)
                        .WithPreviousProviderIdentifier(providerIdFour)))
            };
        }

        private static ProviderConverter[] EligibleConverters(params ProviderConverter[] eligibleConverters) => eligibleConverters;

        private static ApiProviderVersion NewApiProviderVersion(Action<ApiProviderVersionBuilder> setUp = null)
        {
            ApiProviderVersionBuilder apiProviderVersionBuilder = new ApiProviderVersionBuilder();

            setUp?.Invoke(apiProviderVersionBuilder);
            
            return apiProviderVersionBuilder.Build();
        }

        private static ApiProvider NewApiProvider(Action<ApiProviderBuilder> setUp = null)
        {
            ApiProviderBuilder apiProviderBuilder = new ApiProviderBuilder();

            setUp?.Invoke(apiProviderBuilder);
            
            return apiProviderBuilder.Build();
        }
        
        private static FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder apiFundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(apiFundingConfigurationBuilder);
            
            return apiFundingConfigurationBuilder.Build();
        }

        private static ProviderConverter NewEligibleConverter(Action<ProviderConverterBuilder> setUp = null)
        {
            ProviderConverterBuilder eligibleConverterBuilder = new ProviderConverterBuilder();

            setUp?.Invoke(eligibleConverterBuilder);
            
            return eligibleConverterBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();
    }
}