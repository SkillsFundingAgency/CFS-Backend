using System;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.UnitTests.Profiling;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations
{
    public abstract class ProviderVariationContextTestBase : ProfilingTestBase
    {
        protected ProviderVariationContext VariationContext;
        private int _queuedChangeIndex;

        [TestInitialize]
        public void ProviderVariationContextTestBaseSetUp()
        {
            _queuedChangeIndex = 0;
            
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
                .WithPublishedProvider(NewPublishedProvider(_ => _
                    .WithReleased(NewPublishedProviderVersion(ppv => ppv
                        .WithTotalFunding(totalFunding)
                        .WithFundingCalculations(NewFundingCalculation(fc => fc.WithTemplateCalculationId(templateCalculationId).WithValue(valueOne)))))
                    ))
                .WithCurrentState(NewProvider(_ => _.WithStatus(Variation.Closed)))
                .WithUpdatedTotalFunding(totalFunding);

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
            VariationContext.SuccessorRefreshState.FundingLines = fundingLines;
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
            VariationContext.SuccessorRefreshState.Calculations = fundingCalculations;
        }
    }
}