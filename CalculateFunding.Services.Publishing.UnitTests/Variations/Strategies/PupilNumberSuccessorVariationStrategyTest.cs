using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Strategies
{
    [TestClass]
    public class PupilNumberSuccessorVariationStrategyTest : SuccessorVariationStrategyTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            VariationName = "Pupil Number Successor";
            
            ClosureVariationStrategy = new PupilNumberSuccessorVariationStrategy(ProviderService.Object);
        }
        
        [TestMethod]
        public async Task QueuesMovePupilNumbersToSuccessorChangeIfPassesPreconditionChecks()
        {
            string successorId = NewRandomString();
            
            GivenTheOtherwiseValidVariationContext(_ => _.UpdatedProvider.Successor = successorId);
            AndTheAllRefreshStateProvider(NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => 
                ppv.WithProviderId(successorId)))));
            
            await WhenTheVariationsAreDetermined();

            ThenTheVariationChangeWasQueued<MovePupilNumbersToSuccessorChange>();
            AndThePredecessorWasAddedToTheSuccessor(VariationContext.ProviderId);
        }
    }
}