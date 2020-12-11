using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ProfilingUpdatedVariationStrategyTests : VariationStrategyTestBase
    {
        private ProfilingUpdatedVariationStrategy _variationStrategy;
        private string _fundingLineCode;

        [TestInitialize]
        public void SetUp()
        {
            _variationStrategy = new ProfilingUpdatedVariationStrategy();

            _fundingLineCode = NewRandomString();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfNoPriorStateYet()
        {
            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.AllPublishedProviderSnapShots = new Dictionary<string, PublishedProviderSnapShots>();
                _.AllPublishedProvidersRefreshStates = new Dictionary<string, PublishedProvider>();
            });
            
            await WhenTheVariationsAreDetermined();
            
            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }
        
        [TestMethod]
        public async Task FailsPreconditionCheckIfCurrentProviderClosed()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.PriorState.Provider.Status = Closed);

            await WhenTheVariationsAreDetermined();

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

            await WhenTheVariationsAreDetermined();

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

            await WhenTheVariationsAreDetermined();

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

            await WhenTheVariationsAreDetermined();

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

            await WhenTheVariationsAreDetermined();

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
        public async Task AddsProfilingUpdatedVariationReasonAndQueuesMetaDataVariationChangeIfProfilingDiffers()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Status = NewRandomString());
            AndTheRefreshStateFundingLines(NewFundingLine(_ => _.WithFundingLineCode(_fundingLineCode)
                .WithFundingLineType(FundingLineType.Payment)
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                dp.WithProfilePeriods(NewProfilePeriod())))));

            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<MetaDataVariationsChange>();
            AndTheVariationReasonsWereRecordedOnTheVariationContext(VariationReason.ProfilingUpdated);
        }

        private async Task WhenTheVariationsAreDetermined()
        {
            await _variationStrategy.DetermineVariations(VariationContext, null);
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

            publishedProviderCurrent.FundingLines = new[] {NewFundingLine(_ => _.WithFundingLineCode(_fundingLineCode)
                .WithFundingLineType(FundingLineType.Payment)
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                dp.WithProfilePeriods(NewProfilePeriod()))))};
            publishedProviderCurrent.Provider = NewProvider();

            publishedProvider.Released = publishedProviderCurrent.DeepCopy();
        }

        private void AndTheRefreshStateFundingLines(params FundingLine[] fundingLines)
        {
            VariationContext.RefreshState.FundingLines = fundingLines;
        }

        private void AndThereIsNoPreviouslyReleasedVersion()
        {
            VariationContext.PublishedProvider.Released = null;
        }
    }
}