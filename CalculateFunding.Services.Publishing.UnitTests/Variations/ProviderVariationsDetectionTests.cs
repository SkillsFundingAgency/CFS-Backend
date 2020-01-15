using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

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
            GeneratedProviderResult generatedProviderResult = NewGeneratedProviderResult();
            ApiProvider updatedProvider = NewApiProvider();

            ProviderVariationContext providerVariationContext = await _factory.CreateRequiredVariationChanges(existingPublishedProvider,
                generatedProviderResult,
                updatedProvider,
                fundingVariations);

            providerVariationContext
                .GeneratedProvider
                .Should()
                .BeSameAs(generatedProviderResult);

            providerVariationContext
                .PriorState
                .Should()
                .BeSameAs(existingPublishedProvider.Current);

            providerVariationContext
                .UpdatedProvider
                .Should()
                .BeSameAs(updatedProvider);
            
            Received.InOrder(() =>
            {
                foreach (FundingVariation fundingVariation in fundingVariations.OrderBy(_ => _.Order))
                {
                    _variationStrategyServiceLocator.GetService(fundingVariation.Name);
                    _variationStrategy.DetermineVariations(Arg.Is<ProviderVariationContext>(
                        ctx => ctx.Result != null &&
                               ReferenceEquals(ctx.GeneratedProvider, generatedProviderResult) &&
                               ReferenceEquals(ctx.PriorState, existingPublishedProvider.Current) &&
                               ctx.ProviderId == existingPublishedProvider.Current.ProviderId &&
                               ReferenceEquals(ctx.UpdatedProvider, updatedProvider)));
                }   
            });
        }

        private ApiProvider NewApiProvider()
        {
            return new ApiProviderBuilder()
                .Build();
        }

        private GeneratedProviderResult NewGeneratedProviderResult()
        {
            return new GeneratedProviderResultBuilder()
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
                (NewRandomString(), 13),
                (NewRandomString(), 3));
            yield return NewFundingVariationExample(
                (NewRandomString(), 1),
                (NewRandomString(), 3),
                (NewRandomString(), 5),
                (NewRandomString(), 12));
        }

        private static object[] NewFundingVariationExample(params (string, int)[] variations)
        {
            return new object[] { variations.Select(variation =>
                    NewFundingVariation(fv => fv.WithName(variation.Item1)
                        .WithOrder(variation.Item2)))
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