using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class VariationChangeTestBase : ProviderVariationContextTestBase
    {
        protected IVariationChange Change;
        protected IApplyProviderVariations VariationsApplication;
        
        private ISpecificationsApiClient _specificationsApiClient;

        [TestInitialize]
        public void VariationChangeTestBaseSetUp()
        {
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            VariationsApplication = Substitute.For<IApplyProviderVariations>();

            VariationsApplication.SpecificationsApiClient
                .Returns(_specificationsApiClient);

            VariationsApplication.ResiliencePolicies
            .Returns(new ResiliencePolicies
            {
                SpecificationsApiClient = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                ProfilingApiClient = Policy.NoOpAsync()
            });
        }

        protected async Task WhenTheChangeIsApplied()
        {
            await Change.Apply(VariationsApplication);
        }

        protected void GivenTheVariationPointersForTheSpecification(params ProfileVariationPointer[] variationPointers)
        {
            VariationContext.VariationPointers = variationPointers;
        }

        protected void AndTheVariationPointersForTheSpecification(params ProfileVariationPointer[] variationPointers)
        {
            GivenTheVariationPointersForTheSpecification(variationPointers);
        }

        protected ProfileVariationPointer NewVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder variationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(variationPointerBuilder);

            return variationPointerBuilder.Build();
        }

        protected void AndTheSuccessorFundingLines(params FundingLine[] fundingLines)
        {
            GivenTheSuccessorFundingLines(fundingLines);
        }

        protected void AndTheProfilePeriodAmountShouldBe(ProfilePeriod profilePeriod, decimal expectedAmount)
        {
            profilePeriod
                .ProfiledValue
                .Should()
                .Be(expectedAmount);
        }

        protected void AndTheProfilePeriodsAmountShouldBe(ProfilePeriod[] profilePeriods, decimal expectedAmount)
        {
            foreach (ProfilePeriod profilePeriod in profilePeriods)
            {
                AndTheProfilePeriodAmountShouldBe(profilePeriod, expectedAmount);
            }
        }
        protected PublishedProviderVersion CurrentState => VariationContext.CurrentState;

        protected PublishedProviderVersion RefreshState => VariationContext.RefreshState;

        protected PublishedProviderVersion PriorState => VariationContext.PriorState;
    }
}
