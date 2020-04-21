using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    [TestClass]
    public class ProviderVariationsDetectionTests
    {
        private IVariationStrategyServiceLocator _variationStrategyServiceLocator;
        private ProviderVariationsDetection _factory;
        private IVariationStrategy _variationStrategy;

        [TestInitialize]
        public void SetUp()
        {
            _variationStrategyServiceLocator = Substitute.For<IVariationStrategyServiceLocator>();
            _variationStrategy = Substitute.For<IVariationStrategy>();
            
            _factory = new ProviderVariationsDetection(_variationStrategyServiceLocator);
            
            _variationStrategyServiceLocator
                .GetService(Arg.Any<string>())
                .Returns(_variationStrategy);
        }

        [TestMethod]
        [DynamicData(nameof(FundingVariationExamples), DynamicDataSourceType.Method)]
        public async Task ExecutesVariationStrategiesSpecifiedInSuppliedFundingVariationsAndReturnsVariationResult(
            FundingVariation[] fundingVariations)
        {
            PublishedProvider existingPublishedProvider = NewPublishedProvider();
            Provider updatedProvider = NewApiProvider();
            decimal updatedTotalFunding = new RandomNumberBetween(0, 1000);
            IDictionary<string, PublishedProviderSnapShots> allPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>();
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates = new Dictionary<string, PublishedProvider>();
            string providerVersionId = NewRandomString();

            ProviderVariationContext providerVariationContext = await _factory.CreateRequiredVariationChanges(existingPublishedProvider,
                updatedTotalFunding,
                updatedProvider,
                fundingVariations, 
                allPublishedProviderSnapShots,
                allPublishedProviderRefreshStates,
                providerVersionId);

            providerVariationContext
                .UpdatedTotalFunding
                .Should()
                .Equals(updatedTotalFunding);

            providerVariationContext
                .ReleasedState
                .Should()
                .BeSameAs(existingPublishedProvider.Released);

            providerVariationContext
                .UpdatedProvider
                .Should()
                .BeSameAs(updatedProvider);

            providerVariationContext
                .ProviderVersionId
                .Should()
                .BeSameAs(providerVersionId);

            Received.InOrder(() =>
            {
                foreach (FundingVariation fundingVariation in fundingVariations.OrderBy(_ => _.Order))
                {
                    _variationStrategyServiceLocator.GetService(fundingVariation.Name);
                    _variationStrategy.DetermineVariations(Arg.Is<ProviderVariationContext>(
                        ctx => ctx.UpdatedTotalFunding == updatedTotalFunding &&
                               ReferenceEquals(ctx.ReleasedState, existingPublishedProvider.Released) &&
                               ctx.ProviderId == existingPublishedProvider.Current.ProviderId &&
                               ReferenceEquals(ctx.UpdatedProvider, updatedProvider) &&
                               ReferenceEquals(ctx.AllPublishedProviderSnapShots, allPublishedProviderSnapShots) &&
                               ReferenceEquals(ctx.AllPublishedProvidersRefreshStates, allPublishedProviderRefreshStates) &&
                               ctx.ProviderVersionId == providerVersionId), 
                        fundingVariation.FundingLineCodes);
                }   
            });
        }

        private Provider NewApiProvider()
        {
            return new ProviderBuilder()
                .Build();
        }

        private PublishedProvider NewPublishedProvider()
        {
            return new PublishedProviderBuilder()
                .WithCurrent(NewPublishedProviderVersion())
                .Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion()
        {
            return new PublishedProviderVersionBuilder()
                .Build();
        }

        private static IEnumerable<object[]> FundingVariationExamples()
        {
            yield return NewFundingVariationExample(
                NewFundingLineCodes(NewRandomString()),
                (NewRandomString(), 13),
                (NewRandomString(), 3));
            yield return NewFundingVariationExample(
                NewFundingLineCodes(NewRandomString(), NewRandomString(), NewRandomString()),
                (NewRandomString(), 1),
                (NewRandomString(), 3),
                (NewRandomString(), 5),
                (NewRandomString(), 12));
        }

        private static string[] NewFundingLineCodes(params string[] codes) => codes;

        private static object[] NewFundingVariationExample(string[] fundingLineCodes, 
            params (string, int)[] variations)
        {
            return new object[] { variations.Select(variation =>
                    NewFundingVariation(fv => fv.WithName(variation.Item1)
                        .WithOrder(variation.Item2)
                        .WithFundingLineCodes(fundingLineCodes)))
                .ToArray() };
        }

        private static FundingVariation NewFundingVariation(Action<FundingVariationBuilder> setUp = null)
        {
            FundingVariationBuilder variationBuilder = new FundingVariationBuilder();

            setUp?.Invoke(variationBuilder);
            
            return variationBuilder.Build();
        }
        
        private static string NewRandomString() => new RandomString();
    }
}