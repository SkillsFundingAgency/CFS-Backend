using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class PupilNumberSuccessorVariationStrategyTest : SuccessorVariationStrategyTestBase
    {
        private Mock<ILogger> _logger;
        [TestInitialize]
        public void SetUp()
        {
            VariationName = "PupilNumberSuccessor";

            _logger = new Mock<ILogger>();

            ClosureVariationStrategy = new PupilNumberSuccessorVariationStrategy(ProviderService.Object, _logger.Object);
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
        public async Task QueuesMovePupilNumbersToSuccessorChangeIfPassesPreconditionChecks()
        {
            string successorId = NewRandomString();
            
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = successorId);
            AndTheAllRefreshStateProvider(NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => 
                ppv.WithProviderId(successorId)))));
            
            await WhenTheVariationsAreProcessed();

            ThenTheVariationChangeWasQueued<MovePupilNumbersToSuccessorChange>();
            AndThePredecessorWasAddedToTheSuccessor(VariationContext.ProviderId);
        }
    }
}