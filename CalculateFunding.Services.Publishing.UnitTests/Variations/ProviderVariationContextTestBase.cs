using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.UnitTests.Profiling;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public abstract class ProviderVariationContextTestBase : ProfilingTestBase
    {
        protected ProviderVariationContext VariationContext;
        protected IPoliciesService _policiesService;
        private int _queuedChangeIndex;

        [TestInitialize]
        public void ProviderVariationContextTestBaseSetUp()
        {
            _queuedChangeIndex = 0;
            _policiesService = Substitute.For<IPoliciesService>();
            VariationContext = NewVariationContext();
        }

        private ProviderVariationContext NewVariationContext(Action<ProviderVariationContextBuilder> setUp = null)
        {
            decimal totalFunding = new RandomNumberBetween(100, 100000);
            string profilePatternKey = NewRandomString();
            string fundingLineCode = NewRandomString();
            uint templateCalculationId = (uint) new RandomNumberBetween(1, 1000);
            string valueOne = NewRandomString();
            string valueTwo = NewRandomString();

            ProviderVariationContextBuilder variationContextBuilder = new ProviderVariationContextBuilder()
                .WithPoliciesService(_policiesService)
                .WithPublishedProvider(NewPublishedProvider(_ => _
                    .WithReleased(NewPublishedProviderVersion(ppv => ppv
                        .WithTotalFunding(totalFunding)
                        .WithFundingCalculations(NewFundingCalculation(fc => fc.WithTemplateCalculationId(templateCalculationId).WithValue(valueOne)))))
                    ))
                .WithCurrentState(NewProvider(_ => _.WithStatus(VariationStrategy.Closed)))
                .WithUpdatedTotalFunding(totalFunding)
                .WithAllPublishedProviderSnapShots(new Dictionary<string, PublishedProviderSnapShots>());

            setUp?.Invoke(variationContextBuilder);

            return variationContextBuilder.Build();
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

        protected void AndTheVariationReasonsWereRecordedOnTheVariationContext(params VariationReason[] variationReasons)
        {
            foreach (VariationReason variationReason in variationReasons)
            {
                VariationContext
                    .VariationReasons
                    .Should()
                    .Contain(variationReason, variationReason.ToString());
            }
        }
        
        protected void AndTheVariationChangeWasQueued<TChange>()
            where TChange : IVariationChange
        {
            ThenTheVariationChangeWasQueued<TChange>();
        }

        protected void ThenNoVariationChangesWereQueued()
            => AndNoVariationChangesWereQueued();

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
            VariationContext.Successor.Current.FundingLines = fundingLines;
        }

        protected void AndTheFundingLines(params FundingLine[] fundingLines)
        {
            GivenTheFundingLines(fundingLines);
        }

        protected void AndTheProfilePatternKeys(params ProfilePatternKey[] profilePatternKeys)
        {
            GivenTheProfilePatternKeys(profilePatternKeys);
        }

        protected void GivenTheFundingLines(params FundingLine[] fundingLines)
        {
            VariationContext.RefreshState.FundingLines = fundingLines;
        }

        protected void GivenTheProfilePatternKeys(params ProfilePatternKey[] profilePatternKeys)
        {
            VariationContext.RefreshState.ProfilePatternKeys = profilePatternKeys;
        }

        protected void GivenTheCalculations(params FundingCalculation[] fundingCalculations)
        {
            VariationContext.RefreshState.Calculations = fundingCalculations;
        }

        protected void AndTheFundingCalculations(params FundingCalculation[] fundingCalculations)
        {
            GivenTheCalculations(fundingCalculations);
        }

        protected void AndTheSuccessorFundingCalculations(params FundingCalculation[] fundingCalculations)
        {
            VariationContext.Successor.Current.Calculations = fundingCalculations;
        }

        public void GivenThePublishedProviderOriginalSnapshot(string providerId, PublishedProviderSnapShots publishedProviderSnapShots)
        {
            VariationContext.AllPublishedProviderSnapShots.Add(providerId, publishedProviderSnapShots);
        }
    }
}