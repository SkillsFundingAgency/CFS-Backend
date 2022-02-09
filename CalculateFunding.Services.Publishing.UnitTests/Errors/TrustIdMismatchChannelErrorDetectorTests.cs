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
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class TrustIdMismatchChannelErrorDetectorTests
    {
        private TrustIdMismatchChannelErrorDetector _errorDetector;
        private IPublishedFundingDataService _publishedFundingDataService;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _errorDetector = new TrustIdMismatchChannelErrorDetector();
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
        public async Task ReturnsErrorMessageWhenTrustIdMismatch()
        {
            OrganisationGroupTypeIdentifier groupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN;
            string identifierValue = NewRandomString();

            string summaryErrorMessage = "TrustId not matched";
            string detailedErrorMessage = $"TrustId {groupTypeIdentifier}-{identifierValue} not matched.";

            PublishedProvider publishedProvider = await RunDetectTrustIdMismatchErrors(false, identifierValue);

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
                .Be(detailedErrorMessage);

            publishedProvider
                .Current
                .Errors
                .First()
                .SummaryErrorMessage
                .Should()
                .Be(summaryErrorMessage);
        }

        [TestMethod]
        public async Task ReturnsNoErrorMessagesWhenNoTrustIdMismatch()
        {
            PublishedProvider publishedProvider = await RunDetectTrustIdMismatchErrors(true);

            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        private async Task<PublishedProvider> RunDetectTrustIdMismatchErrors(bool withCompletePublishedFunding, string identifierValue2 = null)
        {
            // Arrange
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            OrganisationGroupTypeIdentifier groupTypeIdentifier = OrganisationGroupTypeIdentifier.UKPRN;

            string identifierValue1 = NewRandomString();
            identifierValue2 ??= NewRandomString();
            string providerId1 = NewRandomString();
            int majorVersion1 = NewRandomInt();
            int minorVersion1 = NewRandomInt();
            string providerId2 = NewRandomString();
            int majorVersion2 = NewRandomInt();
            int minorVersion2 = NewRandomInt();
            string fundingConfigurationId = NewRandomString();
            
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId).WithProviderVersionId(providerVersionId));
            PublishedProvider publishedProvider = GivenPublishedProvider(fundingStreamId, fundingPeriodId, providerId2, majorVersion2, minorVersion2);
            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupChannelResultsData = AndOrganisationGroupChannelResultsData(groupTypeIdentifier, identifierValue1, identifierValue2, providerId2);

            IEnumerable<Provider> providers = new[]
            {
                NewProvider(_ => _.WithProviderId(providerId1)),
                NewProvider(_ => _.WithProviderId(providerId2))
            };

            IEnumerable<PublishedFunding> publishedFundings = AndPublishedFunding(withCompletePublishedFunding, fundingStreamId, fundingPeriodId, providerId1, providerId2,
                majorVersion1, minorVersion1, majorVersion2, minorVersion2, identifierValue1, identifierValue2, groupTypeIdentifier);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithId(fundingConfigurationId));
            GivenTheCurrentPublishedFundingForTheSpecification(specificationId, publishedFundings);

            // Act
            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider,
                                                              providers,
                                                              specificationId,
                                                              providerVersionId,
                                                              fundingConfiguration,
                                                              publishedFundings,
                                                              organisationGroupChannelResultsData);

            return publishedProvider;
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider,
            IEnumerable<Provider> providers,
            string specificationId,
            string providerVersionId,
            FundingConfiguration fundingConfiguration,
            IEnumerable<PublishedFunding> publishedFundings,
            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupChannelResults)
        {
            PublishedProvidersContext publishedProvidersContext = new PublishedProvidersContext
            {
                ScopedProviders = providers,
                SpecificationId = specificationId,
                ProviderVersionId = providerVersionId,
                CurrentPublishedFunding = publishedFundings,
                ChannelOrganisationGroupResultsData = organisationGroupChannelResults,
                FundingConfiguration = fundingConfiguration
            };

            await _errorDetector.DetectErrors(publishedProvider, publishedProvidersContext);
        }

        private void GivenTheCurrentPublishedFundingForTheSpecification(string specificationId,
            IEnumerable<PublishedFunding> publishedFundings)
        {
            _publishedFundingDataService.GetCurrentPublishedFunding(specificationId, GroupingReason.Payment)
                .Returns(publishedFundings);
        }

        private PublishedProvider GivenPublishedProvider(string fundingStreamId, string fundingPeriodId, string providerId, int majorVersion2, int minorVersion2)
        {
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

        private Dictionary<string, IEnumerable<OrganisationGroupResult>> AndOrganisationGroupChannelResultsData(OrganisationGroupTypeIdentifier groupTypeIdentifier, string identifierValue1, 
            string identifierValue2, string providerId)
        {
            IEnumerable<OrganisationGroupResult> organisationGroupResults = new[]
            {
                NewOrganisationGroupResult(_ => _
                    .WithIdentifiers(new[]
                    {
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue1)),
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
                    }))
                ,
                NewOrganisationGroupResult(_ => _
                    .WithIdentifiers(new[]
                    {
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue2)),
                        NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
                    })
                    .WithProviders(new[] { new ApiProvider { ProviderId = providerId } }))
            };

            return new Dictionary<string, IEnumerable<OrganisationGroupResult>> { { $"ChannelOne", organisationGroupResults } };
        }

        private IEnumerable<PublishedFunding> AndPublishedFundingWithMissingProvider(string fundingStreamId, string fundingPeriodId, string providerId1, string providerId2, int majorVersion1,
            int minorVersion1, int majorVersion2, int minorVersion2, string identifierValue1, string identifierValue2, OrganisationGroupTypeIdentifier groupTypeIdentifier)
        {
            return AndPublishedFunding(false, fundingStreamId, fundingPeriodId, providerId1, providerId2, majorVersion1,
                minorVersion1, majorVersion2, minorVersion2, identifierValue1, identifierValue2, groupTypeIdentifier);
        }

        private IEnumerable<PublishedFunding> AndPublishedFundingComplete(string fundingStreamId, string fundingPeriodId, string providerId1, string providerId2, int majorVersion1,
            int minorVersion1, int majorVersion2, int minorVersion2, string identifierValue1, string identifierValue2, OrganisationGroupTypeIdentifier groupTypeIdentifier)
        {
            return AndPublishedFunding(true, fundingStreamId, fundingPeriodId, providerId1, providerId2, majorVersion1,
                minorVersion1, majorVersion2, minorVersion2, identifierValue1, identifierValue2, groupTypeIdentifier);
        }

        private IEnumerable<PublishedFunding> AndPublishedFunding(bool completeFunding, string fundingStreamId, string fundingPeriodId, string providerId1, string providerId2, int majorVersion1,
            int minorVersion1, int majorVersion2, int minorVersion2, string identifierValue1, string identifierValue2, OrganisationGroupTypeIdentifier groupTypeIdentifier)
        {
            return new[]
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
                            completeFunding ? $"{fundingStreamId}-{fundingPeriodId}-{providerId2}-{majorVersion2}_{minorVersion2}" : NewRandomString()
                        })
                        .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                        .WithOrganisationGroupIdentifierValue(identifierValue2)
                        .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier))))
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

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
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

        private static SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
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

        private static FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();
        private static int NewRandomInt() => new RandomNumberBetween(1, 10);
    }
}