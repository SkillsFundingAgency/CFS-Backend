using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    [TestClass]
    public class SoftDeletePublishedProviderVersionUndoTaskTests : PublishedProviderVersionUndoTaskTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Task = new SoftDeletePublishedProviderVersionsUndoTask(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object);
        }
        
        [TestMethod]
        public async Task DeletesOnlyBlobStoreDocumentsForFeedItems()
        {
            PublishedProviderVersion publishedProviderVersionOne = NewPublishedProviderVersion();
            PublishedProviderVersion publishedProviderVersionTwo = NewPublishedProviderVersion();
            PublishedProviderVersion publishedProviderVersionThree = NewPublishedProviderVersion();
            PublishedProviderVersion publishedProviderVersionFour = NewPublishedProviderVersion();
            
            GivenThePublishedProviderVersionFeed(NewFeedIterator(
                WithPages(Page(publishedProviderVersionOne, publishedProviderVersionTwo),
                    Page(publishedProviderVersionThree, publishedProviderVersionFour))));

            await WhenTheTaskIsRun();
            
            ThenTheDocumentsWereDeleted(new [] { publishedProviderVersionOne, publishedProviderVersionTwo },
                new [] { publishedProviderVersionOne.PartitionKey, publishedProviderVersionTwo.PartitionKey },
                false);
            AndTheDocumentsWereDeleted(new [] { publishedProviderVersionThree, publishedProviderVersionFour },
                new [] { publishedProviderVersionThree.PartitionKey, publishedProviderVersionFour.PartitionKey },
                false);
            AndThePublishedProviderVersionBlobDocumentsWereRemoved(publishedProviderVersionOne,
                publishedProviderVersionTwo,
                publishedProviderVersionThree,
                publishedProviderVersionFour);
        }
    }
}