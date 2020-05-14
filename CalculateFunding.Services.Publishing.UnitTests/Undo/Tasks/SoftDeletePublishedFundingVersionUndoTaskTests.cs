using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    [TestClass]
    public class SoftDeletePublishedFundingVersionUndoTaskTests : PublishedFundingVersionUndoTaskTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Task = new SoftDeletePublishedFundingVersionUndoTask(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object);
        } 
        
        [TestMethod]
        public async Task DeletesOnlyBlobStoreDocumentsForFeedItems()
        {
            PublishedFundingVersion publishedFundingVersionOne = NewPublishedFundingVersion();
            PublishedFundingVersion publishedFundingVersionTwo = NewPublishedFundingVersion();
            PublishedFundingVersion publishedFundingVersionThree = NewPublishedFundingVersion();
            PublishedFundingVersion publishedFundingVersionFour = NewPublishedFundingVersion();
            
            GivenThePublishedFundingVersionFeed(NewFeedIterator(
                WithPages(Page(publishedFundingVersionOne, publishedFundingVersionTwo),
                    Page(publishedFundingVersionThree, publishedFundingVersionFour))));

            await WhenTheTaskIsRun();
            
            ThenNothingWasDeleted<PublishedFundingVersion>();
            AndThePublishedFundingVersionBlobDocumentsWereRemoved(publishedFundingVersionOne,
                publishedFundingVersionTwo,
                publishedFundingVersionThree,
                publishedFundingVersionFour);
        }
    }
}