using System;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Tasks
{
    [TestClass]
    public class PublishedProviderUndoTaskTests : UndoTaskTestBase
    {
        private bool _isHardDelete;
        
        [TestInitialize]
        public void PublishedProviderUndoTaskBaseTestBaseSetUp()
        {
            _isHardDelete = NewRandomFlag();
            
            TaskContext = NewPublishedFundingUndoTaskContext(_ => 
                _.WithPublishedProviderDetails(NewUndoTaskDetails())
                    .WithPublishedProviderVersionDetails(NewUndoTaskDetails()));

            TaskDetails = TaskContext.PublishedProviderDetails;
            
            Task = new PublishedProviderUndoTask(Cosmos.Object,
                BlobStore.Object,
                ProducerConsumerFactory,
                Logger,
                JobTracker.Object,
                _isHardDelete);
        }
        
        [TestMethod]
        public async Task ExitsEarlyIfErrorWhenPagingFeed()
        {
            await WhenTheTaskIsRun();
            
            ThenNothingWasDeleted<PublishedProvider>();
            AndNothingWasUpdated<PublishedProvider>();
        }

        [TestMethod]
        public async Task ExitsEarlyIfFeedHasNoRecords()
        {
            GivenThePublishedProviderFeed(NewFeedIterator<PublishedProvider>());
            
            await WhenTheTaskIsRun();
            
            ThenNothingWasDeleted<PublishedProvider>();
            AndNothingWasUpdated<PublishedProvider>();
        }
        
        [TestMethod]
        public async Task SetsCurrentToLatestPreviousVersionAndDeletesIfInitialVersion()
        {
            PublishedProvider publishedProviderOne = NewPublishedProvider();
            PublishedProvider publishedProviderTwo = NewPublishedProvider();
            PublishedProvider publishedProviderThree = NewPublishedProvider();
            PublishedProvider publishedProviderFour = NewPublishedProvider();
            PublishedProvider publishedProviderFive = NewPublishedProvider();
            
            PublishedProviderVersion previousVersionTwo = NewPublishedProviderVersion();
            PublishedProviderVersion previousVersionThree = NewPublishedProviderVersion();
            PublishedProviderVersion previousVersionFive = NewPublishedProviderVersion();
            PublishedProviderVersion previousReleasedVersionThree = NewPublishedProviderVersion();

            GivenThePublishedProviderFeed(NewFeedIterator(WithPages(Page(publishedProviderOne, publishedProviderTwo),
                Page(publishedProviderThree, publishedProviderFour),
                Page(publishedProviderFive))));
            AndThePreviousLatestVersion(publishedProviderTwo.Current, previousVersionTwo);
            AndThePreviousLatestVersion(publishedProviderThree.Current, previousVersionThree);
            AndThePreviousLatestReleasedVersion(publishedProviderThree.Current, previousReleasedVersionThree);
            AndThePreviousLatestVersion(publishedProviderFive.Current, previousVersionFive);

            await WhenTheTaskIsRun();
            
            ThenTheDocumentsWereDeleted(new [] { publishedProviderOne}, 
                new [] { publishedProviderOne.PartitionKey }, 
                _isHardDelete);
            ThenTheDocumentsWereDeleted(new [] { publishedProviderFour}, 
                new [] { publishedProviderFour.PartitionKey }, 
                _isHardDelete);
            AndTheDocumentsWereUpdated(new [] { publishedProviderTwo },
                new [] {publishedProviderTwo.PartitionKey} );
            AndTheDocumentsWereUpdated(new [] { publishedProviderThree },
                new [] {publishedProviderThree.PartitionKey} );
            AndTheDocumentsWereUpdated(new [] { publishedProviderFive },
                new [] {publishedProviderFive.PartitionKey} );
            AndThePublishedProviderHasCurrent((publishedProviderFive, previousVersionFive), 
                (publishedProviderTwo, previousVersionTwo),
                (publishedProviderThree, previousVersionThree));
            AndThePublishedProviderHasReleased((publishedProviderFive, null), 
                (publishedProviderTwo, null),
                (publishedProviderThree, previousReleasedVersionThree));
        }

        protected void AndThePublishedProviderHasCurrent(params (PublishedProvider funding, PublishedProviderVersion current)[] expectedMatches)
        {
            foreach ((PublishedProvider funding, PublishedProviderVersion current) expectedMatch in expectedMatches)
            {
                expectedMatch.funding.Current
                    .Should()
                    .BeSameAs(expectedMatch.current);
            }
        }
        
        protected void AndThePublishedProviderHasReleased(params (PublishedProvider funding, PublishedProviderVersion released)[] expectedMatches)
        {
            foreach ((PublishedProvider funding, PublishedProviderVersion released) expectedMatch in expectedMatches)
            {
                expectedMatch.funding.Released
                    .Should()
                    .BeSameAs(expectedMatch.released);
            }
        }
        
        protected void GivenThePublishedProviderFeed(ICosmosDbFeedIterator feed)
        {
            Cosmos.Setup(_ => _.GetPublishedProviders(TaskDetails.FundingStreamId,
                    TaskDetails.FundingPeriodId,
                    TaskDetails.TimeStamp))
                .Returns(feed);
        }

        protected void AndThePreviousLatestVersion(PublishedProviderVersion current, PublishedProviderVersion previous)
        {
            UndoTaskDetails publishedProviderVersionDetails = TaskContext.PublishedProviderVersionDetails;
            
            Cosmos.Setup(_ => _.GetLatestEarlierPublishedProviderVersion(publishedProviderVersionDetails.FundingStreamId,
                    publishedProviderVersionDetails.FundingPeriodId,
                    publishedProviderVersionDetails.TimeStamp,
                    current.ProviderId,
                    null))
                .ReturnsAsync(previous);
        }
        
        protected void AndThePreviousLatestReleasedVersion(PublishedProviderVersion current, PublishedProviderVersion previousReleased)
        {
            UndoTaskDetails publishedProviderVersionDetails = TaskContext.PublishedProviderVersionDetails;
            
            Cosmos.Setup(_ => _.GetLatestEarlierPublishedProviderVersion(publishedProviderVersionDetails.FundingStreamId,
                    publishedProviderVersionDetails.FundingPeriodId,
                    publishedProviderVersionDetails.TimeStamp,
                    current.ProviderId,
                    PublishedProviderStatus.Released))
                .ReturnsAsync(previousReleased);
        }

        protected PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder fundingBuilder = new PublishedProviderBuilder()
                .WithCurrent(NewPublishedProviderVersion())
                .WithReleased(NewPublishedProviderVersion());

            setUp?.Invoke(fundingBuilder);
            
            return fundingBuilder.Build();
        }
    }
}