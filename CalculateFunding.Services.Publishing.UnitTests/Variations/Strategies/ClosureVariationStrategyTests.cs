using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class ClosureVariationStrategyTests : ClosureVariationStrategyTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            ClosureVariationStrategy = new ClosureVariationStrategy();
        }

        [TestMethod]
        public async Task FailsPreconditionCheckIfUpdatedProviderHasASuccessor()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = NewRandomString());

            await WhenTheVariationsAreProcessed();

            VariationContext
                .ErrorMessages
                .Should()
                .BeEmpty();

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        [TestMethod]
        public async Task QueuesZeroRemainingProfileAndCloseProviderChangesIfPassesPreconditionChecks()
        {
            GivenTheOtherwiseValidVariationContext();

            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<ZeroRemainingProfilesChange>();
            ThenTheVariationChangeWasQueued<ZeroInitialPaymentProfilesChange>();
            AndTheVariationChangeWasQueued<ReAdjustFundingValuesForProfileValuesChange>();
        }

        [TestMethod]
        public async Task TracksErrorIfTotalFundingDiffersFromPriorStateWhenGeneratingChanges()
        {
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedTotalFunding += 1M);

            await WhenTheVariationsAreProcessed();

            IEnumerable<string> errorFieldValues = GetFieldValues(VariationContext.ErrorMessages.First());

            errorFieldValues
                .First()
                .Should()
                .Be(VariationContext.ProviderId);

            errorFieldValues
                .Skip(1)
                .First()
                .Should()
                .Be("Unable to apply strategy 'Closure'");


            errorFieldValues
                .Skip(2)
                .First()
                .Should()
                .Be("Unable to run Closure variation as TotalFunding has changed during the refresh funding");

            VariationContext
                .QueuedChanges
                .Should()
                .BeEmpty();
        }

        private IEnumerable<string> GetFieldValues(string errorRow)
        {
            return errorRow
                .Split("\",\"")
                .Select(_ =>
                    _.Replace("\"", "")
                );
        }
    }
}