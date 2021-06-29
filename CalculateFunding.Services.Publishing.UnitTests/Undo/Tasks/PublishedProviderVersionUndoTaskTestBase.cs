using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    public abstract class PublishedProviderVersionUndoTaskTestBase : UndoTaskTestBase
    {
        [TestInitialize]
        public void  PublishedProviderVersionUndoTaskTestBaseSetUp()
        {
            TaskContext = NewPublishedFundingUndoTaskContext(_ => 
                _.WithPublishedProviderVersionDetails(NewUndoTaskDetails()));

            TaskDetails = TaskContext.UndoTaskDetails;
        }
        
        [TestMethod]
        public async Task ExitsEarlyIfErrorWhenPagingFeed()
        {
            await WhenTheTaskIsRun();
            
            ThenNothingWasDeleted<PublishedProviderVersion>();
            AndNothingWasUpdated<PublishedProviderVersion>();
        }

        [TestMethod]
        public async Task ExitsEarlyIfFeedHasNoRecords()
        {
            GivenThePublishedProviderVersionFeed(NewFeedIterator<PublishedProviderVersion>());
            
            await WhenTheTaskIsRun();
            
            ThenNothingWasDeleted<PublishedProviderVersion>();
            AndNothingWasUpdated<PublishedProviderVersion>();
        }
        
        protected void GivenThePublishedProviderVersionFeed(ICosmosDbFeedIterator feed)
        {

            Cosmos.Setup(_ => _.GetPublishedProviderVersions(TaskDetails.FundingStreamId,
                    TaskDetails.FundingPeriodId,
                    TaskDetails.TimeStamp))
                .Returns(feed);
        }     
    }
}