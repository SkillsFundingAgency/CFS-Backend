using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.UnitTests.Variations;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VariationReason = CalculateFunding.Models.Publishing.VariationReason;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class GenerateVariationReasonsForProvidersTests
    {
        private Mock<IDetectProviderVariations> _detectProviderVariations;
        private Mock<IPublishedProvidersLoadContext> _publishedProvidersLoadContext;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IProviderService> _providerService;
        private Mock<ISpecificationService> _specificationService;
        private Mock<IPoliciesService> _policiesService;

        private GenerateVariationReasonsForChannelService _generateVariationReasonsForChannelService;


        [TestInitialize]
        public void SetUp()
        {
            _detectProviderVariations = new Mock<IDetectProviderVariations>();
            _publishedProvidersLoadContext = new Mock<IPublishedProvidersLoadContext>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _providerService = new Mock<IProviderService>();
            _specificationService = new Mock<ISpecificationService>();
            _policiesService = new Mock<IPoliciesService>();

            _generateVariationReasonsForChannelService = new GenerateVariationReasonsForChannelService(
                _detectProviderVariations.Object,
                _publishedProvidersLoadContext.Object,
                _releaseManagementRepository.Object,
                _providerService.Object,
                _specificationService.Object);
        }

        [TestMethod]
        public async Task GenerateVariationReasonsForProviders()
        {
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();
            string providerIdThree = NewRandomString();

            decimal totalFundingOne = NewRandomDecimal();
            string specificationId = NewRandomString();
            int channelId = NewRandomNumber();
            int majorVersionOne = NewRandomNumber();
            int majorVersionTwo = NewRandomNumber();
            string providerVersionId = NewRandomString();

            IEnumerable<string> batchProviderIds = new List<string>
            {
                providerIdOne,
                providerIdTwo,
                providerIdThree
            };

            Channel channel = NewChannel(_ => _.WithChannelId(channelId));
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId).WithProviderVersionId(providerVersionId));

            FundingVariation fundingVariation = NewFundingVariation(); 
            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithFundingVariations(fundingVariation));
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResults = new Dictionary<string, IEnumerable<OrganisationGroupResult>>();

            IEnumerable<int> channelIds = new[] { channel.ChannelId };

            IEnumerable<PublishedProvider> publishedProviders = new List<PublishedProvider>
            {
                NewPublishedProvider(pp => pp
                    .WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerIdOne).WithTotalFunding(totalFundingOne)))
                    .WithReleased(
                        NewPublishedProviderVersion(ppv => ppv.WithMajorVersion(majorVersionOne)
                        ))),
                NewPublishedProvider(pp => pp
                    .WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerIdTwo)))),
                                NewPublishedProvider(pp => pp
                    .WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv.WithProviderId(providerIdThree).WithTotalFunding(totalFundingOne)))
                    .WithReleased(
                        NewPublishedProviderVersion(ppv => ppv.WithMajorVersion(majorVersionTwo)
                        ))),
            };
            GetOrLoadProviders(batchProviderIds, publishedProviders);

            IEnumerable<ProviderVersionInChannel> providerVersionInChannels = new List<ProviderVersionInChannel>
            {
                new ProviderVersionInChannel
                {
                    MajorVersion = majorVersionTwo,
                    ProviderId = providerIdOne
                },
                new ProviderVersionInChannel
                {
                    MajorVersion = majorVersionTwo,
                    ProviderId = providerIdThree
                },
            };

            GetLatestPublishedProviderVersions(specificationId, channelIds, providerVersionInChannels);

            Provider provider = NewProvider();
            IDictionary<string, Provider> providers = new Dictionary<string, Provider>
            {
                { providerIdOne, provider }
            };
            GetScopedProvidersForSpecification(specificationId, providerVersionId, providers);

            IEnumerable<ProfileVariationPointer> profileVariationPointers = new List<ProfileVariationPointer>
            {
                NewProfileVariationPointer()
            };
            GetProfileVariationPointers(specificationId, profileVariationPointers);

            PublishedProvider previousReleasedMajorVersion = NewPublishedProvider();
            GetOrLoadProvider(providerIdOne, majorVersionTwo, previousReleasedMajorVersion);

            VariationReason variationReason = VariationReason.AuthorityFieldUpdated;
            ProviderVariationContext providerVariationContext = NewProviderVariationContext(_ => _.WithVariationReasons(variationReason).WithPoliciesService(_policiesService.Object));

            CreateRequiredVariationChanges(
                previousReleasedMajorVersion,
                totalFundingOne,
                provider,
                fundingConfiguration.ReleaseManagementVariations,
                publishedProviders.ToDictionary(_ => _.Current.ProviderId),
                profileVariationPointers,
                providerVersionId,
                organisationGroupResults,
                providerVariationContext,
                specificationSummary.FundingStreams.FirstOrDefault()?.Id,
                specificationSummary.FundingPeriod.Id);

            IDictionary<string, IEnumerable<VariationReason>> actual =
                await _generateVariationReasonsForChannelService.GenerateVariationReasonsForProviders(
                    batchProviderIds, 
                    channel, 
                    specificationSummary, 
                    fundingConfiguration, 
                    organisationGroupResults);

            IDictionary<string, IEnumerable<VariationReason>> expected = new Dictionary<string, IEnumerable<VariationReason>>
            {
                {providerIdOne, new []{ variationReason } },
                {providerIdTwo, new[] { VariationReason.FundingUpdated, VariationReason.ProfilingUpdated } },
                {providerIdThree, Array.Empty<VariationReason>() },
            };

            AssertProviderVariationResults(actual, expected);
        }

        private void AssertProviderVariationResults(
            IDictionary<string, IEnumerable<VariationReason>> actual,
            IDictionary<string, IEnumerable<VariationReason>> expected)
        {
            foreach (string providerId in expected.Keys)
            {
                Assert.IsTrue(actual.ContainsKey(providerId));
                Assert.IsTrue(expected.ContainsKey(providerId));
                Assert.IsTrue(actual[providerId].SequenceEqual(expected[providerId]));
            }
        }

        private void CreateRequiredVariationChanges(PublishedProvider existingPublishedProvider,
            decimal? totalFunding,
            Provider provider,
            IEnumerable<FundingVariation> variations,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            IEnumerable<ProfileVariationPointer> variationPointers,
            string providerVersionId,
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData,
            ProviderVariationContext providerVariationContext,
            string fundingStreamId,
            string fundingPeriodId,
            IEnumerable<string> variances = null)
        {
            _detectProviderVariations
                .Setup(_ => _.CreateRequiredVariationChanges(
                    existingPublishedProvider,
                    totalFunding,
                    provider,
                    variations,
                    It.IsAny<IDictionary<string, PublishedProviderSnapShots>>(),
                    It.Is<Dictionary<string, PublishedProvider>>(d => d.SequenceEqual(allPublishedProviderRefreshStates)),
                    variationPointers,
                    providerVersionId,
                    organisationGroupResultsData,
                    fundingStreamId,
                    fundingPeriodId,
                    It.IsAny<PublishedProviderVersion>(),
                    variances))
                .ReturnsAsync(providerVariationContext);
        }

        private void GetOrLoadProvider(string providerId, int majorVersion, PublishedProvider previousReleasedMajorVersion)
        {
            _publishedProvidersLoadContext
                .Setup(_ => _.GetOrLoadProvider(providerId, majorVersion))
                .ReturnsAsync(previousReleasedMajorVersion);
        }

        private void GetProfileVariationPointers(string specificationId, IEnumerable<ProfileVariationPointer> profileVariationPointers)
        {
            _specificationService
                .Setup(_ => _.GetProfileVariationPointers(specificationId))
                .ReturnsAsync(profileVariationPointers);
        }

        private void GetScopedProvidersForSpecification(string specificationId, string providerVersionId, IDictionary<string, Provider> providers)
        {
            _providerService
                .Setup(_ => _.GetScopedProvidersForSpecification(specificationId, providerVersionId))
                .ReturnsAsync(providers);
        }

        private void GetLatestPublishedProviderVersions(string specificationId, IEnumerable<int> channelIds, IEnumerable<ProviderVersionInChannel> providerVersionInChannels)
        {
            _releaseManagementRepository
                .Setup(_ => _.GetLatestPublishedProviderVersions(
                    specificationId, 
                    It.Is<IEnumerable<int>>(i => i.SequenceEqual(channelIds)) ))
                .ReturnsAsync(providerVersionInChannels);
        }

        private void GetOrLoadProviders(IEnumerable<string> batchProviderIds, IEnumerable<PublishedProvider> publishedProviders)
        {
            _publishedProvidersLoadContext
                .Setup(_ => _.GetOrLoadProviders(batchProviderIds))
                .ReturnsAsync(publishedProviders);
        }

        private static ProviderVariationContext NewProviderVariationContext(Action<ProviderVariationContextBuilder> setup = null)
        {
            ProviderVariationContextBuilder providerVariationContextBuilder = new ProviderVariationContextBuilder();

            setup?.Invoke(providerVariationContextBuilder);

            return providerVariationContextBuilder.Build();
        }

        private static ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setup = null)
        {
            ProfileVariationPointerBuilder profileVariationPointerBuilder = new ProfileVariationPointerBuilder();

            setup?.Invoke(profileVariationPointerBuilder);

            return profileVariationPointerBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setup = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setup?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setup = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setup?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static Provider NewProvider(Action<ProviderBuilder> setup = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setup?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private static Channel NewChannel(Action<ChannelBuilder> setup = null)
        {
            ChannelBuilder channelBuilder = new ChannelBuilder();

            setup?.Invoke(channelBuilder);

            return channelBuilder.Build();
        }

        private static SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setup = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setup?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private static FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setup = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setup?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private static FundingVariation NewFundingVariation(Action<FundingVariationBuilder> setup = null)
        {
            FundingVariationBuilder fundingVariationBuilder = new FundingVariationBuilder();

            setup?.Invoke(fundingVariationBuilder);

            return fundingVariationBuilder.Build();
        }

        private decimal NewRandomDecimal() => new RandomNumberBetween(1, 1000);
        private int NewRandomNumber() => new RandomNumberBetween(1, 1000);
        private string NewRandomString() => new RandomString();
    }
}
