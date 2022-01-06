using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class IndicativeToLiveVariationStrategyTests : ReProfilingVariationStrategyTestsBase
    {
        protected override string Strategy => "IndicativeToLive";

        [TestInitialize]
        public void SetUp()
        {
            VariationStrategy = new IndicativeToLiveVariationStrategy();
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

            ThenNoVariationChangesWereQueued();
        }

        [TestMethod]
        public async Task SetTheVariationReasonIfIndicativeToLive()
        {
            int year = NewRandomNumber();
            string month = NewRandomMonth();

            GivenTheOtherwiseValidVariationContext(_ =>
            {
                _.RefreshState.IsIndicative = false;
            });

            VariationContext.PriorState.IsIndicative = true;

            AndTheRefreshStateFundingLines(NewFundingLine(),
                NewFundingLine(_ => _.WithFundingLineCode(FundingLineCode)
                    .WithFundingLineType(FundingLineType.Payment)
                    .WithValue(NewRandomNumber())
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(0)),
                            NewProfilePeriod(pp => pp.WithYear(year)
                                .WithTypeValue(month)
                                .WithType(ProfilePeriodType.CalendarMonth)
                                .WithOccurence(1)))))));

            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<MetaDataVariationsChange>();
            AndTheVariationReasonsWereRecordedOnTheVariationContext(VariationReason.IndicativeToLive);
            AndTheAffectedFundingLinesWereTracked(FundingLineCode);
        }
    }
}
