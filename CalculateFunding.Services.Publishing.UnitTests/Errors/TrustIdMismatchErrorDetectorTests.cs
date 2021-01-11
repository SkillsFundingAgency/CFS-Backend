using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    class TrustIdMismatchErrorDetectorTests
    {
        private TrustIdMismatchErrorDetector _errorDetector;
        private IOrganisationGroupGenerator _organisationGroupGenerator;
        private IMapper _mapper;
        private IPublishedFundingDataService _publishedFundingDataService;
        private IPublishingResiliencePolicies _publishingResiliencePolicies;

        [TestInitialize]
        public void SetUp()
        {
            _organisationGroupGenerator = Substitute.For<IOrganisationGroupGenerator>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _publishingResiliencePolicies = new ResiliencePolicies
            {
                PublishedFundingRepository = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync()
            };
            _mapper = new MapperConfiguration(_ =>
            {
                _.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper();
            _errorDetector = new TrustIdMismatchErrorDetector(
                _organisationGroupGenerator, 
                _mapper,
                _publishedFundingDataService,
                _publishingResiliencePolicies);

        }

        [TestMethod]
        public async Task ReturnsErrorMessageWhenTrustIdMisatch()
        {
            // Arrange
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
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
            string fundingConfigurationId = NewRandomString();
            Generators.OrganisationGroup.Enums.OrganisationGroupTypeIdentifier groupTypeIdentifier = Generators.OrganisationGroup.Enums.OrganisationGroupTypeIdentifier.UKPRN;

            string summaryErrorMessage = "TrustId  not matched";
            string detailedErrorMessage = $"TrustId {groupTypeIdentifier}-{identifierValue2} not matched.";

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId).WithProviderVersionId(providerVersionId));
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _
                .WithCurrent(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                                                                .WithFundingPeriodId(fundingPeriodId)))
                .WithReleased(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                                                                .WithFundingPeriodId(fundingPeriodId)
                                                                .WithProviderId(providerId2)
                                                                .WithMajorVersion(majorVersion2)
                                                                .WithMinorVersion(minorVersion2))));

            IEnumerable<Provider> providers = new[]
            {
                NewProvider(_ => _.WithProviderId(providerId1)),
                 NewProvider(_ => _.WithProviderId(providerId2))
            };

            IEnumerable<OrganisationGroupResult> organisationGroupResults = new[]
            {
               NewOrganisationGroupResult(_ => _
               .WithIdentifiers(new []
               {
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue1)),
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
               })),
               NewOrganisationGroupResult(_ => _
               .WithIdentifiers(new []
               {
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue2)),
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
               }))
            };

            IEnumerable<PublishedFunding> publishedFundings = new[]
            {
                NewPublishedFunding(_ => _
                .WithCurrent(NewPublishedFundingVersion(fv => fv.WithProviderFundings(new []
                                                                        {
                                                                            $"{fundingStreamId}-{fundingPeriodId}-{providerId1}-{majorVersion1}_{minorVersion1}",
                                                                            $"{fundingStreamId}-{fundingPeriodId}-{providerId2}-{majorVersion2}_{minorVersion2}"
                                                                        })
                                                                .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                                                                .WithOrganisationGroupIdentifierValue(identifierValue1)
                                                                .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier)))),
                NewPublishedFunding(_ => _
                .WithCurrent(NewPublishedFundingVersion(fv => fv.WithProviderFundings(new []{ $"{fundingStreamId}-{fundingPeriodId}-{providerId1}-{majorVersion1}_{minorVersion1}", NewRandomString() })
                                                                .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                                                                .WithOrganisationGroupIdentifierValue(identifierValue2)
                                                                .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier))))
            };

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithId(fundingConfigurationId));
            GivenTheCurrentPublishedFundingForTheSpecification(specificationId, publishedFundings);
            GivenTheOrganisationGroupsForTheFundingConfiguration(fundingConfiguration, providers, providerVersionId, organisationGroupResults);
            
            // Act
            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider, providers, specificationId, providerVersionId, fundingConfiguration, publishedFundings);

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

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider, IEnumerable<Provider> providers, string specificationId, string providerVersionId, FundingConfiguration fundingConfiguration, IEnumerable<PublishedFunding> publishedFundings)
        {
            PublishedProvidersContext publishedProvidersContext = new PublishedProvidersContext
            {
                ScopedProviders = providers,
                SpecificationId = specificationId,
                ProviderVersionId = providerVersionId,
                CurrentPublishedFunding = publishedFundings,
                OrganisationGroupResultsData = new Dictionary<string, HashSet<string>>(),
                FundingConfiguration = fundingConfiguration
            };

            await _errorDetector.DetectErrors(publishedProvider, publishedProvidersContext);
        }

        private void GivenTheCurrentPublishedFundingForTheSpecification(string specificationId, IEnumerable<PublishedFunding> publishedFundings)
        {
            _publishedFundingDataService.GetCurrentPublishedFunding(specificationId, GroupingReason.Payment)
                .Returns(publishedFundings);
        }

        private void GivenTheOrganisationGroupsForTheFundingConfiguration(FundingConfiguration fundingConfiguration, IEnumerable<Provider> scopedProviders, string providerVersionId, IEnumerable<OrganisationGroupResult> organisationGroupResults)
        {
            _organisationGroupGenerator.GenerateOrganisationGroup(
                Arg.Is<FundingConfiguration>(f => f.Id == fundingConfiguration.Id),
                Arg.Is<IEnumerable<Common.ApiClient.Providers.Models.Provider>>(p => p.All(i => scopedProviders.Any(sp => sp.ProviderId == i.ProviderId))),
                providerVersionId)
                .Returns(organisationGroupResults);
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
