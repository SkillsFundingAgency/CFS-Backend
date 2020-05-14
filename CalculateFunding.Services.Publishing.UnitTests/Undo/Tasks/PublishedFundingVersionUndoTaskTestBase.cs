using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    public abstract class PublishedFundingVersionUndoTaskTestBase : UndoTaskTestBase
    {
        [TestInitialize]
        public void PublishedFundingVersionUndoTaskTestBaseSetUp()
        {
            TaskContext = NewPublishedFundingUndoTaskContext(_ => 
                _.WithPublishedFundingVersionDetails(NewCorrelationIdDetails()));

            TaskDetails = TaskContext.PublishedFundingVersionDetails;
        }
        
        [TestMethod]
        public async Task ExitsEarlyIfErrorWhenPagingFeed()
        {
            await WhenTheTaskIsRun();
            
            ThenNothingWasDeleted<PublishedFundingVersion>();
            AndNothingWasUpdated<PublishedFundingVersion>();
        }

        [TestMethod]
        public async Task ExitsEarlyIfFeedHasNoRecords()
        {
            GivenThePublishedFundingVersionFeed(NewFeedIterator<PublishedFundingVersion>());
            
            await WhenTheTaskIsRun();
            
            ThenNothingWasDeleted<PublishedFundingVersion>();
            AndNothingWasUpdated<PublishedFundingVersion>();
        }
        
        protected void GivenThePublishedFundingVersionFeed(ICosmosDbFeedIterator<PublishedFundingVersion> feed)
        {

            Cosmos.Setup(_ => _.GetPublishedFundingVersions(TaskDetails.FundingStreamId,
                    TaskDetails.FundingPeriodId,
                    TaskDetails.TimeStamp))
                .Returns(feed);
        }
    }
}