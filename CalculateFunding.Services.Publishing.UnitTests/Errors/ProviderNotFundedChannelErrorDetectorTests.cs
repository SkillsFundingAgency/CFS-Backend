using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class ProviderNotFundedChannelErrorDetectorTests
    {
        private ProviderNotFundedChannelErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new ProviderNotFundedChannelErrorDetector();
        }
        
        [TestMethod]
        public void RunsForPreVariations()
        {
            _errorDetector.IsPostVariationCheck
                .Should()
                .BeFalse();

            _errorDetector.IsPreVariationCheck
                .Should()
                .BeTrue();

            _errorDetector.IsForAllFundingConfigurations
                .Should()
                .BeFalse();

            _errorDetector.IsAssignProfilePatternCheck
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task ReturnsErrorMessageWhenProviderNotFunded()
        {
            // Arrange
            string providerId = NewRandomString();
            string errorMessage = $"Provider {providerId} not configured to be a member of any group.";

            PublishedProvider publishedProvider = GivenPublishedProvider(providerId);

            IEnumerable<OrganisationGroupResult> organisationGroupChannelResults = AndOrganisationGroupResultsInformation(providerId);

            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupChannelResultsData
                = new Dictionary<string, IEnumerable<OrganisationGroupResult>> { { $"ChannelOne", organisationGroupChannelResults } };

            
            // Act
            await WhenDetectErrorsIsRunOnThePublishedProvider(publishedProvider, 
                                                              organisationGroupChannelResultsData);

            publishedProvider.Current
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            publishedProvider
                .Current
                .Errors
                .First()
                .DetailedErrorMessage
                .Should()
                .Be(errorMessage);

            publishedProvider
                .Current
                .Errors
                .First()
                .SummaryErrorMessage
                .Should()
                .Be(errorMessage);
        }

        [TestMethod]
        public async Task ReturnsNoErrorMessagesWhenProviderInChannelOrganisation()
        {
            // Arrange
            string providerId = NewRandomString();

            PublishedProvider publishedProvider = GivenPublishedProvider(providerId);

            IEnumerable<OrganisationGroupResult> organisationGroupChannelResults = AndOrganisationGroupResultsIncludingProvider(providerId);

            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupChannelResultsData
                = new Dictionary<string, IEnumerable<OrganisationGroupResult>> { { $"ChannelOne", organisationGroupChannelResults } };


            // Act
            await WhenDetectErrorsIsRunOnThePublishedProvider(publishedProvider,
                                                              organisationGroupChannelResultsData);

            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        private async Task WhenDetectErrorsIsRunOnThePublishedProvider(PublishedProvider publishedProvider,
            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupChannelResults)
        {
            PublishedProvidersContext publishedProvidersContext = new PublishedProvidersContext
            {
                ChannelOrganisationGroupResultsData = organisationGroupChannelResults
            };

            await _errorDetector.DetectErrors(publishedProvider, publishedProvidersContext);
        }

        private PublishedProvider GivenPublishedProvider(string providerId)
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            int majorVersion1 = NewRandomInt();
            int minorVersion1 = NewRandomInt();
            int majorVersion2 = NewRandomInt();
            int minorVersion2 = NewRandomInt();
            string fundingConfigurationId = NewRandomString();
            
            return NewPublishedProvider(_ => _
                .WithCurrent(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithProviderId(providerId)))
                .WithReleased(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithProviderId(providerId)
                    .WithMajorVersion(majorVersion2)
                    .WithMinorVersion(minorVersion2))));
        }

        private IEnumerable<OrganisationGroupResult> AndOrganisationGroupResultsInformation(string providerId)
        {
            OrganisationGroupTypeIdentifier groupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN;

            return new[]
            {
                NewOrganisationGroupResult(_ => _
                    .WithIdentifiers(new[]
                    {
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString())),
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
                    })),
                NewOrganisationGroupResult(_ => _
                    .WithIdentifiers(new[]
                    {
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString())),
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
                    })
                    .WithGroupReason(OrganisationGroupingReason.Information)
                    .WithProviders(new [] 
                    { 
                        new Common.ApiClient.Providers.Models.Provider { ProviderId = providerId } 
                    }))
            };
        }

        private IEnumerable<OrganisationGroupResult> AndOrganisationGroupResultsIncludingProvider(string providerId)
        {
            OrganisationGroupTypeIdentifier groupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN;

            return new[]
            {
                NewOrganisationGroupResult(_ => _
                    .WithIdentifiers(new[]
                    {
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString())),
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
                    })
                    .WithProviders(new [] { new Common.ApiClient.Providers.Models.Provider { ProviderId = providerId } }))
            };
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }

        private static OrganisationIdentifier NewOrganisationIdentifier(Action<OrganisationIdentifierBuilder> setUp = null)
        {
            OrganisationIdentifierBuilder organisationIdentifierBuilder = new OrganisationIdentifierBuilder();

            setUp?.Invoke(organisationIdentifierBuilder);

            return organisationIdentifierBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();
        private static int NewRandomInt() => new RandomNumberBetween(1, 10);
    }
}