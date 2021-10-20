using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    public abstract class ProfilingVariationStrategyTestBase : VariationStrategyTestBase
    {
        protected IVariationStrategy VariationStrategy;
        protected string FundingLineCode;

        [TestInitialize]
        public void ProfilingVariationStrategyTestBaseSetUp()
        {
            FundingLineCode = NewRandomString();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfNoPriorStateYet()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>();
                _.AllPublishedProvidersRefreshStates = new Dictionary<string, PublishedProvider>();
            });
            
            await WhenTheVariationsAreProcessed();
            
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfCurrentProviderClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.PriorState.Provider.Status = Closed);

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = Closed);

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfHasDifferentFundingLinesButNotProfilingChangesInExistingLines()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(_ => _.WithDistributionPeriods(NewDistributionPeriod(dp =>
                dp.WithProfilePeriods(NewProfilePeriod())))));

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsFailsPreconditionCheckIfThereIsNoProfilingChanges()
        {
            GivenTheOtherwiseValidVariationContext();

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfThereIsNoPreviouslyReleasedVersion()
        {
            GivenTheOtherwiseValidVariationContext();
            AndThereIsNoPreviouslyReleasedVersion();

            await WhenTheVariationsAreProcessed();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
            VariationContext
                .VariationReasons
                .Should()
                .BeEmpty();
        }

        protected async Task WhenTheVariationsAreProcessed()
        {
            await VariationStrategy.Process(VariationContext, null);
        }

        protected override void GivenTheOtherwiseValidVariationContext(Action<ProviderVariationContext> changes = null)
        {
            base.GivenTheOtherwiseValidVariationContext(changes);

            PublishedProvider publishedProvider =
                VariationContext.GetPublishedProviderOriginalSnapShot(VariationContext.ProviderId);

            if (publishedProvider == null)
            {
                return;
            }

            PublishedProviderVersion publishedProviderCurrent = publishedProvider.Current;

            publishedProviderCurrent.FundingLines = new[]
            {
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(new RandomNumberBetween(1, int.MaxValue))
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod()))))
            };
            publishedProviderCurrent.Provider = NewProvider();

            publishedProvider.Released = publishedProviderCurrent.DeepCopy();
        }

        protected void AndThereIsNoPreviouslyReleasedVersion()
        {
            VariationContext.PublishedProvider.Released = null;
        }

        protected void AndTheRefreshStateFundingLines(params FundingLine[] fundingLines)
        {
            VariationContext.RefreshState.FundingLines = fundingLines;
        }

        protected void AndTheReleaseStateFundingLines(params FundingLine[] fundingLines)
        {
            VariationContext.PriorState.FundingLines = fundingLines;
        }

        protected static RandomNumberBetween NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);
    }
}