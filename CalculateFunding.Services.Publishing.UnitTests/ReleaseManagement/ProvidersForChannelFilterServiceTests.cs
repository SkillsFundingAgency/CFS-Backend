using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ProvidersForChannelFilterServiceTests
    {

        private ProvidersForChannelFilterService _service;

        [TestInitialize]
        public void Initialise()
        {

            _service = new ProvidersForChannelFilterService();
        }

        [TestMethod]
        public void UpsertThrowsInvalidOperationExceptionIfChannelIsNull()
        {
            Func<IEnumerable<PublishedProviderVersion>> invocation = () => WhenProvidersForChannelFiltered(null, null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .Message
                .Should()
                .Be($"Value cannot be null. (Parameter 'channel')");
        }

        [TestMethod]
        public void UpsertThrowsInvalidOperationExceptionIfPublishedProvidersAreNull()
        {
            Func<IEnumerable<PublishedProviderVersion>> invocation = () => WhenProvidersForChannelFiltered(new Channel(), null, null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .Message
                .Should()
                .Be($"Value cannot be null. (Parameter 'publishedProviders')");
        }

        [TestMethod]
        public void UpsertThrowsInvalidOperationExceptionIfFundingConfigurationIsNull()
        {
            Func<IEnumerable<PublishedProviderVersion>> invocation = () => WhenProvidersForChannelFiltered(new Channel(), Array.Empty<PublishedProviderVersion>(), null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .Message
                .Should()
                .Be($"Value cannot be null. (Parameter 'fundingConfiguration')");
        }

        [TestMethod]
        public void FiltersProvidersBasedOnGivenChannelConfiguration()
        {
            string channelCode = NewRandomString();
            string providerStatus = NewRandomString();
            string providerType = NewRandomString();
            string providerSubType = NewRandomString();

            string providerIdOne = NewRandomString();

            Channel channel = new Channel
            {
                ChannelCode = channelCode
            };

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithReleaseChannels(
                    NewFundingConfigurationChannel(c => c
                        .WithChannelCode(channelCode)
                        .WithProviderStatus(providerStatus)
                        .WithProviderTypeMatch(
                            NewProviderTypeMatch(m => m
                                .WithProviderType(providerType)
                                .WithProviderSubtype(providerSubType))))));

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = new List<PublishedProviderVersion>
            {
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(providerStatus).WithProviderType(providerType).WithProviderSubType(providerSubType).WithProviderId(providerIdOne)))),
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(null).WithProviderType(providerType).WithProviderSubType(providerSubType)))),
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(providerStatus).WithProviderType(null).WithProviderSubType(providerSubType)))),
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(providerStatus).WithProviderType(providerType).WithProviderSubType(null)))),
            };

            IEnumerable<PublishedProviderVersion> actualPublishedProviderVersions 
                = WhenProvidersForChannelFiltered(channel, publishedProviderVersions, fundingConfiguration);

            actualPublishedProviderVersions.Count().Should().Be(1);
            actualPublishedProviderVersions.FirstOrDefault().ProviderId.Should().Be(providerIdOne);
        }

        [TestMethod]
        public void DoesNotFiltersProvidersBasedOnGivenChannelConfigurationWithNoMatchingChannelCode()
        {
            string channelCodeOne = NewRandomString();
            string channelCodeTwo = NewRandomString();

            string providerStatus = NewRandomString();
            string providerType = NewRandomString();
            string providerSubType = NewRandomString();

            string providerIdOne = NewRandomString();

            Channel channel = new Channel
            {
                ChannelCode = channelCodeOne
            };

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _
                .WithReleaseChannels(
                    NewFundingConfigurationChannel(c => c
                        .WithChannelCode(channelCodeTwo)
                        .WithProviderStatus(providerStatus)
                        .WithProviderTypeMatch(
                            NewProviderTypeMatch(m => m
                                .WithProviderType(providerType)
                                .WithProviderSubtype(providerSubType))))));

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = new List<PublishedProviderVersion>
            {
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(providerStatus).WithProviderType(providerType).WithProviderSubType(providerSubType).WithProviderId(providerIdOne)))),
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(null).WithProviderType(providerType).WithProviderSubType(providerSubType)))),
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(providerStatus).WithProviderType(null).WithProviderSubType(providerSubType)))),
                NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider(p => p.WithStatus(providerStatus).WithProviderType(providerType).WithProviderSubType(null)))),
            };

            IEnumerable<PublishedProviderVersion> actualPublishedProviderVersions
                = WhenProvidersForChannelFiltered(channel, publishedProviderVersions, fundingConfiguration);

            actualPublishedProviderVersions.Count().Should().Be(4);
        }

        private IEnumerable<PublishedProviderVersion> WhenProvidersForChannelFiltered(
            Channel channel, 
            IEnumerable<PublishedProviderVersion> publishedProviders, 
            FundingConfiguration fundingConfiguration)
                => _service.FilterProvidersForChannel(channel, publishedProviders, fundingConfiguration);

        private string NewRandomString() => new RandomString();

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder builder = new FundingConfigurationBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private FundingConfigurationChannel NewFundingConfigurationChannel(Action<FundingConfigurationChannelBuilder> setUp = null)
        {
            FundingConfigurationChannelBuilder builder = new FundingConfigurationChannelBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ProviderTypeMatch NewProviderTypeMatch(Action<ProviderTypeMatchBuilder> setUp = null)
        {
            ProviderTypeMatchBuilder builder = new ProviderTypeMatchBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder builder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder builder = new ProviderBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}
