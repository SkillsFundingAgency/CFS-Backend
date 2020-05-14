using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    [TestClass]
    public class HardDeletePublishedFundingVersionUndoTaskTests : PublishedFundingVersionUndoTaskTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Task = new HardDeletePublishedFundingVersionUndoTask(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object);
        }

        [TestMethod]
        public async Task DeletesCosmosDocumentsAndMatchingBlobStoreDocumentsForFeedItems()
        {
            PublishedFundingVersion publishedFundingVersionOne = NewPublishedFundingVersion();
            PublishedFundingVersion publishedFundingVersionTwo = NewPublishedFundingVersion();
            PublishedFundingVersion publishedFundingVersionThree = NewPublishedFundingVersion();
            PublishedFundingVersion publishedFundingVersionFour = NewPublishedFundingVersion();
            
            GivenThePublishedFundingVersionFeed(NewFeedIterator(
                WithPages(Page(publishedFundingVersionOne, publishedFundingVersionTwo),
                    Page(publishedFundingVersionThree, publishedFundingVersionFour))));

            await WhenTheTaskIsRun();
            
            ThenTheDocumentsWereDeleted(new [] { publishedFundingVersionOne, publishedFundingVersionTwo },
                new [] { publishedFundingVersionOne.PartitionKey, publishedFundingVersionTwo.PartitionKey },
                true);
            AndTheDocumentsWereDeleted(new [] { publishedFundingVersionThree, publishedFundingVersionFour },
                new [] { publishedFundingVersionThree.PartitionKey, publishedFundingVersionFour.PartitionKey },
                true);
            AndThePublishedFundingVersionBlobDocumentsWereRemoved(publishedFundingVersionOne,
                publishedFundingVersionTwo,
                publishedFundingVersionThree,
                publishedFundingVersionFour);
        }
    }
}