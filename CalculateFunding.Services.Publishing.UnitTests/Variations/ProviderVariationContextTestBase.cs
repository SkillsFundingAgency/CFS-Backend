using System;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public abstract class ProviderVariationContextTestBase
    {
        protected ProviderVariationContext VariationContext;
        private int _queuedChangeIndex;

        [TestInitialize]
        public void ProviderVariationContextTestBaseSetUp()
        {
            _queuedChangeIndex = 0;
            
            VariationContext = NewVariationContext();
        }

        protected ProfilePeriod NewProfilePeriod(int occurrence, int year, string month, decimal? amount = null)
        {
            return NewProfilePeriod(_ => _.WithOccurence(occurrence)
                .WithAmount(amount.GetValueOrDefault())
                .WithType(ProfilePeriodType.CalendarMonth)
                .WithYear(year)
                .WithTypeValue(month));
        }

        protected FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }

        protected DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            
            return distributionPeriodBuilder.Build();
        }

        protected ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();
            
            setUp?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
        }

        protected ProviderVariationContext NewVariationContext(Action<ProviderVariationContextBuilder> setUp = null)
        {
            decimal totalFunding = new RandomNumberBetween(100, 100000);
            
            ProviderVariationContextBuilder variationContextBuilder = new ProviderVariationContextBuilder()
                .WithPublishedProvider(NewPublishedProvider(_ => _.WithReleased(NewPublishedProviderVersion(ppv =>
                    ppv.WithTotalFunding(totalFunding)))))
                .WithCurrentState(NewApiProvider(_ => _.WithStatus(Variation.Closed)))
                .WithGeneratedProviderResult(NewGeneratedProviderResult(_ => _.WithTotalFunding(totalFunding)));

            setUp?.Invoke(variationContextBuilder);

            return variationContextBuilder.Build();
        }

        protected PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);
            
            return publishedProviderBuilder.Build();
        }

        protected PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder()
                .WithProvider(NewProvider());

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        protected Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected Common.ApiClient.Providers.Models.Provider NewApiProvider(Action<ApiProviderBuilder> setUp = null)
        {
            ApiProviderBuilder providerBuilder = new ApiProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected GeneratedProviderResult NewGeneratedProviderResult(Action<GeneratedProviderResultBuilder> setUp = null)
        {
            GeneratedProviderResultBuilder providerResultBuilder = new GeneratedProviderResultBuilder();

            setUp?.Invoke(providerResultBuilder);

            return providerResultBuilder.Build();
        }

        protected string NewRandomString() => new RandomString();

        protected void ThenTheVariationChangeWasQueued<TChange>()
            where TChange : IVariationChange
        {
            VariationContext
                .QueuedChanges
                .Count()
                .Should()
                .BeGreaterOrEqualTo(_queuedChangeIndex + 1);
            
            IVariationChange variationChange = VariationContext
                .QueuedChanges
                .ElementAt(_queuedChangeIndex);

            variationChange
                .Should()
                .BeOfType<TChange>();

            _queuedChangeIndex++;
        }

        protected void AndTheVariationChangeWasQueued<TChange>()
            where TChange : IVariationChange
        {
            ThenTheVariationChangeWasQueued<TChange>();
        }

        protected void AndNoVariationChangesWereQueued()
        {
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        protected void ThenTheErrorWasRecorded(string error)
        {
            VariationContext
                .ErrorMessages
                .Count(_ => _.Contains(error))
                .Should()
                .Be(1);
        }

        protected void GivenTheSuccessorFundingLines(params FundingLine[] fundingLines)
        {
            VariationContext.SuccessorRefreshState.FundingLines = fundingLines;
        }
    }
}