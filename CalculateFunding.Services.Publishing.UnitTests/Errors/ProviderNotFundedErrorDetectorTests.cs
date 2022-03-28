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
    public class ProviderNotFundedErrorDetectorTests
    {
        private ProviderNotFundedErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new ProviderNotFundedErrorDetector();
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
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string providerId1 = NewRandomString();
            int majorVersion1 = NewRandomInt();
            int minorVersion1 = NewRandomInt();
            string providerId2 = NewRandomString();
            int majorVersion2 = NewRandomInt();
            int minorVersion2 = NewRandomInt();
            string identifierValue1 = NewRandomString();
            string identifierValue2 = NewRandomString();
            string identifierValue3 = NewRandomString();
            string fundingConfigurationId = NewRandomString();
            OrganisationGroupTypeIdentifier groupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN;


            string errorMessage = $"Provider {providerId2} not configured to be a member of any group.";

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _
                .WithCurrent(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithProviderId(providerId2)))
                .WithReleased(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithProviderId(providerId2)
                    .WithMajorVersion(majorVersion2)
                    .WithMinorVersion(minorVersion2))));

            IEnumerable<OrganisationGroupResult> organisationGroupResults = new[]
            {
                NewOrganisationGroupResult(_ => _
                    .WithIdentifiers(new[]
                    {
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue1)),
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
                    })),
                NewOrganisationGroupResult(_ => _
                    .WithIdentifiers(new[]
                    {
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue3)),
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
                    })
                    .WithGroupReason(OrganisationGroupingReason.Information)
                    .WithProviders(new[] {
                        new Common.ApiClient.Providers.Models.Provider{ ProviderId = publishedProvider.Current.ProviderId }
                    }))
            };

            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData
                = new Dictionary<string, IEnumerable<OrganisationGroupResult>> { { $"{fundingStreamId}:{fundingPeriodId}", organisationGroupResults } };

            IEnumerable<PublishedFunding> publishedFundings = new[]
            {
                NewPublishedFunding(_ => _
                    .WithCurrent(NewPublishedFundingVersion(fv => fv.WithProviderFundings(new[]
                        {
                            $"{fundingStreamId}-{fundingPeriodId}-{providerId1}-{majorVersion1}_{minorVersion1}",
                            $"{fundingStreamId}-{fundingPeriodId}-{providerId2}-{majorVersion2}_{minorVersion2}"
                        })
                        .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                        .WithOrganisationGroupIdentifierValue(identifierValue1)
                        .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier)))),
                NewPublishedFunding(_ => _
                    .WithCurrent(NewPublishedFundingVersion(fv => fv.WithProviderFundings(new[]
                        {
                            $"{fundingStreamId}-{fundingPeriodId}-{providerId1}-{majorVersion1}_{minorVersion1}",
                            NewRandomString()
                        })
                        .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                        .WithOrganisationGroupIdentifierValue(identifierValue2)
                        .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier)))),
                NewPublishedFunding(_ => _
                    .WithCurrent(NewPublishedFundingVersion(fv => fv.WithProviderFundings(new[]
                        {
                            $"{fundingStreamId}-{fundingPeriodId}-{providerId1}-{majorVersion1}_{minorVersion1}",
                            NewRandomString()
                        })
                        .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Information)
                        .WithOrganisationGroupIdentifierValue(identifierValue3)
                        .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier))))
            };

            // Act
            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider, 
                                                              organisationGroupResultsData);

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

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider,
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResults)
        {
            PublishedProvidersContext publishedProvidersContext = new PublishedProvidersContext
            {
                OrganisationGroupResultsData = organisationGroupResults
            };

            await _errorDetector.DetectErrors(publishedProvider, publishedProvidersContext);
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

        private static PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        private static PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
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