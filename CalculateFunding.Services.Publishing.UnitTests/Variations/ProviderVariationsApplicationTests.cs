using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    [TestClass]
    public class ProviderVariationsApplicationTests
    {
        private ProviderVariationsApplication _variationsApplication;

        [TestInitialize]
        public void SetUp()
        {
            _variationsApplication = new ProviderVariationsApplication(new ResiliencePolicies
                {
                    CacheProvider = Policy.NoOpAsync(),
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync()
                }, 
                Substitute.For<ISpecificationsApiClient>(),
                Substitute.For<IPoliciesApiClient>(),
                Substitute.For<ICacheProvider>());
        }

        [TestMethod]
        public async Task AddsVariationContextsToBeAppliedLater()
        {
            ProviderVariationContext contextOne = NewVariationContext();
            ProviderVariationContext contextTwo = NewVariationContext();
            
            GivenTheVariationContextWasAdded(contextOne);
            AndTheVariationContextWasAdded(contextTwo);

            await WhenTheVariationsAreApplied();

            await ThenTheVariationsWereApplied(contextOne);
            await AndTheVariationsWereApplied(contextTwo);
        }

        [TestMethod]
        [DynamicData(nameof(HasErrorExamples), DynamicDataSourceType.Method)]
        public void DetectsErrorsByProjectingOverVariationsCollectionInternally(ProviderVariationContext[] variationContexts,
            bool expectedHasErrorsFlag)
        {
            GivenTheVariationContextsWereAdded(variationContexts);

            _variationsApplication
                .HasErrors
                .Should()
                .Be(expectedHasErrorsFlag);
        }

        [TestMethod]
        [DynamicData(nameof(ErrorRollUpExamples), DynamicDataSourceType.Method)]
        public void ReturnsErrorsForAllVariationsTrackedInternally(ProviderVariationContext[] variationContexts,
            string[] expectedErrorMessages)
        {
            GivenTheVariationContextsWereAdded(variationContexts);

            _variationsApplication
                .ErrorMessages
                .Should()
                .BeEquivalentTo(expectedErrorMessages);
        }

        private static IEnumerable<object[]> HasErrorExamples()
        {
            yield return new object[]
            {
                new[]
                {
                    NewVariationContext(_ => _.WithErrors("one")),
                    NewVariationContext()
                },
                true
            };
            yield return new object[]
            {
                new[]
                {
                    NewVariationContext(),
                    NewVariationContext()
                },
                false
            };
            yield return new object[]
            {
                new[]
                {
                    NewVariationContext(),
                    NewVariationContext(_ => _.WithErrors("two")),
                    NewVariationContext()
                },
                true
            };
        }

        private static IEnumerable<object[]> ErrorRollUpExamples()
        {
            yield return new object[]
            {
                new[]
                {
                    NewVariationContext(_ => _.WithErrors("one")),
                    NewVariationContext(),
                    NewVariationContext(_ => _.WithErrors("two", "three"))
                },
                new [] { "one", "two", "three"}
            };
            yield return new object[]
            {
                new[]
                {
                    NewVariationContext(),
                    NewVariationContext(),
                },
                new string[0]
            };
        }
        
        private Task WhenTheVariationsAreApplied()
        {
            return _variationsApplication.ApplyProviderVariations();
        }

        private void GivenTheVariationContextWasAdded(ProviderVariationContext variationContext)
        {
            _variationsApplication.AddVariationContext(variationContext);
        }

        private void GivenTheVariationContextsWereAdded(params ProviderVariationContext[] variationContexts)
        {
            foreach (ProviderVariationContext providerVariationContext in variationContexts)
            {
                GivenTheVariationContextWasAdded(providerVariationContext);
            }
        }

        private void AndTheVariationContextWasAdded(ProviderVariationContext variationContext)
        {
            GivenTheVariationContextWasAdded(variationContext);
        }

        private async Task ThenTheVariationsWereApplied(ProviderVariationContext variationContext)
        {
            await variationContext
                .Received(1)
                .ApplyVariationChanges(_variationsApplication);
        }

        private async Task AndTheVariationsWereApplied(ProviderVariationContext variationContext)
        {
            await ThenTheVariationsWereApplied(variationContext);
        }

        private static ProviderVariationContext NewVariationContext() => Substitute.ForPartsOf<ProviderVariationContext>();

        private static ProviderVariationContext NewVariationContext(Action<ProviderVariationContextBuilder> setUp)
        {
            ProviderVariationContextBuilder variationContextBuilder = new ProviderVariationContextBuilder();

            setUp(variationContextBuilder);
            
            return variationContextBuilder.Build();
        }
    }
}